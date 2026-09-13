using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Backend.Services.OutboxWorkers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Backend.Services.MailSubscriptionServices;

/// <summary>
/// Implements <see cref="IMailSubscriptionService"/> against the Mailchimp API. Mailchimp is treated as the sole source of truth for mailing lists and member subscriptions - nothing is mirrored locally.
/// Also implements <see cref="INameChangedListener"/> and <see cref="IMailChangedListener"/> explicitly.
/// </summary>
public class MailChimpSubscriptionService : IMailSubscriptionService
{
    private readonly ILogger<MailChimpSubscriptionService> _logger;
    private readonly HttpClient _httpClient;
    private readonly PostgresDbContext _context;
    private readonly MailSubscriptionOutboxWorker _mailSubscriptionOutboxWorker;
    private string ListKey => _context.Settings.Find("MailchimpListKey")?.Value ?? string.Empty;
    private bool IsEnabled => _context.Settings.Find("MailSubscriptionService")?.Value?.Trim().Equals("MAILCHIMP", StringComparison.OrdinalIgnoreCase) ?? false;

    /// <inheritdoc />
    bool INameChangedListener.IsEnabled => IsEnabled;

    /// <inheritdoc />
    bool IMailChangedListener.IsEnabled => IsEnabled;

    /// <inheritdoc />
    void INameChangedListener.OnNameChanged(Member member, PostgresDbContext db) =>
        _mailSubscriptionOutboxWorker.EnqueueUpdateNameTask(member.Email, member.FirstName, member.LastName, db);

    /// <inheritdoc />
    void IMailChangedListener.OnMailChanged(Guid memberId, string oldEmail, string newEmail, PostgresDbContext db)
    {
        // Carries the name over so an email-only change doesn't leave the migrated record blank.
        var member = db.Members.Find(memberId);
        _mailSubscriptionOutboxWorker.EnqueueMigrateEmailTask(oldEmail, newEmail, db, member?.FirstName, member?.LastName);
    }

    /// <summary>
    /// Initializes a new instance of the MailChimpSubscriptionService class with the specified logger, HTTP client, and database context. The constructor sets up the necessary dependencies for the service to function correctly, allowing it to log important events and errors, make HTTP requests to the MailChimp API, and interact with the database to retrieve Mailchimp settings.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="httpClient">The HTTP client.</param>
    /// <param name="context">The database context.</param>
    /// <param name="mailSubscriptionOutboxWorker">Used to queue outbox tasks from listener notifications.</param>
    public MailChimpSubscriptionService(
        ILogger<MailChimpSubscriptionService> logger,
        HttpClient httpClient,
        PostgresDbContext context,
        MailSubscriptionOutboxWorker mailSubscriptionOutboxWorker)
    {
        _logger = logger;
        _httpClient = httpClient;
        _context = context;
        _mailSubscriptionOutboxWorker = mailSubscriptionOutboxWorker;
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
    public async Task UpdateMemberSubscriptionsAsync(string email, IEnumerable<string> subscribedListIds, CancellationToken ct, string? firstName = null, string? lastName = null)
    {
        if (!IsEnabled)
        {
            _logger.LogInformation("MailChimp subscription service is disabled. Skipping update for {Email}.", email);
            return;
        }

        var subscribedIdSet = subscribedListIds.ToHashSet();

        if (subscribedIdSet.Count == 0)
        {
            await ArchiveMemberAsync(email, ct);
            return;
        }

        var availableLists = await GetAvailableMailinglistsAsync(ct);

        ConfigureHttpClient();

        var emailHash = CalculateMd5Hash(email);

        var interests = availableLists.ToDictionary(l => l.Id, l => subscribedIdSet.Contains(l.Id));

        // Dictionary so merge_fields can be added conditionally - a plain anonymous object would
        // let the default naming policy lowercase FIRSTNAME/LASTNAME, which are case-sensitive at
        // Mailchimp (see UpdateMemberNameAsync for the actual tag names on this audience).
        var payload = new Dictionary<string, object>
        {
            ["email_address"] = email,
            ["status_if_new"] = "subscribed",
            ["status"] = "subscribed",
            ["interests"] = interests
        };

        if (firstName != null && lastName != null)
        {
            payload["merge_fields"] = new Dictionary<string, string> { ["FIRSTNAME"] = firstName, ["LASTNAME"] = lastName };
        }

        var response = await _httpClient.PutAsJsonAsync($"lists/{ListKey}/members/{emailHash}", payload, ct);
        await EnsureSuccessOrLogAsync(response, email, ct);

        _logger.LogInformation("Subscriptions for {Email} updated.", email);
    }

    /// <summary>
    /// Archives a member (Mailchimp's plain member DELETE) rather than permanently forgetting them -
    /// used when a member unsubscribes from every list, so re-checking a list later can add them
    /// back via the API. Unlike <see cref="DeleteMemberAsync"/>, this does not erase their record,
    /// but Mailchimp does not treat it as a "forgotten" email either.
    /// </summary>
    private async Task ArchiveMemberAsync(string email, CancellationToken ct)
    {
        ConfigureHttpClient();

        var emailHash = CalculateMd5Hash(email);
        var response = await _httpClient.DeleteAsync($"lists/{ListKey}/members/{emailHash}", ct);

        if (response.StatusCode != System.Net.HttpStatusCode.NotFound && response.StatusCode != System.Net.HttpStatusCode.MethodNotAllowed)
        {
            await EnsureSuccessOrLogAsync(response, email, ct);
        }

        _logger.LogInformation("Member {Email} archived (unsubscribed from all lists) in Mailchimp.", email);
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
            await EnsureSuccessOrLogAsync(response, email, ct);
        }

        _logger.LogInformation("Member {Email} removed from Mailchimp.", email);
    }

    /// <inheritdoc />
    public async Task MigrateEmailAsync(string oldEmail, string newEmail, CancellationToken ct, string? firstName = null, string? lastName = null)
    {
        if (!IsEnabled)
        {
            _logger.LogInformation("MailChimp subscription service is disabled. Skipping email migration from {OldEmail} to {NewEmail}.", oldEmail, newEmail);
            return;
        }

        var subscribedIds = (await GetMemberMailinglistsAsync(oldEmail, ct))
            .Where(l => l.Subscribed)
            .Select(l => l.Id);

        await UpdateMemberSubscriptionsAsync(newEmail, subscribedIds, ct, firstName, lastName);
        await DeleteMemberAsync(oldEmail, ct);

        _logger.LogInformation("Migrated Mailchimp subscriptions from {OldEmail} to {NewEmail}.", oldEmail, newEmail);
    }

    /// <inheritdoc />
    public async Task UpdateMemberNameAsync(string email, string firstName, string lastName, CancellationToken ct)
    {
        if (!IsEnabled)
        {
            _logger.LogInformation("MailChimp subscription service is disabled. Skipping name update for {Email}.", email);
            return;
        }

        ConfigureHttpClient();

        var emailHash = CalculateMd5Hash(email);

        // Dictionary, not an anonymous object - merge tags are case-sensitive at Mailchimp and the
        // default naming policy would otherwise lowercase them. This audience's name fields are
        // tagged FIRSTNAME/LASTNAME, not Mailchimp's usual FNAME/LNAME defaults - confirmed via
        // GET /lists/{list_id}/merge-fields.
        var payload = new
        {
            merge_fields = new Dictionary<string, string> { ["FIRSTNAME"] = firstName, ["LASTNAME"] = lastName }
        };

        var response = await _httpClient.PatchAsJsonAsync($"lists/{ListKey}/members/{emailHash}", payload, ct);

        // Member not in Mailchimp yet - skip rather than create a bare record.
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogInformation("Member {Email} not found in Mailchimp. Skipping name update.", email);
            return;
        }

        await EnsureSuccessOrLogAsync(response, email, ct);

        _logger.LogInformation("Name for {Email} updated in Mailchimp.", email);
    }

    /// <summary>
    /// Throws for a non-success Mailchimp response, first logging the response body - Mailchimp's
    /// error detail (e.g. "Member In Compliance State" for a previously-unsubscribed address) is
    /// otherwise lost, since EnsureSuccessStatusCode's exception only carries the status code.
    /// </summary>
    private async Task EnsureSuccessOrLogAsync(HttpResponseMessage response, string email, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(ct);
        _logger.LogError("Mailchimp request for {Email} failed with {StatusCode}: {Body}", email, (int)response.StatusCode, body);

        // Two distinct Mailchimp compliance states, both permanent and both unfixable by retrying:
        // - "Forgotten Email Not Subscribed": the member deleted their Tavern account, which
        //   GDPR-forgets them at Mailchimp.
        // - "Member In Compliance State": the member unsubscribed, bounced, or was flagged for
        //   review directly at Mailchimp (e.g. via a real campaign's unsubscribe link) - unrelated
        //   to anything Tavern did.
        // In both cases the person must manually opt back in through an actual Mailchimp signup
        // form; forcing status back to "subscribed" via this API can never succeed.
        var error = TryParseError(body);
        if (error?.Title is "Forgotten Email Not Subscribed" or "Member In Compliance State")
        {
            throw new NonRetriableMailSubscriptionException($"Mailchimp will not subscribe {email}: {error.Detail}");
        }

        response.EnsureSuccessStatusCode();
    }

    private static MailchimpErrorResponse? TryParseError(string body)
    {
        try
        {
            return JsonSerializer.Deserialize<MailchimpErrorResponse>(body);
        }
        catch (JsonException)
        {
            return null;
        }
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

    private class MailchimpErrorResponse
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("detail")]
        public string Detail { get; set; } = string.Empty;
    }
}
