using Backend.Database;
using Backend.Interfaces;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace Backend.Services.MailSubscriptionServices;

/// <summary>
/// Implements <see cref="IMailSubscriptionService"/> against the Mailchimp API. Mailchimp is treated as the sole source of truth for mailing lists and member subscriptions - nothing is mirrored locally.
/// </summary>
public class MailChimpSubscriptionService : IMailSubscriptionService, IMailUpdateService
{
    private readonly ILogger<MailChimpSubscriptionService> _logger;
    private readonly HttpClient _httpClient;
    private readonly PostgresDbContext _context;
    private string ListKey => _context.Settings.Find("MailchimpListKey")?.Value ?? string.Empty;
    private bool IsEnabled => _context.Settings.Find("MailSubscriptionService")?.Value?.Trim().Equals("MAILCHIMP", StringComparison.OrdinalIgnoreCase) ?? false;

    /// <summary>
    /// Initializes a new instance of the MailChimpSubscriptionService class with the specified logger, HTTP client, and database context. The constructor sets up the necessary dependencies for the service to function correctly, allowing it to log important events and errors, make HTTP requests to the MailChimp API, and interact with the database to retrieve Mailchimp settings.
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
        // Several public methods on this service call one another (e.g.
        // GetMemberMailinglistsAsync calls GetAvailableMailinglistsAsync, both of which call
        // this), so this can run more than once against the same HttpClient instance within a
        // single request. HttpClient throws once a request has actually been sent if you try to
        // mutate BaseAddress/DefaultRequestHeaders again, so make this a no-op after the first
        // successful configuration instead of reconfiguring (and re-reading Settings) every time.
        if (_httpClient.BaseAddress != null)
        {
            return;
        }

        var apiKey = _context.Settings.Find("MailchimpApiKey")?.Value ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(apiKey) && apiKey.Contains('-'))
        {
            var dataCenter = apiKey.Split('-')[1];
            _httpClient.BaseAddress = new Uri($"https://{dataCenter}.api.mailchimp.com/3.0/");
            var authValue = Convert.ToBase64String(Encoding.ASCII.GetBytes($"anyuser:{apiKey}"));
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authValue);
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<MailinglistDto>> GetAvailableMailinglistsAsync(CancellationToken ct)
    {
        if (!IsEnabled)
        {
            _logger.LogInformation("MailChimp subscription service is disabled. Returning no available mailing lists.");
            return [];
        }

        ConfigureHttpClient();

        var categoriesResponse = await _httpClient.GetFromJsonAsync<InterestCategoriesResponse>(
            $"lists/{ListKey}/interest-categories?count=1000", ct);

        var lists = new List<MailinglistDto>();
        foreach (var category in categoriesResponse?.Categories ?? [])
        {
            var interestsResponse = await _httpClient.GetFromJsonAsync<InterestsResponse>(
                $"lists/{ListKey}/interest-categories/{category.Id}/interests?count=1000", ct);

            foreach (var interest in interestsResponse?.Interests ?? [])
            {
                lists.Add(new MailinglistDto(interest.Id, interest.Name));
            }
        }

        return lists;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<MemberMailinglistDto>> GetMemberMailinglistsAsync(string email, CancellationToken ct)
    {
        if (!IsEnabled)
        {
            _logger.LogInformation("MailChimp subscription service is disabled. Returning no member mailing lists for {Email}.", email);
            return [];
        }

        var availableLists = (await GetAvailableMailinglistsAsync(ct)).ToList();

        ConfigureHttpClient();

        var emailHash = CalculateMd5Hash(email);
        var response = await _httpClient.GetAsync($"lists/{ListKey}/members/{emailHash}?fields=status,interests", ct);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return availableLists.Select(l => new MemberMailinglistDto(l.Id, l.Name, false));
        }

        response.EnsureSuccessStatusCode();

        var member = await response.Content.ReadFromJsonAsync<MailchimpMemberResponse>(ct);

        // Mailchimp does not clear the interests dict when a member unsubscribes/is cleaned - it
        // just retains their last known preferences while flipping status. Trusting interests
        // directly for a non-subscribed member would resurrect stale opt-ins.
        if (member == null || !string.Equals(member.Status, "subscribed", StringComparison.OrdinalIgnoreCase))
        {
            return availableLists.Select(l => new MemberMailinglistDto(l.Id, l.Name, false));
        }

        return availableLists.Select(l => new MemberMailinglistDto(
            l.Id,
            l.Name,
            member.Interests != null && member.Interests.TryGetValue(l.Id, out var subscribed) && subscribed));
    }

    /// <inheritdoc />
    public async Task UpdateMemberSubscriptionsAsync(string email, IEnumerable<string> subscribedListIds, CancellationToken ct)
    {
        if (!IsEnabled)
        {
            _logger.LogInformation("MailChimp subscription service is disabled. Skipping update for {Email}.", email);
            return;
        }

        var subscribedIdSet = subscribedListIds.ToHashSet();

        if (subscribedIdSet.Count == 0)
        {
            await DeleteMemberAsync(email, ct);
            return;
        }

        var availableLists = await GetAvailableMailinglistsAsync(ct);

        ConfigureHttpClient();

        var emailHash = CalculateMd5Hash(email);

        var interests = availableLists.ToDictionary(l => l.Id, l => subscribedIdSet.Contains(l.Id));

        var payload = new
        {
            email_address = email,
            status_if_new = "subscribed",
            status = "subscribed",
            interests
        };

        var response = await _httpClient.PutAsJsonAsync($"lists/{ListKey}/members/{emailHash}", payload, ct);
        response.EnsureSuccessStatusCode();

        _logger.LogInformation("Subscriptions for {Email} updated.", email);
    }

    /// <inheritdoc />
    public async Task DeleteMemberAsync(string email, CancellationToken ct)
    {
        if (!IsEnabled)
        {
            _logger.LogInformation("MailChimp subscription service is disabled. Skipping delete for {Email}.", email);
            return;
        }

        ConfigureHttpClient();

        var emailHash = CalculateMd5Hash(email);
        var response = await _httpClient.PostAsync(
            $"lists/{ListKey}/members/{emailHash}/actions/delete-permanent",
            null,
            ct);

        if (response.StatusCode != System.Net.HttpStatusCode.NotFound)
        {
            response.EnsureSuccessStatusCode();
        }

        _logger.LogInformation("Member {Email} removed from Mailchimp.", email);
    }

    /// <inheritdoc />
    public async Task MigrateEmailAsync(string oldEmail, string newEmail, CancellationToken ct)
    {
        if (!IsEnabled)
        {
            _logger.LogInformation("MailChimp subscription service is disabled. Skipping email migration from {OldEmail} to {NewEmail}.", oldEmail, newEmail);
            return;
        }

        var subscribedIds = (await GetMemberMailinglistsAsync(oldEmail, ct))
            .Where(l => l.Subscribed)
            .Select(l => l.Id);

        await UpdateMemberSubscriptionsAsync(newEmail, subscribedIds, ct);
        await DeleteMemberAsync(oldEmail, ct);

        _logger.LogInformation("Migrated Mailchimp subscriptions from {OldEmail} to {NewEmail}.", oldEmail, newEmail);
    }

    /// <inheritdoc />
    public Task SyncMailAsync(string oldEmail, string newEmail, CancellationToken ct)
    {
        return MigrateEmailAsync(oldEmail, newEmail, ct);
    }

    /// <inheritdoc />
    public Task DeleteMailAsync(string email, CancellationToken ct)
    {
        return DeleteMemberAsync(email, ct);
    }

    private string CalculateMd5Hash(string input)
    {
        byte[] hashBytes = MD5.HashData(Encoding.UTF8.GetBytes(input.ToLower().Trim()));
        return Convert.ToHexString(hashBytes).ToLower();
    }

    private class InterestCategoriesResponse
    {
        [JsonPropertyName("categories")]
        public List<InterestCategory> Categories { get; set; } = [];
    }

    private class InterestCategory
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;
    }

    private class InterestsResponse
    {
        [JsonPropertyName("interests")]
        public List<Interest> Interests { get; set; } = [];
    }

    private class Interest
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    private class MailchimpMemberResponse
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("interests")]
        public Dictionary<string, bool>? Interests { get; set; }
    }
}
