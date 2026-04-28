using System.Security.Cryptography;
using System.Text;
using Backend.Models;

namespace Backend.Interfaces;

public class MailChimpSubscriptionService : IMailSubscriptionService
{
    private readonly ILogger<MailChimpSubscriptionService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _listKey = Environment.GetEnvironmentVariable("MAILCHIMP_LIST_KEY") ?? string.Empty;
    private readonly bool _isEnabled = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MAIL_SUBSCRIPTION_SERVICE")) && Environment.GetEnvironmentVariable("MAIL_SUBSCRIPTION_SERVICE") == "MAILCHIMP";

    public MailChimpSubscriptionService(ILogger<MailChimpSubscriptionService> logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
    }

    public async Task UpdateSubscriptionAsync(string email, MailSubscriptions mailSubscription, CancellationToken ct)
    {
        if(!_isEnabled) 
        {
            _logger.LogInformation("MailChimp subscription service is disabled. Skipping update for {Email}.", email);
            return;
        }

        var emailHash = CalculateMd5Hash(email);

        if (mailSubscription == MailSubscriptions.None)
        {
            await DeleteMemberAsync(emailHash, ct);
            return;
        }

        await UpsertMemberAsync(email, emailHash, mailSubscription, ct);
    }

    private async Task UpsertMemberAsync(string email, string emailHash, MailSubscriptions mailSubscription, CancellationToken ct)
    {
        var payload = new
        {
            email_address = email,
            status_if_new = "subscribed",
            status = "subscribed",
            interests = new Dictionary<string, bool>
            {
                { "ID_MEETINGS", mailSubscription.HasFlag(MailSubscriptions.GeneralMemberMeetings) },
                { "ID_COMPANY", mailSubscription.HasFlag(MailSubscriptions.CompanyMails) },
                { "ID_MONDAY", mailSubscription.HasFlag(MailSubscriptions.MondayMorningMails) },
                { "ID_LECTURES", mailSubscription.HasFlag(MailSubscriptions.LecturesAndWorkshops) },
                { "ID_TEACHERS", mailSubscription.HasFlag(MailSubscriptions.TeacherMails) }
            }
        };

        var response = await _httpClient.PutAsJsonAsync($"lists/{_listKey}/members/{emailHash}", payload, ct);
        response.EnsureSuccessStatusCode();
        
        _logger.LogInformation("Subscriptions for {Email} updated.", email);
    }

    private async Task DeleteMemberAsync(string emailHash, CancellationToken ct)
    {
        var response = await _httpClient.DeleteAsync($"lists/{_listKey}/members/{emailHash}", ct);

        if (response.StatusCode != System.Net.HttpStatusCode.NotFound)
        {
            response.EnsureSuccessStatusCode();
        }

        _logger.LogInformation("User with hash {Hash} removed from Mailchimp due to empty subscriptions.", emailHash);
    }

    private string CalculateMd5Hash(string input)
    {
        using MD5 md5 = MD5.Create();
        byte[] inputBytes = Encoding.UTF8.GetBytes(input.ToLower().Trim());
        byte[] hashBytes = md5.ComputeHash(inputBytes);

        return Convert.ToHexString(hashBytes).ToLower();
    }
}