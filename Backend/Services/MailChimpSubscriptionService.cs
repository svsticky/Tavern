using System.Security.Cryptography;
using System.Text;
using Backend.Database;
using Microsoft.EntityFrameworkCore;

namespace Backend.Interfaces;

public class MailChimpSubscriptionService : IMailSubscriptionService
{
    private readonly ILogger<MailChimpSubscriptionService> _logger;
    private readonly HttpClient _httpClient;
    private readonly PostgresDbContext _context; 
    private readonly string _listKey = Environment.GetEnvironmentVariable("MAILCHIMP_LIST_KEY") ?? string.Empty;
    private readonly bool _isEnabled = Environment.GetEnvironmentVariable("MAIL_SUBSCRIPTION_SERVICE") == "MAILCHIMP";

    public MailChimpSubscriptionService(
        ILogger<MailChimpSubscriptionService> logger, 
        HttpClient httpClient,
        PostgresDbContext context)
    {
        _logger = logger;
        _httpClient = httpClient;
        _context = context;
    }

    public async Task UpdateSubscriptionAsync(string email, uint mailSubscription, CancellationToken ct)
    {
        if(!_isEnabled) 
        {
            _logger.LogInformation("MailChimp subscription service is disabled. Skipping update for {Email}.", email);
            return;
        }

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

        var response = await _httpClient.PutAsJsonAsync($"lists/{_listKey}/members/{emailHash}", payload, ct);
        response.EnsureSuccessStatusCode();
        
        _logger.LogInformation("Subscriptions for {Email} updated via dynamic bitmap.", email);
    }

    private async Task DeleteMemberAsync(string emailHash, CancellationToken ct)
    {
        var response = await _httpClient.DeleteAsync($"lists/{_listKey}/members/{emailHash}", ct);

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