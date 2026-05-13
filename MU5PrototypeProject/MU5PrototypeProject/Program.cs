using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MU5PrototypeProject.Configuration;
using MU5PrototypeProject.Data;
using MU5PrototypeProject.Middleware;
using MU5PrototypeProject.Models;
using MU5PrototypeProject.Security;

var builder = WebApplication.CreateBuilder(args);

var configuredConnectionString = builder.Configuration.GetConnectionString("MUContext")
        ?? throw new InvalidOperationException("Connection string 'MUContext' not found.");
var connectionString = ResolveSqliteConnectionString(configuredConnectionString);

builder.Services.Configure<BootstrapOwnerOptions>(
    builder.Configuration.GetSection(BootstrapOwnerOptions.SectionName));
builder.Services.Configure<DemoAccountOptions>(
    builder.Configuration.GetSection(DemoAccountOptions.SectionName));
builder.Services.Configure<PasswordResetDeliveryOptions>(
    builder.Configuration.GetSection(PasswordResetDeliveryOptions.SectionName));

var passwordResetDeliveryOptions = builder.Configuration
    .GetSection(PasswordResetDeliveryOptions.SectionName)
    .Get<PasswordResetDeliveryOptions>() ?? new PasswordResetDeliveryOptions();

if (builder.Environment.IsProduction() &&
    passwordResetDeliveryOptions.Mode == PasswordResetDeliveryMode.ScreenPreview)
{
    throw new InvalidOperationException(
        "Password reset screen preview cannot be enabled in Production. Change PasswordResetDelivery:Mode before starting the app.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddDbContext<MUContext>(options =>
    options.UseSqlite(connectionString));

//To give acess to IHttpContextAccessor for Audit Data with IAudtable
builder.Services.TryAddSingleton<IHttpContextAccessor, HttpContextAccessor>();

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.User.RequireUniqueEmail = true;

        options.Password.RequiredLength = 12;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;

        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.MaxFailedAccessAttempts = 5;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
});

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    options.AddPolicy(AuthorizationPolicies.OwnerOnly, policy =>
        policy.RequireRole(AppRoles.Owner));

    options.AddPolicy(AuthorizationPolicies.OwnerOrAdministration, policy =>
        policy.RequireRole(AppRoles.Owner, AppRoles.Administration));
});

builder.Services.AddScoped<DisabledPasswordResetDeliveryService>();
builder.Services.AddScoped<ScreenPreviewPasswordResetDeliveryService>();
builder.Services.AddScoped<IPasswordResetDeliveryService>(serviceProvider =>
{
    var options = serviceProvider.GetRequiredService<IOptions<PasswordResetDeliveryOptions>>().Value;

    return options.Mode switch
    {
        PasswordResetDeliveryMode.Disabled =>
            serviceProvider.GetRequiredService<DisabledPasswordResetDeliveryService>(),
        PasswordResetDeliveryMode.ScreenPreview =>
            serviceProvider.GetRequiredService<ScreenPreviewPasswordResetDeliveryService>(),
        PasswordResetDeliveryMode.Email =>
            throw new InvalidOperationException(
                "PasswordResetDelivery:Mode is set to Email, but no email delivery service is configured yet."),
        _ => throw new InvalidOperationException(
            $"Unsupported password reset delivery mode '{options.Mode}'.")
    };
});

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseMiddleware<AccountStateMiddleware>();
app.UseAuthorization();

app.MapStaticAssets().AllowAnonymous();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorPages()
   .WithStaticAssets();

// Prepare DB and seed data (Medical Office style: just before app.Run)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var seedSampleData = builder.Configuration.GetValue("Seeding:SeedSampleData", true);
    var identityContext = services.GetRequiredService<ApplicationDbContext>();
  
    await identityContext.Database.MigrateAsync();
    await IdentityInitializer.InitializeAsync(services);

    MUInitializer.Initialize(
        serviceProvider: services,
        deleteDatabase: false,
        useMigrations: true,
        seedSampleData: seedSampleData);
}

app.Run();

static string ResolveSqliteConnectionString(string configuredConnectionString)
{
    var sqliteConnection = new SqliteConnectionStringBuilder(configuredConnectionString);
    var dataSource = sqliteConnection.DataSource;

    if (string.IsNullOrWhiteSpace(dataSource)
        || string.Equals(dataSource, ":memory:", StringComparison.OrdinalIgnoreCase)
        || Path.IsPathRooted(dataSource))
    {
        return sqliteConnection.ConnectionString;
    }

    var azureHome = Environment.GetEnvironmentVariable("HOME");
    var isAzureAppService = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME"));
    if (!isAzureAppService || string.IsNullOrWhiteSpace(azureHome))
    {
        return sqliteConnection.ConnectionString;
    }

    var dataDirectory = Path.Combine(azureHome, "data");
    Directory.CreateDirectory(dataDirectory);
    sqliteConnection.DataSource = Path.Combine(dataDirectory, dataSource);

    return sqliteConnection.ConnectionString;
}
