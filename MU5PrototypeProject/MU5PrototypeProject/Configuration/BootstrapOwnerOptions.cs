namespace MU5PrototypeProject.Configuration
{
    public class BootstrapOwnerOptions
    {
        public const string SectionName = "BootstrapOwner";

        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Password { get; set; }

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(FirstName) &&
            !string.IsNullOrWhiteSpace(LastName) &&
            !string.IsNullOrWhiteSpace(Email) &&
            !string.IsNullOrWhiteSpace(Password);
    }
}
