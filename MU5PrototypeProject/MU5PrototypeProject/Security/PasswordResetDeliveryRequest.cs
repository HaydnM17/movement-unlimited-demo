namespace MU5PrototypeProject.Security
{
    public class PasswordResetDeliveryRequest
    {
        public string Email { get; set; } = string.Empty;

        public string PreviewResetUrl { get; set; } = string.Empty;

        public string? ActualResetUrl { get; set; }
    }
}
