using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using MU5PrototypeProject.Configuration;
using MU5PrototypeProject.Data;
using MU5PrototypeProject.Models;
using MU5PrototypeProject.Models.ViewModels;
using MU5PrototypeProject.Security;
using System.Text;

namespace MU5PrototypeProject.Controllers
{
    public class AccountController : Controller
    {
        private const string InvalidPreviewResetToken = "preview-invalid-token";
        private const string InvalidResetLinkMessage = "This password reset link is invalid or has expired.";
        private const string PasswordResetPreviewUrlKey = "PasswordResetPreviewUrl";

        private readonly ApplicationDbContext _identityContext;
        private readonly DemoAccountOptions _demoAccountOptions;
        private readonly ILogger<AccountController> _logger;
        private readonly IPasswordResetDeliveryService _passwordResetDeliveryService;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public AccountController(
            IOptions<DemoAccountOptions> demoAccountOptions,
            ApplicationDbContext identityContext,
            ILogger<AccountController> logger,
            IPasswordResetDeliveryService passwordResetDeliveryService,
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager)
        {
            _demoAccountOptions = demoAccountOptions.Value;
            _identityContext = identityContext;
            _logger = logger;
            _passwordResetDeliveryService = passwordResetDeliveryService;
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [AllowAnonymous]
        public async Task<IActionResult> Login(string? returnUrl = null, string? reason = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser?.MustChangePassword == true)
                {
                    return RedirectToAction(nameof(ChangePassword), new { returnUrl });
                }

                return RedirectToAction("Index", "Home");
            }

            if (string.Equals(reason, "inactive", StringComparison.OrdinalIgnoreCase))
            {
                ViewData["ReasonMessage"] = "This account is inactive. Contact the owner if you need access restored.";
            }

            PrepareLoginView(returnUrl);
            return View(new LoginViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            PrepareLoginView(returnUrl);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email.Trim());
            if (user is null)
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(model);
            }

            if (!user.IsActive)
            {
                ModelState.AddModelError(string.Empty, "This account is inactive.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(
                user,
                model.Password,
                model.RememberMe,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                if (user.MustChangePassword)
                {
                    return RedirectToAction(nameof(ChangePassword), new { returnUrl });
                }

                return RedirectToLocal(returnUrl);
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "This account has been locked for 15 minutes after too many failed login attempts.");
                return View(model);
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DemoLogin(string? returnUrl = null)
        {
            PrepareLoginView(returnUrl);

            if (!_demoAccountOptions.IsConfigured)
            {
                return NotFound();
            }

            var user = await _userManager.FindByEmailAsync(NormalizeEmail(_demoAccountOptions.Email!));
            if (user is null || !user.IsActive)
            {
                ModelState.AddModelError(string.Empty, "Demo login is not available right now.");
                return View(nameof(Login), new LoginViewModel());
            }

            await _signInManager.SignOutAsync();
            await _signInManager.SignInAsync(user, isPersistent: false);

            return RedirectToLocal(returnUrl);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }

        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [Authorize]
        public IActionResult ChangePassword(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(new ChangePasswordViewModel());
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user is null)
            {
                return RedirectToAction(nameof(Login));
            }

            if (await _userManager.CheckPasswordAsync(user, model.NewPassword))
            {
                ModelState.AddModelError(nameof(model.NewPassword), "Your new password must be different from your current password.");
                return View(model);
            }

            await using var transaction = await _identityContext.Database.BeginTransactionAsync();

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            if (!result.Succeeded)
            {
                AddErrors(result);
                return View(model);
            }

            user.MustChangePassword = false;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                AddErrors(updateResult);
                return View(model);
            }

            await transaction.CommitAsync();

            await _signInManager.RefreshSignInAsync(user);

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null)
            {
                return RedirectToAction(nameof(Login));
            }

            var model = new EditProfileViewModel
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? user.UserName ?? string.Empty
            };

            return View(model);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(EditProfileViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null)
            {
                return RedirectToAction(nameof(Login));
            }

            if (string.IsNullOrWhiteSpace(model.Email))
            {
                ModelState.AddModelError(nameof(model.Email), "Email is required.");
            }
            else
            {
                model.Email = NormalizeEmail(model.Email);
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            user.FirstName = model.FirstName.Trim();
            user.LastName = model.LastName.Trim();

            var currentEmail = NormalizeEmail(user.Email ?? user.UserName ?? string.Empty);
            if (!string.Equals(currentEmail, model.Email, StringComparison.Ordinal))
            {
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser is not null && !string.Equals(existingUser.Id, user.Id, StringComparison.Ordinal))
                {
                    ModelState.AddModelError(nameof(model.Email), "An account with this email already exists.");
                    return View(model);
                }

                user.Email = model.Email;
                user.UserName = model.Email;
                user.EmailConfirmed = false;
            }

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                AddErrors(result);
                return View(model);
            }

            await _signInManager.RefreshSignInAsync(user);

            TempData["StatusMessage"] = "Your profile has been updated.";
            return RedirectToAction(nameof(Profile));
        }

        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            ClearPasswordResetPreview();

            var email = NormalizeEmail(model.Email);

            try
            {
                var previewResetUrl = CreateResetPasswordUrl(email, EncodeResetCode(InvalidPreviewResetToken));
                string? actualResetUrl = null;

                var user = await _userManager.FindByEmailAsync(email);
                if (user is not null && user.IsActive)
                {
                    var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                    actualResetUrl = CreateResetPasswordUrl(email, EncodeResetCode(token));
                }

                var deliveryResult = await _passwordResetDeliveryService.DeliverPasswordResetAsync(
                    new PasswordResetDeliveryRequest
                    {
                        Email = email,
                        ActualResetUrl = actualResetUrl,
                        PreviewResetUrl = previewResetUrl
                    });

                StorePasswordResetPreview(deliveryResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unable to prepare password reset delivery for {Email}.", email);
                ClearPasswordResetPreview();
            }

            return RedirectToAction(nameof(ForgotPasswordConfirmation));
        }

        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View(new ForgotPasswordConfirmationViewModel
            {
                PreviewResetUrl = TempData.Peek(PasswordResetPreviewUrlKey) as string
            });
        }

        [AllowAnonymous]
        public IActionResult ResetPassword(string? email = null, string? code = null)
        {
            var model = new ResetPasswordViewModel
            {
                Email = string.IsNullOrWhiteSpace(email) ? string.Empty : NormalizeEmail(email),
                Code = code ?? string.Empty
            };

            if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Code))
            {
                ModelState.AddModelError(string.Empty, "This password reset link is invalid or incomplete.");
            }

            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            model.Email = NormalizeEmail(model.Email);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user is null || !user.IsActive)
            {
                ModelState.AddModelError(string.Empty, InvalidResetLinkMessage);
                return View(model);
            }

            string decodedCode;
            try
            {
                decodedCode = DecodeResetCode(model.Code);
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, InvalidResetLinkMessage);
                return View(model);
            }

            await using var transaction = await _identityContext.Database.BeginTransactionAsync();

            var resetResult = await _userManager.ResetPasswordAsync(user, decodedCode, model.NewPassword);
            if (!resetResult.Succeeded)
            {
                AddErrors(resetResult, InvalidResetLinkMessage);
                return View(model);
            }

            user.MustChangePassword = false;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                AddErrors(updateResult);
                return View(model);
            }

            var unlockResult = await _userManager.SetLockoutEndDateAsync(user, null);
            if (!unlockResult.Succeeded)
            {
                AddErrors(unlockResult);
                return View(model);
            }

            var resetFailedCountResult = await _userManager.ResetAccessFailedCountAsync(user);
            if (!resetFailedCountResult.Succeeded)
            {
                AddErrors(resetFailedCountResult);
                return View(model);
            }

            await transaction.CommitAsync();

            return RedirectToAction(nameof(ResetPasswordConfirmation));
        }

        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        private void PrepareLoginView(string? returnUrl)
        {
            ViewData["ReturnUrl"] = returnUrl;
            ViewData["DemoLoginEnabled"] = _demoAccountOptions.IsConfigured;
        }

        private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

        private string CreateResetPasswordUrl(string email, string encodedCode) =>
            Url.Action(
                nameof(ResetPassword),
                "Account",
                new { email, code = encodedCode },
                Request.Scheme)
            ?? throw new InvalidOperationException("Unable to generate the password reset URL.");

        private static string EncodeResetCode(string code) =>
            WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

        private static string DecodeResetCode(string encodedCode) =>
            Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(encodedCode));

        private void StorePasswordResetPreview(PasswordResetDeliveryResult result)
        {
            if (result.HasPreview)
            {
                TempData[PasswordResetPreviewUrlKey] = result.PreviewResetUrl!;
                return;
            }

            ClearPasswordResetPreview();
        }

        private void ClearPasswordResetPreview()
        {
            TempData.Remove(PasswordResetPreviewUrlKey);
        }

        private void AddErrors(IdentityResult identityResult, string? invalidTokenMessage = null)
        {
            foreach (var error in identityResult.Errors)
            {
                if (!string.IsNullOrWhiteSpace(invalidTokenMessage) &&
                    string.Equals(error.Code, nameof(IdentityErrorDescriber.InvalidToken), StringComparison.Ordinal))
                {
                    ModelState.AddModelError(string.Empty, invalidTokenMessage);
                    continue;
                }

                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
    }
}
