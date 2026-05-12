using Microsoft.AspNetCore.Identity;
using MU5PrototypeProject.Models;

namespace MU5PrototypeProject.Middleware
{
    public class AccountStateMiddleware
    {
        private static readonly PathString[] AllowedPaths =
        [
            new PathString("/Account/Login"),
            new PathString("/Account/AccessDenied"),
            new PathString("/Account/ChangePassword"),
            new PathString("/Account/Logout")
        ];

        private readonly RequestDelegate _next;

        public AccountStateMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(
            HttpContext context,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var user = await userManager.GetUserAsync(context.User);

                if (user is null)
                {
                    await signInManager.SignOutAsync();
                    context.Response.Redirect("/Account/Login");
                    return;
                }

                if (!user.IsActive)
                {
                    await signInManager.SignOutAsync();
                    context.Response.Redirect("/Account/Login?reason=inactive");
                    return;
                }

                if (user.MustChangePassword && !IsAllowedPath(context.Request.Path))
                {
                    var returnUrl = context.Request.Path + context.Request.QueryString;
                    context.Response.Redirect($"/Account/ChangePassword?returnUrl={Uri.EscapeDataString(returnUrl)}");
                    return;
                }
            }

            await _next(context);
        }

        private static bool IsAllowedPath(PathString path)
        {
            if (AllowedPaths.Any(allowedPath =>
                path.StartsWithSegments(allowedPath, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            // Let static assets load while a user is on the forced change-password screen.
            var value = path.Value;
            return !string.IsNullOrWhiteSpace(value) && Path.HasExtension(value);
        }
    }
}
