using Backend.Database;
using Backend.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Backend.Services.MailSubscriptionServices;

/// <summary>
/// Defines the contract for a mail subscription service that manages user subscriptions to mailing lists. The IMailSubscriptionService interface provides a method for updating a user's subscription status based on their email address and a bitmap representing their subscription preferences. Implementations of this interface are responsible for integrating with external mailing list providers (such as MailChimp) to ensure that users are subscribed or unsubscribed according to their preferences, while also handling any necessary data transformations and API interactions required by the specific mailing list service being used.
/// </summary>
public class MailChimpSubscriptionService : IMailSubscriptionService
{
    private readonly ILogger<MailChimpSubscriptionService> _logger;
    private readonly HttpClient _httpClient;
    private readonly PostgresDbContext _context;
    private string ListKey => _context.Settings.Find("MailchimpListKey")?.Value ?? string.Empty;
    private bool IsEnabled => _context.Settings.Find("MailSubscriptionService")?.Value?.Trim().Equals("MAILCHIMP", StringComparison.OrdinalIgnoreCase) ?? false;

    /// <summary>
    /// Initializes a new instance of the MailChimpSubscriptionService class with the specified logger, HTTP client, and database context. The constructor sets up the necessary dependencies for the service to function correctly, allowing it to log important events and errors, make HTTP requests to the MailChimp API, and interact with the database to retrieve mailing list definitions. This setup is essential for ensuring that the service can effectively manage mail subscriptions by communicating with MailChimp and maintaining accurate subscription data based on user preferences stored in the database.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="context">The database context.</param>
    public MailChimpSubscriptionService(
        ILogger<MailChimpSubscriptionService> logger,
        HttpClient httpClient,
        PostgresDbContext context)
    {
        _logger = logger;
        _httpClient = httpClient;
        _context = context;
    }

    private void ConfigureHttpClient()
    {
        var apiKey = _context.Settings.Find("MailchimpApiKey")?.Value ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(apiKey) && apiKey.Contains('-'))
        {
            var dataCenter = apiKey.Split('-')[1];
            _httpClient.BaseAddress = new Uri($"https://{dataCenter}.api.mailchimp.com/3.0/");
            var authValue = Convert.ToBase64String(Encoding.ASCII.GetBytes($"anyuser:{apiKey}"));
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authValue);
        }
    }

    /// <summary>
    /// Updates the mail subscription status for a given email address based on the provided mail subscription bitmap. The UpdateSubscriptionAsync method checks if the mail subscription service is enabled, calculates the MD5 hash of the email address for MailChimp API compatibility, and then either updates or deletes the member's subscription in MailChimp based on whether the mailSubscription bitmap is zero (indicating unsubscription) or not. The method also includes logging to track subscription updates and removals for monitoring and debugging purposes.
    /// </summary>
    /// <param name="email">The email address of the user.</param>
    /// <param name="mailSubscription">The bitmap representing the user's subscription preferences.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task UpdateSubscriptionAsync(string email, uint mailSubscription, CancellationToken ct)
    {
        if (!IsEnabled)
        {
            _logger.LogInformation("MailChimp subscription service is disabled. Skipping update for {Email}.", email);
            return;
        }

        ConfigureHttpClient();

        var emailHash = CalculateMd5Hash(email);

        if (mailSubscription == 0)
        {
            await DeleteMemberAsync(emailHash, ct);
            return;
        }

        await UpsertMemberAsync(email, emailHash, mailSubscription, ct);
    }

    private async Task UpsertMemberAsync(string email, string emailHash, uint mailSubscription, CancellationToken ct)
    {
        var definitions = await _context.Mailinglists
            .ToListAsync(ct);

        var interests = new Dictionary<string, bool>();
        foreach (var def in definitions)
        {
            bool isSubscribed = (mailSubscription & def.BitValue) != 0;
            interests.Add(def.ServiceId, isSubscribed);
        }

        var payload = new
        {
            email_address = email,
            status_if_new = "subscribed",
            status = "subscribed",
            interests = interests
        };

        var response = await _httpClient.PutAsJsonAsync($"lists/{ListKey}/members/{emailHash}", payload, ct);
        response.EnsureSuccessStatusCode();

        _logger.LogInformation("Subscriptions for {Email} updated via dynamic bitmap.", email);
    }

    private async Task DeleteMemberAsync(string emailHash, CancellationToken ct)
    {
        var response = await _httpClient.DeleteAsync($"lists/{ListKey}/members/{emailHash}", ct);

        if (response.StatusCode != System.Net.HttpStatusCode.NotFound)
        {
            response.EnsureSuccessStatusCode();
        }

        _logger.LogInformation("User with hash {Hash} removed from Mailchimp.", emailHash);
    }

    private string CalculateMd5Hash(string input)
    {
        byte[] hashBytes = MD5.HashData(Encoding.UTF8.GetBytes(input.ToLower().Trim()));
        return Convert.ToHexString(hashBytes).ToLower();
    }
}
