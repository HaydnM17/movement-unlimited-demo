namespace MU5PrototypeProject.Security
{
    public class PasswordResetDeliveryResult
    {
        public string? PreviewResetUrl { get; init; }

        public bool HasPreview => !string.IsNullOrWhiteSpace(PreviewResetUrl);
    }
}
