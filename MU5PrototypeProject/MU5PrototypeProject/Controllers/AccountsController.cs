using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MU5PrototypeProject.Configuration;
using MU5PrototypeProject.Data;
using MU5PrototypeProject.Models;
using MU5PrototypeProject.Models.ViewModels;
using MU5PrototypeProject.Security;
using System.ComponentModel.DataAnnotations;

namespace MU5PrototypeProject.Controllers
{
    [Authorize(Policy = AuthorizationPolicies.OwnerOnly)]
    public class AccountsController : Controller
    {
        private const string CurrentAccountBlockedReason = "Current account";
        private const string BootstrapOwnerBlockedReason = "Configured bootstrap owner";
        private const string OnlyActiveOwnerBlockedReason = "Only active owner";

        private readonly BootstrapOwnerOptions _bootstrapOwnerOptions;
        private readonly MUContext _muContext;
        private readonly IPasswordHasher<ApplicationUser> _passwordHasher;
        private readonly UserManager<ApplicationUser> _userManager;

        public AccountsController(
            IOptions<BootstrapOwnerOptions> bootstrapOwnerOptions,
            MUContext muContext,
            IPasswordHasher<ApplicationUser> passwordHasher,
            UserManager<ApplicationUser> userManager)
        {
            _bootstrapOwnerOptions = bootstrapOwnerOptions.Value;
            _muContext = muContext;
            _passwordHasher = passwordHasher;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var currentUserId = _userManager.GetUserId(User);
            var ownerUsers = await _userManager.GetUsersInRoleAsync(AppRoles.Owner);
            var ownerUserIds = ownerUsers
                .Select(ownerUser => ownerUser.Id)
                .ToHashSet(StringComparer.Ordinal);
            var activeOwnerCount = ownerUsers.Count(ownerUser => ownerUser.IsActive);

            var users = await _userManager.Users
                .OrderBy(user => user.LastName)
                .ThenBy(user => user.FirstName)
                .ToListAsync();

            var trainersByUserId = await _muContext.Trainers
                .AsNoTracking()
                .Where(trainer => trainer.ApplicationUserId != null)
                .ToDictionaryAsync(trainer => trainer.ApplicationUserId!, trainer => trainer.TrainerName);

            var model = new List<AccountListItemViewModel>();
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var deactivationBlockedReason = GetDeactivationBlockedReason(
                    user,
                    currentUserId,
                    ownerUserIds,
                    activeOwnerCount);
                var roleChangeBlockedReason = GetRoleChangeBlockedReason(
                    user,
                    currentUserId,
                    ownerUserIds,
                    activeOwnerCount);

                model.Add(new AccountListItemViewModel
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email ?? user.UserName ?? string.Empty,
                    Role = roles.SingleOrDefault() ?? "Unassigned",
                    IsActive = user.IsActive,
                    MustChangePassword = user.MustChangePassword,
                    LinkedTrainerName = trainersByUserId.GetValueOrDefault(user.Id),
                    CanDeactivate = user.IsActive && deactivationBlockedReason is null,
                    DeactivationBlockedReason = deactivationBlockedReason,
                    CanChangeRole = roleChangeBlockedReason is null,
                    RoleChangeBlockedReason = roleChangeBlockedReason
                });
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateEmail(string id, string email)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["ErrorMessage"] = "Select a valid account.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["ErrorMessage"] = "Enter an email address.";
                return RedirectToAction(nameof(Index));
            }

            email = NormalizeEmail(email);

            if (!new EmailAddressAttribute().IsValid(email))
            {
                TempData["ErrorMessage"] = "Enter a valid email address.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user is null)
            {
                TempData["ErrorMessage"] = "That account could not be found.";
                return RedirectToAction(nameof(Index));
            }

            if (string.Equals(user.Email, email, StringComparison.Ordinal))
            {
                TempData["StatusMessage"] = $"{user.FullName}'s email is already set to {email}.";
                return RedirectToAction(nameof(Index));
            }

            var existingUser = await _userManager.FindByEmailAsync(email);
            if (existingUser is not null && !string.Equals(existingUser.Id, user.Id, StringComparison.Ordinal))
            {
                TempData["ErrorMessage"] = "An account with this email already exists.";
                return RedirectToAction(nameof(Index));
            }

            user.Email = email;
            user.UserName = email;
            user.EmailConfirmed = false;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                TempData["ErrorMessage"] = string.Join(" ", updateResult.Errors.Select(error => error.Description));
                return RedirectToAction(nameof(Index));
            }

            TempData["StatusMessage"] = $"Updated {user.FullName}'s email.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRole(string id, string role)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["ErrorMessage"] = "Select a valid account.";
                return RedirectToAction(nameof(Index));
            }

            if (!AppRoles.IsSupported(role))
            {
                TempData["ErrorMessage"] = "Select a valid role.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user is null)
            {
                TempData["ErrorMessage"] = "That account could not be found.";
                return RedirectToAction(nameof(Index));
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            var currentRole = currentRoles.SingleOrDefault();
            if (string.Equals(currentRole, role, StringComparison.Ordinal))
            {
                TempData["StatusMessage"] = $"{user.FullName} is already assigned to {role}.";
                return RedirectToAction(nameof(Index));
            }

            var ownerUsers = await _userManager.GetUsersInRoleAsync(AppRoles.Owner);
            var ownerUserIds = ownerUsers
                .Select(ownerUser => ownerUser.Id)
                .ToHashSet(StringComparer.Ordinal);
            var activeOwnerCount = ownerUsers.Count(ownerUser => ownerUser.IsActive);

            var roleChangeBlockedReason = GetRoleChangeBlockedReason(
                user,
                _userManager.GetUserId(User),
                ownerUserIds,
                activeOwnerCount);

            if (roleChangeBlockedReason is not null)
            {
                TempData["ErrorMessage"] = GetRoleChangeBlockedMessage(roleChangeBlockedReason);
                return RedirectToAction(nameof(Index));
            }

            if (currentRoles.Count > 0)
            {
                var removeRolesResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeRolesResult.Succeeded)
                {
                    TempData["ErrorMessage"] = string.Join(" ", removeRolesResult.Errors.Select(error => error.Description));
                    return RedirectToAction(nameof(Index));
                }
            }

            var addRoleResult = await _userManager.AddToRoleAsync(user, role);
            if (!addRoleResult.Succeeded)
            {
                if (currentRoles.Count > 0)
                {
                    var rollbackResult = await _userManager.AddToRolesAsync(user, currentRoles);
                    if (!rollbackResult.Succeeded)
                    {
                        TempData["ErrorMessage"] =
                            "Unable to update the role and unable to restore previous roles. Review this account's role assignments before continuing.";
                        return RedirectToAction(nameof(Index));
                    }
                }

                TempData["ErrorMessage"] = string.Join(" ", addRoleResult.Errors.Select(error => error.Description));
                return RedirectToAction(nameof(Index));
            }

            TempData["StatusMessage"] = $"Updated {user.FullName}'s role to {role}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(string id, bool isActive)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                TempData["ErrorMessage"] = "Select a valid account.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user is null)
            {
                TempData["ErrorMessage"] = "That account could not be found.";
                return RedirectToAction(nameof(Index));
            }

            if (user.IsActive == isActive)
            {
                TempData["StatusMessage"] = $"{user.FullName} is already {(isActive ? "active" : "inactive")}.";
                return RedirectToAction(nameof(Index));
            }

            if (!isActive)
            {
                var ownerUsers = await _userManager.GetUsersInRoleAsync(AppRoles.Owner);
                var ownerUserIds = ownerUsers
                    .Select(ownerUser => ownerUser.Id)
                    .ToHashSet(StringComparer.Ordinal);
                var activeOwnerCount = ownerUsers.Count(ownerUser => ownerUser.IsActive);

                var deactivationBlockedReason = GetDeactivationBlockedReason(
                    user,
                    _userManager.GetUserId(User),
                    ownerUserIds,
                    activeOwnerCount);

                if (deactivationBlockedReason is not null)
                {
                    TempData["ErrorMessage"] = GetDeactivationBlockedMessage(deactivationBlockedReason);
                    return RedirectToAction(nameof(Index));
                }
            }

            user.IsActive = isActive;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                TempData["ErrorMessage"] = string.Join(" ", updateResult.Errors.Select(error => error.Description));
                return RedirectToAction(nameof(Index));
            }

            TempData["StatusMessage"] = $"{user.FullName} has been {(isActive ? "reactivated" : "deactivated")}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var model = new AccountCreateViewModel();
            await PopulateSelectionsAsync(model);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AccountCreateViewModel model)
        {
            await ValidateCreateModelAsync(model);

            if (!ModelState.IsValid)
            {
                await PopulateSelectionsAsync(model);
                return View(model);
            }

            var email = NormalizeEmail(model.Email);
            var user = new ApplicationUser
            {
                FirstName = model.FirstName.Trim(),
                LastName = model.LastName.Trim(),
                Email = email,
                UserName = email,
                EmailConfirmed = true,
                IsActive = model.IsActive,
                MustChangePassword = true
            };

            if (string.IsNullOrEmpty(model.TemporaryPassword))
            {
                ModelState.AddModelError(nameof(model.TemporaryPassword), "Temporary password is required.");
                await PopulateSelectionsAsync(model);
                return View(model);
            }

            user.PasswordHash = _passwordHasher.HashPassword(user, model.TemporaryPassword);

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                AddErrors(createResult);
                await PopulateSelectionsAsync(model);
                return View(model);
            }

            var roleResult = await _userManager.AddToRoleAsync(user, model.Role);
            if (!roleResult.Succeeded)
            {
                await _userManager.DeleteAsync(user);
                AddErrors(roleResult);
                await PopulateSelectionsAsync(model);
                return View(model);
            }

            if (model.TrainerId.HasValue)
            {
                var trainer = await _muContext.Trainers.FirstOrDefaultAsync(t => t.ID == model.TrainerId.Value);
                if (trainer is null || !trainer.IsActive || !string.IsNullOrWhiteSpace(trainer.ApplicationUserId))
                {
                    await _userManager.DeleteAsync(user);
                    ModelState.AddModelError(nameof(model.TrainerId), "That trainer record is no longer available to link.");
                    await PopulateSelectionsAsync(model);
                    return View(model);
                }

                trainer.ApplicationUserId = user.Id;

                try
                {
                    await _muContext.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    await _userManager.DeleteAsync(user);
                    ModelState.AddModelError(nameof(model.TrainerId), "Unable to link the trainer record. The account was not created.");
                    await PopulateSelectionsAsync(model);
                    return View(model);
                }
            }

            TempData["StatusMessage"] = $"Created {model.Role} account for {user.FullName}.";
            return RedirectToAction(nameof(Index));
        }

        private async Task ValidateCreateModelAsync(AccountCreateViewModel model)
        {
            if (!AppRoles.IsSupported(model.Role))
            {
                ModelState.AddModelError(nameof(model.Role), "Select a valid role.");
            }

            var email = NormalizeEmail(model.Email);
            if (!string.IsNullOrWhiteSpace(email))
            {
                var existingUser = await _userManager.FindByEmailAsync(email);
                if (existingUser is not null)
                {
                    ModelState.AddModelError(nameof(model.Email), "An account with this email already exists.");
                }
            }

            if (model.Role != AppRoles.Trainer && model.TrainerId.HasValue)
            {
                ModelState.AddModelError(nameof(model.TrainerId), "Only trainer accounts can link to a trainer record.");
            }

            if (model.TrainerId.HasValue)
            {
                var trainer = await _muContext.Trainers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.ID == model.TrainerId.Value);

                if (trainer is null || !trainer.IsActive)
                {
                    ModelState.AddModelError(nameof(model.TrainerId), "Select an active trainer record.");
                }
                else if (!string.IsNullOrWhiteSpace(trainer.ApplicationUserId))
                {
                    ModelState.AddModelError(nameof(model.TrainerId), "That trainer already has a linked login account.");
                }
            }
        }

        private async Task PopulateSelectionsAsync(AccountCreateViewModel model)
        {
            model.AvailableRoles = AppRoles.All
                .Select(role => new SelectListItem
                {
                    Value = role,
                    Text = role,
                    Selected = string.Equals(model.Role, role, StringComparison.Ordinal)
                })
                .ToList();

            model.AvailableTrainers = await _muContext.Trainers
                .AsNoTracking()
                .Where(trainer =>
                    trainer.IsActive &&
                    (trainer.ApplicationUserId == null || trainer.ID == model.TrainerId))
                .OrderBy(trainer => trainer.LastName)
                .ThenBy(trainer => trainer.FirstName)
                .Select(trainer => new SelectListItem
                {
                    Value = trainer.ID.ToString(),
                    Text = string.IsNullOrWhiteSpace(trainer.Email)
                        ? trainer.TrainerName
                        : $"{trainer.TrainerName} ({trainer.Email})",
                    Selected = model.TrainerId.HasValue && trainer.ID == model.TrainerId.Value
                })
                .ToListAsync();
        }

        private string? GetDeactivationBlockedReason(
            ApplicationUser user,
            string? currentUserId,
            ISet<string> ownerUserIds,
            int activeOwnerCount)
        {
            if (!user.IsActive)
            {
                return null;
            }

            if (string.Equals(user.Id, currentUserId, StringComparison.Ordinal))
            {
                return CurrentAccountBlockedReason;
            }

            if (IsBootstrapOwner(user))
            {
                return BootstrapOwnerBlockedReason;
            }

            if (ownerUserIds.Contains(user.Id) && activeOwnerCount <= 1)
            {
                return OnlyActiveOwnerBlockedReason;
            }

            return null;
        }

        private string GetDeactivationBlockedMessage(string blockedReason) =>
            blockedReason switch
            {
                CurrentAccountBlockedReason =>
                    "You can't deactivate the account you're currently signed in with.",
                BootstrapOwnerBlockedReason =>
                    "You can't deactivate the configured bootstrap owner account while the BootstrapOwner settings are enabled.",
                OnlyActiveOwnerBlockedReason =>
                    "You can't deactivate the only active owner account.",
                _ => "That account can't be deactivated."
            };

        private string? GetRoleChangeBlockedReason(
            ApplicationUser user,
            string? currentUserId,
            ISet<string> ownerUserIds,
            int activeOwnerCount)
        {
            if (string.Equals(user.Id, currentUserId, StringComparison.Ordinal))
            {
                return CurrentAccountBlockedReason;
            }

            if (IsBootstrapOwner(user))
            {
                return BootstrapOwnerBlockedReason;
            }

            if (ownerUserIds.Contains(user.Id) && activeOwnerCount <= 1)
            {
                return OnlyActiveOwnerBlockedReason;
            }

            return null;
        }

        private string GetRoleChangeBlockedMessage(string blockedReason) =>
            blockedReason switch
            {
                CurrentAccountBlockedReason =>
                    "You can't change the role for the account you're currently signed in with.",
                BootstrapOwnerBlockedReason =>
                    "You can't change the configured bootstrap owner's role while the BootstrapOwner settings are enabled.",
                OnlyActiveOwnerBlockedReason =>
                    "You can't change the role of the only active owner account.",
                _ => "That account's role can't be changed."
            };

        private bool IsBootstrapOwner(ApplicationUser user) =>
            _bootstrapOwnerOptions.IsConfigured &&
            string.Equals(
                user.Email,
                NormalizeEmail(_bootstrapOwnerOptions.Email!),
                StringComparison.Ordinal);

        private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

        private void AddErrors(IdentityResult identityResult)
        {
            foreach (var error in identityResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
    }
}
