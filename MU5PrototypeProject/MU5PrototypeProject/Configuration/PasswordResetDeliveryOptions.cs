namespace MU5PrototypeProject.Configuration
{
    public class PasswordResetDeliveryOptions
    {
        public const string SectionName = "PasswordResetDelivery";

        public PasswordResetDeliveryMode Mode { get; set; } = PasswordResetDeliveryMode.Disabled;
    }

    public enum PasswordResetDeliveryMode
    {
        Disabled = 0,
        ScreenPreview = 1,
        Email = 2
    }
}
