using Backend.Database;
using Backend.Interfaces;
using Mollie.Api.Client.Abstract;
using Mollie.Api.Models;
using Mollie.Api.Models.Payment.Request;
using Mollie.Api.Models.Payment.Response;

namespace Backend.Services.PaymentServices;

/// <summary>
/// Implements payment-service related operations for Mollie.
/// </summary>
public class MollieService(IPaymentClient mollieClient, PostgresDbContext db, ILogger<MollieService> logger) : AbstractPaymentService(db, logger)
{
    /// <inheritdoc/>
    public override async Task<GetPaymentResponse> GetPaymentAsync(string paymentId)
    {
        PaymentResponse response = await mollieClient.GetPaymentAsync(paymentId);
        return new GetPaymentResponse
        (
            response.Id,
            response.Status switch
            {
                "paid" => PaymentStatus.Paid,
                "open" => PaymentStatus.Pending,
                "pending" => PaymentStatus.Pending,
                "cancelled" => PaymentStatus.Failed,
                "failed" => PaymentStatus.Failed,
                "expired" => PaymentStatus.Failed,
                _ => throw new Exception($"Unknown payment status: {response.Status}")
            },
            response.PaidAt
        );
    }

    /// <inheritdoc/>
    public override async Task CancelPaymentAsync(string paymentId)
    {
        await mollieClient.CancelPaymentAsync(paymentId);
    }

    /// <inheritdoc/>
    public override async Task<CreatePaymentResponse> CreatePaymentAsync(decimal amount, string description, string? redirectUrl = null, string? webhookUrl = null, string? metadata = null)
    {
        PaymentRequest request = new PaymentRequest
        {
            Amount = new Amount(Currency.EUR, 7.50m),
            Description = description,
            RedirectUrl = redirectUrl,
            WebhookUrl = webhookUrl,
            Metadata = metadata
        };

        PaymentResponse response = await mollieClient.CreatePaymentAsync(request);

        if(response.Links.Checkout == null)
        {
            throw new InvalidOperationException("Mollie response did not contain a checkout link.");
        }

        return new CreatePaymentResponse(response.Id, response.Links.Checkout.Href);
    }
}