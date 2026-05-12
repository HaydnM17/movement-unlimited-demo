using MU5PrototypeProject.Security;

namespace MU5PrototypeProject.Configuration
{
    public class DemoAccountOptions
    {
        public const string SectionName = "DemoAccount";

        public bool Enabled { get; set; }

        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public string? Email { get; set; }

        public string? Role { get; set; } = AppRoles.Administration;

        public bool IsConfigured =>
            Enabled &&
            !string.IsNullOrWhiteSpace(FirstName) &&
            !string.IsNullOrWhiteSpace(LastName) &&
            !string.IsNullOrWhiteSpace(Email) &&
            AppRoles.IsSupported(Role);
    }
}
