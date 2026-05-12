namespace MU5PrototypeProject.Security
{
    public interface IPasswordResetDeliveryService
    {
        Task<PasswordResetDeliveryResult> DeliverPasswordResetAsync(
            PasswordResetDeliveryRequest request,
            CancellationToken cancellationToken = default);
    }
}
