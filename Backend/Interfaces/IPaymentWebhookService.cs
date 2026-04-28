namespace Backend.Interfaces
{
    /// <summary>
    /// Defines the contract for handling inbound payment-provider webhooks.
    /// </summary>
    public interface IPaymentWebhookService
    {
        /// <summary>
        /// Handles an incoming payment webhook by provider payment identifier.
        /// </summary>
        /// <param name="paymentId">The provider payment ID from the webhook payload.</param>
        Task HandleWebhookAsync(string paymentId);
    }
}
