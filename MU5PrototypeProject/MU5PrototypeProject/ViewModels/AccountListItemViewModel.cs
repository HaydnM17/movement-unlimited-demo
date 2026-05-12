namespace MU5PrototypeProject.Models.ViewModels
{
    public class AccountListItemViewModel
    {
        public string Id { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public bool MustChangePassword { get; set; }

        public string? LinkedTrainerName { get; set; }

        public bool CanDeactivate { get; set; }

        public string? DeactivationBlockedReason { get; set; }

        public bool CanChangeRole { get; set; }

        public string? RoleChangeBlockedReason { get; set; }
    }
}
