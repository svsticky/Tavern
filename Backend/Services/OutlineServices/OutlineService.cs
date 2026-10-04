using Backend.Database;
using Backend.Interfaces;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;

namespace Backend.Services.OutlineServices;

/// <summary>
/// Implements synchronization with an Outline knowledge base instance for email updates, deletions, and administrative role assignments.
/// </summary>
public class OutlineService : IOutlineService
{
    private readonly ILogger<OutlineService> _logger;
    private readonly HttpClient _httpClient;
    private readonly PostgresDbContext _context;

    private bool IsEnabled => _context.Settings.Find("OutlineEnabled")?.Value?.Trim().Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;
    private string ApiUrl => _context.Settings.Find("OutlineApiUrl")?.Value?.Trim() ?? string.Empty;
    private string ApiKey => _context.Settings.Find("OutlineApiKey")?.Value?.Trim() ?? string.Empty;
    private string BoardGroupName => _context.Settings.Find("OutlineBoardGroupName")?.Value?.Trim() ?? "Board";

    /// <summary>
    /// Initializes a new instance of the <see cref="OutlineService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="httpClient">The HTTP client used to make requests to the Outline API.</param>
    /// <param name="context">The database context for accessing application settings.</param>
    public OutlineService(
        ILogger<OutlineService> logger,
        HttpClient httpClient,
        PostgresDbContext context)
    {
        _logger = logger;
        _httpClient = httpClient;
        _context = context;
    }

    private void ConfigureHttpClient()
    {
        if (_httpClient.BaseAddress != null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(ApiUrl) || string.IsNullOrWhiteSpace(ApiKey))
        {
            return;
        }

        var url = ApiUrl.TrimEnd('/') + "/";
        _httpClient.BaseAddress = new Uri(url);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);
    }

    /// <inheritdoc />
    public async Task SyncMailAsync(string oldEmail, string newEmail, CancellationToken ct)
    {
        if (!IsEnabled)
        {
            _logger.LogInformation("Outline service is disabled. Skipping email update from {OldEmail} to {NewEmail}.", oldEmail, newEmail);
            return;
        }

        ConfigureHttpClient();
        var user = await FindUserByEmailAsync(oldEmail, ct);
        if (user == null)
        {
            _logger.LogInformation("No user found in Outline for email {OldEmail}. Skipping email update.", oldEmail);
            return;
        }

        var payload = new { id = user.Id, email = newEmail };
        var response = await _httpClient.PostAsJsonAsync("api/users.updateEmail", payload, ct);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("User {UserId} not found when updating email in Outline.", user.Id);
            return;
        }

        response.EnsureSuccessStatusCode();
        _logger.LogInformation("Successfully updated Outline email for user {UserId} to {NewEmail}.", user.Id, newEmail);
    }

    /// <inheritdoc />
    public async Task DeleteMailAsync(string email, CancellationToken ct)
    {
        if (!IsEnabled)
        {
            _logger.LogInformation("Outline service is disabled. Skipping user deletion for {Email}.", email);
            return;
        }

        ConfigureHttpClient();
        var user = await FindUserByEmailAsync(email, ct);
        if (user == null)
        {
            _logger.LogInformation("No user found in Outline for email {Email}. Skipping deletion.", email);
            return;
        }

        var payload = new { id = user.Id };
        var response = await _httpClient.PostAsJsonAsync("api/users.delete", payload, ct);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogInformation("User {UserId} was already deleted in Outline.", user.Id);
            return;
        }

        response.EnsureSuccessStatusCode();
        _logger.LogInformation("Successfully deleted Outline user {UserId} ({Email}).", user.Id, email);
    }

    /// <inheritdoc />
    public async Task UpdateAdminStatusAsync(string email, bool isAdmin, CancellationToken ct)
    {
        if (!IsEnabled)
        {
            _logger.LogInformation("Outline service is disabled. Skipping admin status update for {Email}.", email);
            return;
        }

        ConfigureHttpClient();
        var user = await FindUserByEmailAsync(email, ct);
        if (user == null)
        {
            _logger.LogInformation("No user found in Outline for email {Email}. Skipping admin status update.", email);
            return;
        }

        string targetRole = isAdmin ? "admin" : "member";
        var rolePayload = new { id = user.Id, role = targetRole };
        var roleResponse = await _httpClient.PostAsJsonAsync("api/users.update_role", rolePayload, ct);

        if (roleResponse.StatusCode != HttpStatusCode.NotFound)
        {
            roleResponse.EnsureSuccessStatusCode();
        }

        string? boardGroupId = await GetOrCreateBoardGroupAsync(isAdmin, ct);

        if (!string.IsNullOrEmpty(boardGroupId))
        {
            if (isAdmin)
            {
                var addPayload = new { id = boardGroupId, userId = user.Id };
                var addResponse = await _httpClient.PostAsJsonAsync("api/groups.add_user", addPayload, ct);
                if (addResponse.StatusCode != HttpStatusCode.BadRequest && addResponse.StatusCode != HttpStatusCode.NotFound)
                {
                    addResponse.EnsureSuccessStatusCode();
                }
            }
            else
            {
                var removePayload = new { id = boardGroupId, userId = user.Id };
                var removeResponse = await _httpClient.PostAsJsonAsync("api/groups.remove_user", removePayload, ct);
                if (removeResponse.StatusCode != HttpStatusCode.BadRequest && removeResponse.StatusCode != HttpStatusCode.NotFound)
                {
                    removeResponse.EnsureSuccessStatusCode();
                }
            }
        }

        _logger.LogInformation("Updated Outline admin status for {Email} (isAdmin: {IsAdmin}).", email, isAdmin);
    }

    private async Task<OutlineUser?> FindUserByEmailAsync(string email, CancellationToken ct)
    {
        var payload = new { query = email };
        var response = await _httpClient.PostAsJsonAsync("api/users.list", payload, ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Failed to list users from Outline. Status: {StatusCode}", response.StatusCode);
            return null;
        }

        var result = await response.Content.ReadFromJsonAsync<OutlineUsersResponse>(ct);
        return result?.Data?.FirstOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<string?> GetOrCreateBoardGroupAsync(bool createIfMissing, CancellationToken ct)
    {
        var listPayload = new { query = BoardGroupName };
        var listResponse = await _httpClient.PostAsJsonAsync("api/groups.list", listPayload, ct);

        if (listResponse.IsSuccessStatusCode)
        {
            var groupsResult = await listResponse.Content.ReadFromJsonAsync<OutlineGroupsResponse>(ct);
            var existingGroup = groupsResult?.Data?.Groups?.FirstOrDefault(g => string.Equals(g.Name, BoardGroupName, StringComparison.OrdinalIgnoreCase));
            if (existingGroup != null)
            {
                return existingGroup.Id;
            }
        }

        if (!createIfMissing)
        {
            return null;
        }

        var createPayload = new { name = BoardGroupName };
        var createResponse = await _httpClient.PostAsJsonAsync("api/groups.create", createPayload, ct);
        if (createResponse.IsSuccessStatusCode)
        {
            var createdGroup = await createResponse.Content.ReadFromJsonAsync<OutlineGroupCreationResponse>(ct);
            return createdGroup?.Data?.Id;
        }

        return null;
    }

    private class OutlineUsersResponse
    {
        [JsonPropertyName("data")]
        public List<OutlineUser>? Data { get; set; }
    }

    private class OutlineUser
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;
    }

    private class OutlineGroupsResponse
    {
        [JsonPropertyName("data")]
        public OutlineGroupsData? Data { get; set; }
    }

    private class OutlineGroupsData
    {
        [JsonPropertyName("groups")]
        public List<OutlineGroup>? Groups { get; set; }
    }

    private class OutlineGroup
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    private class OutlineGroupCreationResponse
    {
        [JsonPropertyName("data")]
        public OutlineGroup? Data { get; set; }
    }
}
