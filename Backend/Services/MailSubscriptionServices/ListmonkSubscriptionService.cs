using Backend.Database;
using Backend.Services.OutboxWorkers;
using System.Text;
using System.Text.Json.Serialization;

namespace Backend.Services.MailSubscriptionServices;

/// <summary>
/// Implements <see cref="AbstractMailSubscriptionService"/> against the Listmonk API. Listmonk is treated as the sole source of truth for mailing lists and member subscriptions - nothing is mirrored locally.
/// </summary>
/// <param name="logger">The logger.</param>
/// <param name="httpClient">The HTTP client.</param>
/// <param name="context">The database context.</param>
/// <param name="mailSubscriptionOutboxWorker">Used to queue outbox tasks from listener notifications.</param>
public class ListmonkSubscriptionService(
    ILogger<ListmonkSubscriptionService> logger,
    HttpClient httpClient,
    PostgresDbContext context,
    MailSubscriptionOutboxWorker mailSubscriptionOutboxWorker) : AbstractMailSubscriptionService(mailSubscriptionOutboxWorker)
{
    private readonly ILogger<ListmonkSubscriptionService> _logger = logger;
    private readonly HttpClient _httpClient = httpClient;
    private readonly PostgresDbContext _context = context;

    /// <inheritdoc />
    public override bool IsEnabled => _context.Settings.Find("MailSubscriptionService")?.Value?.Trim().Equals("LISTMONK", StringComparison.OrdinalIgnoreCase) ?? false;

    private void ConfigureHttpClient()
    {
        if (_httpClient.BaseAddress != null)
            return;

        string? api_key = _context.Settings.Find("ListmonkApiKey")?.Value,
                user = _context.Settings.Find("ListmonkUser")?.Value,
                api_url = _context.Settings.Find("ListmonkUrl")?.Value;
        if (api_key is null || user is null || api_url is null)
        {
            throw new Exception("Cannot instantiate Listmonk subscription service without configured user, url and api key.");
        }

        _httpClient.BaseAddress = new Uri(api_url);
        var authValue = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{user}:{api_key}"));
        _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authValue);
    }

    /// <inheritdoc />
    public override async Task<IEnumerable<MailinglistDto>> GetAvailableMailinglistsAsync(CancellationToken ct)
    {
        if (!IsEnabled)
        {
            _logger.LogInformation("Listmonk subscription service is disabled. Returning no available mailing lists.");
            return [];
        }

        ConfigureHttpClient();

        var response = await _httpClient.GetFromJsonAsync<ListmonkResponse<Paginated<Mailinglist>>>(
            $"api/lists?status=active&minimal=true", ct);

        return (response?.Data?.Results ?? []).Select(l => new MailinglistDto(l.Id.ToString(), l.Name));
    }

    /// <inheritdoc />
    public override async Task<IEnumerable<MemberMailinglistDto>> GetMemberMailinglistsAsync(string email, CancellationToken ct)
    {
        if (!IsEnabled)
        {
            _logger.LogInformation($"Listmonk subscription service is disabled. Returning no member mailing lists for {email}.");
            return [];
        }

        ConfigureHttpClient();

        var all_mailinglists_response = await _httpClient.GetFromJsonAsync<ListmonkResponse<Paginated<Mailinglist>>>(
            $"api/lists?status=active&minimal=true", ct);
        var all_mailinglists = all_mailinglists_response?.Data?.Results ?? [];

        var subscriber = await GetSubscriberByEmail(email, ct);

        // If the subscriber does not exist, they cannot have any mailing list subscriptions.
        // When they select one, a subscriber is created and then their subscriptions will be correctly displayed.
        var own_mailinglists = subscriber?.Lists ?? [];
        var own_mailinglists_ids = own_mailinglists.Select(ml => ml.Id).ToArray();

        return all_mailinglists.Select(ml =>
            new MemberMailinglistDto(ml.Id.ToString(), ml.Name, own_mailinglists_ids.Contains(ml.Id)));
    }

    /// <inheritdoc />
    public override async Task UpdateMemberSubscriptionsAsync(string email, IEnumerable<string> subscribedListIds, CancellationToken ct, string? first_name = null, string? last_name = null)
    {
        if (!IsEnabled)
        {
            _logger.LogInformation($"Listmonk subscription service is disabled. Skipping update for {email}.");
            return;
        }

        ConfigureHttpClient();

        // If this user has been registered in listmonk already, update the record.
        if (await GetSubscriberByEmail(email, ct) is Subscriber { Id: var subscriber_id })
        {
            var payload = new { lists = subscribedListIds.Select(int.Parse).ToList(), name = $"{first_name} {last_name}" };
            var response = await _httpClient.PatchAsJsonAsync($"api/subscribers/{subscriber_id}", payload, ct);
            response.EnsureSuccessStatusCode();
        }
        else
        { // Otherwise create a new subscriber record in listmonk.
            var payload = new
            {
                email,
                name = $"{first_name} {last_name}",
                status = "enabled",
                lists = subscribedListIds.Select(int.Parse).ToList(),
            };
            var response = await _httpClient.PostAsJsonAsync($"api/subscribers", payload, ct);
            response.EnsureSuccessStatusCode();
        }

        _logger.LogInformation($"Subscriptions for {email} updated.");
    }

    /// <inheritdoc />
    public override async Task UpdateMemberNameAsync(string email, string first_name, string last_name, CancellationToken ct)
    {
        if (!IsEnabled)
        {
            _logger.LogInformation($"Listmonk subscription service is disabled. Skipping update for {email}.");
            return;
        }

        ConfigureHttpClient();

        var subscriber = await GetSubscriberByEmail(email, ct);

        var payload = new { name = $"{first_name} {last_name}" };
        var response = await _httpClient.PatchAsJsonAsync($"api/subscribers/{subscriber!.Id}", payload, ct);
        response.EnsureSuccessStatusCode();

        _logger.LogInformation($"Subscriptions for {email} updated.");
    }

    /// <inheritdoc />
    public override async Task DeleteMemberAsync(string email, CancellationToken ct)
    {
        if (!IsEnabled)
        {
            _logger.LogInformation($"Listmonk subscription service is disabled. Skipping delete for {email}.");
            return;
        }

        ConfigureHttpClient();

        var subscriber = await GetSubscriberByEmail(email, ct);
        await _httpClient.DeleteAsync($"api/subscribers/{subscriber!.Id}", ct);

        _logger.LogInformation($"Member {email} removed from Mailchimp.");
    }

    /// <inheritdoc />
    public override async Task MigrateEmailAsync(string old_email, string new_email, CancellationToken ct, string? first_name = null, string? last_name = null)
    {
        if (!IsEnabled)
        {
            _logger.LogInformation($"Listmonk subscription service is disabled. Skipping email migration from {old_email} to {new_email}.");
            return;
        }

        ConfigureHttpClient();

        var subscriber = await GetSubscriberByEmail(old_email, ct);
        var payload = new { email = new_email };
        var response = await _httpClient.PatchAsJsonAsync($"api/subscribers/{subscriber!.Id}", payload, ct);
        response.EnsureSuccessStatusCode();

        _logger.LogInformation($"Migrated Mailchimp subscriptions from {old_email} to {new_email}.");
    }

    private async Task<Subscriber?> GetSubscriberByEmail(string email, CancellationToken ct)
    {
        var subscriber_response = await _httpClient.GetFromJsonAsync<ListmonkResponse<Paginated<Subscriber>>>(
            $"api/subscribers?per_page=1&query=subscribers.email = '{email}'", ct);
        return subscriber_response?.Data?.Results switch
        {
            [var subscriber, ..] => subscriber,
            _ => null
        };
    }

    private class ListmonkResponse<T>
    {
        [JsonPropertyName("data")]
        public required T Data { get; set; }
    }

    private class Paginated<T>
    {
        [JsonPropertyName("results")]
        public required List<T> Results { get; set; }
    }

    private class Mailinglist
    {
        [JsonPropertyName("id")]
        public required int Id { get; set; }

        [JsonPropertyName("name")]
        public required string Name { get; set; }
    }

    private class Subscriber
    {
        [JsonPropertyName("id")]
        public required int Id { get; set; }

        [JsonPropertyName("lists")]
        public required List<Mailinglist> Lists { get; set; }
    }
}
