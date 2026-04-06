namespace Backend.Interfaces
{
    public interface IPaymentWebhookService
    {
        Task HandleWebhookAsync(string paymentId);
    }
}