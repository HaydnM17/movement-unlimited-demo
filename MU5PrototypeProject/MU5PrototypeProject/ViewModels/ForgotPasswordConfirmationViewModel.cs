namespace MU5PrototypeProject.Models.ViewModels
{
    public class ForgotPasswordConfirmationViewModel
    {
        public string? PreviewResetUrl { get; set; }

        public bool ShowPreviewLink => !string.IsNullOrWhiteSpace(PreviewResetUrl);
    }
}
