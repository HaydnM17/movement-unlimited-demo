namespace MU5PrototypeProject.Security
{
    public static class AppRoles
    {
        public const string Owner = "Owner";
        public const string Administration = "Administration";
        public const string Trainer = "Trainer";

        public static readonly string[] All =
        [
            Owner,
            Administration,
            Trainer
        ];

        public static bool IsSupported(string? role) =>
            !string.IsNullOrWhiteSpace(role) &&
            All.Contains(role, StringComparer.Ordinal);
    }
}
