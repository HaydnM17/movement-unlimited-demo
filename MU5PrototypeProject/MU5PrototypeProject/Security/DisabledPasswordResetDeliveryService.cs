namespace MU5PrototypeProject.Security
{
    public class DisabledPasswordResetDeliveryService : IPasswordResetDeliveryService
    {
        public Task<PasswordResetDeliveryResult> DeliverPasswordResetAsync(
            PasswordResetDeliveryRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            return Task.FromResult(new PasswordResetDeliveryResult());
        }
    }
}
