using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using MU5PrototypeProject.Configuration;
using MU5PrototypeProject.Models;
using MU5PrototypeProject.Security;

namespace MU5PrototypeProject.Data
{
    public static class IdentityInitializer
    {
        public static async Task InitializeAsync(IServiceProvider serviceProvider)
        {
            var logger = serviceProvider.GetRequiredService<ILoggerFactory>()
                .CreateLogger("IdentityInitializer");
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var bootstrapOwnerOptions = serviceProvider.GetRequiredService<IOptions<BootstrapOwnerOptions>>().Value;
            var demoAccountOptions = serviceProvider.GetRequiredService<IOptions<DemoAccountOptions>>().Value;

            foreach (var role in AppRoles.All)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    var roleResult = await roleManager.CreateAsync(new IdentityRole(role));
                    if (!roleResult.Succeeded)
                    {
                        throw new InvalidOperationException(
                            $"Unable to create role '{role}': {string.Join("; ", roleResult.Errors.Select(error => error.Description))}");
                    }
                }
            }

            if (bootstrapOwnerOptions.IsConfigured)
            {
                await EnsureBootstrapOwnerAsync(userManager, bootstrapOwnerOptions);
            }
            else
            {
                var ownerUsers = await userManager.GetUsersInRoleAsync(AppRoles.Owner);
                if (ownerUsers.Count == 0 && !demoAccountOptions.IsConfigured)
                {
                    logger.LogWarning(
                        "No bootstrap owner account was created because the {SectionName} configuration section is missing required values.",
                        BootstrapOwnerOptions.SectionName);
                }
            }

            await EnsureDemoAccountAsync(userManager, demoAccountOptions);
        }

        private static async Task EnsureBootstrapOwnerAsync(
            UserManager<ApplicationUser> userManager,
            BootstrapOwnerOptions options)
        {
            var ownerEmail = options.Email!.Trim().ToLowerInvariant();
            var ownerUser = await userManager.FindByEmailAsync(ownerEmail);

            if (ownerUser is null)
            {
                ownerUser = new ApplicationUser
                {
                    FirstName = options.FirstName!.Trim(),
                    LastName = options.LastName!.Trim(),
                    Email = ownerEmail,
                    UserName = ownerEmail,
                    EmailConfirmed = true,
                    IsActive = true,
                    MustChangePassword = false
                };

                var createResult = await userManager.CreateAsync(ownerUser, options.Password!);
                if (!createResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Unable to create bootstrap owner account: {string.Join("; ", createResult.Errors.Select(error => error.Description))}");
                }
            }
            else
            {
                if (!await userManager.IsInRoleAsync(ownerUser, AppRoles.Owner))
                {
                    throw new InvalidOperationException(
                        $"Bootstrap owner email '{ownerEmail}' is already used by a non-owner account. " +
                        "Update BootstrapOwner:Email to a dedicated owner account email to avoid overriding an existing user.");
                }

                ownerUser.FirstName = options.FirstName!.Trim();
                ownerUser.LastName = options.LastName!.Trim();
                ownerUser.Email = ownerEmail;
                ownerUser.UserName = ownerEmail;
                ownerUser.EmailConfirmed = true;
                ownerUser.IsActive = true;
                ownerUser.MustChangePassword = false;

                var updateResult = await userManager.UpdateAsync(ownerUser);
                if (!updateResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Unable to update bootstrap owner account: {string.Join("; ", updateResult.Errors.Select(error => error.Description))}");
                }
            }

            if (!await userManager.IsInRoleAsync(ownerUser, AppRoles.Owner))
            {
                var addToRoleResult = await userManager.AddToRoleAsync(ownerUser, AppRoles.Owner);
                if (!addToRoleResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Unable to assign bootstrap owner role: {string.Join("; ", addToRoleResult.Errors.Select(error => error.Description))}");
                }
            }
        }

        private static async Task EnsureDemoAccountAsync(
            UserManager<ApplicationUser> userManager,
            DemoAccountOptions options)
        {
            if (!options.Enabled)
            {
                return;
            }

            if (!options.IsConfigured)
            {
                throw new InvalidOperationException(
                    $"{DemoAccountOptions.SectionName} is enabled but is missing a first name, last name, email, or supported role.");
            }

            var demoEmail = options.Email!.Trim().ToLowerInvariant();
            var demoRole = options.Role!;
            var demoUser = await userManager.FindByEmailAsync(demoEmail);

            if (demoUser is null)
            {
                demoUser = new ApplicationUser
                {
                    FirstName = options.FirstName!.Trim(),
                    LastName = options.LastName!.Trim(),
                    Email = demoEmail,
                    UserName = demoEmail,
                    EmailConfirmed = true,
                    IsActive = true,
                    MustChangePassword = false
                };

                var createResult = await userManager.CreateAsync(demoUser);
                if (!createResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Unable to create demo account: {string.Join("; ", createResult.Errors.Select(error => error.Description))}");
                }
            }
            else
            {
                demoUser.FirstName = options.FirstName!.Trim();
                demoUser.LastName = options.LastName!.Trim();
                demoUser.Email = demoEmail;
                demoUser.UserName = demoEmail;
                demoUser.EmailConfirmed = true;
                demoUser.IsActive = true;
                demoUser.MustChangePassword = false;

                var updateResult = await userManager.UpdateAsync(demoUser);
                if (!updateResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Unable to update demo account: {string.Join("; ", updateResult.Errors.Select(error => error.Description))}");
                }
            }

            var currentRoles = await userManager.GetRolesAsync(demoUser);
            var rolesToRemove = currentRoles
                .Where(role => !string.Equals(role, demoRole, StringComparison.Ordinal))
                .ToList();

            if (rolesToRemove.Count > 0)
            {
                var removeRolesResult = await userManager.RemoveFromRolesAsync(demoUser, rolesToRemove);
                if (!removeRolesResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Unable to update demo account roles: {string.Join("; ", removeRolesResult.Errors.Select(error => error.Description))}");
                }
            }

            if (!await userManager.IsInRoleAsync(demoUser, demoRole))
            {
                var addToRoleResult = await userManager.AddToRoleAsync(demoUser, demoRole);
                if (!addToRoleResult.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Unable to assign demo account role: {string.Join("; ", addToRoleResult.Errors.Select(error => error.Description))}");
                }
            }
        }
    }
}
