using Backend.Database;
using Backend.Models.Domain;
using Mollie.Api.Client;
using Mollie.Api.Client.Abstract;
using Mollie.Api.Models;
using Mollie.Api.Models.Payment.Request;
using Mollie.Api.Models.Payment.Response;
using MolliePaymentStatus = Mollie.Api.Models.Payment.PaymentStatus;

namespace Backend.Services.PaymentServices;

/// <summary>
/// Implements payment-service related operations for Mollie.
/// </summary>
public class MollieService(PostgresDbContext db, ILogger<MollieService> logger, Func<IPaymentClient>? clientFactory = null) : AbstractPaymentService(db, logger)
{
    private readonly Func<IPaymentClient>? _clientFactory = clientFactory;
    /// <inheritdoc/>
    public override async Task<GetPaymentResponse> GetPaymentAsync(string paymentId)
    {
        IPaymentClient mollieClient = await GetMollieClientAsync();
        PaymentResponse response = await mollieClient.GetPaymentAsync(paymentId);
        return new GetPaymentResponse
        (
            response.Id,
            response.Status switch
            {
                MolliePaymentStatus.Paid => PaymentStatus.Paid,
                MolliePaymentStatus.Open => PaymentStatus.Pending,
                MolliePaymentStatus.Pending => PaymentStatus.Pending,
                MolliePaymentStatus.Canceled => PaymentStatus.Failed,
                MolliePaymentStatus.Failed => PaymentStatus.Failed,
                MolliePaymentStatus.Expired => PaymentStatus.Failed,
                MolliePaymentStatus.Authorized => PaymentStatus.Pending,
                _ => throw new Exception($"Unknown payment status: {response.Status}")
            },
            response.PaidAt
        );
    }

    /// <inheritdoc/>
    public override async Task CancelPaymentAsync(string paymentId)
    {
        IPaymentClient mollieClient = await GetMollieClientAsync();
        await mollieClient.CancelPaymentAsync(paymentId);
    }

    /// <inheritdoc/>
    public override async Task<CreatePaymentResponse> CreatePaymentAsync(decimal amount, string description, string? redirectUrl = null, string? webhookUrl = null, string? metadata = null)
    {
        IPaymentClient mollieClient = await GetMollieClientAsync();
        PaymentRequest request = new PaymentRequest
        {
            Amount = new Amount(Currency.EUR, amount),
            Description = description,
            RedirectUrl = redirectUrl,
            WebhookUrl = webhookUrl,
            Metadata = metadata
        };

        PaymentResponse response = await mollieClient.CreatePaymentAsync(request);

        if (response.Links.Checkout == null)
        {
            throw new InvalidOperationException("Mollie response did not contain a checkout link.");
        }

        return new CreatePaymentResponse(response.Id, response.Links.Checkout.Href);
    }

    private async Task<IPaymentClient> GetMollieClientAsync()
    {
        if (_clientFactory != null)
        {
            return _clientFactory();
        }

        Setting? apiKey = await _db.Settings.FindAsync("MollieApiKey");

        if (string.IsNullOrWhiteSpace(apiKey?.Value))
        {
            throw new InvalidOperationException("Mollie API key is niet geconfigureerd in de instellingen.");
        }

        return new PaymentClient(apiKey.Value);
    }
}
