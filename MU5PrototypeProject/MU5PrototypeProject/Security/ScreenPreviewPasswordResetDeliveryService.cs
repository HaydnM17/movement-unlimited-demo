namespace MU5PrototypeProject.Security
{
    public class ScreenPreviewPasswordResetDeliveryService : IPasswordResetDeliveryService
    {
        public Task<PasswordResetDeliveryResult> DeliverPasswordResetAsync(
            PasswordResetDeliveryRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            var previewResetUrl = string.IsNullOrWhiteSpace(request.ActualResetUrl)
                ? request.PreviewResetUrl
                : request.ActualResetUrl;

            return Task.FromResult(new PasswordResetDeliveryResult
            {
                PreviewResetUrl = previewResetUrl
            });
        }
    }
}
