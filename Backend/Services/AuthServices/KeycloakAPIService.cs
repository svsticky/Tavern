using System.Net.Http.Headers;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Backend.Services;
using Backend.Utils.DateTime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services.AuthServices;

/// <summary>
/// Implements the auth-service contract against Keycloak.
/// </summary>
public class KeycloakAPIService(
    PostgresDbContext db,
    MailSubscriptionOutboxWorker mailSubscriptionOutboxWorker,
    IHttpClientFactory httpClientFactory,
    [FromServices] IPaymentValidationService paymentValidationService,
    ILogger<KeycloakAPIService> logger) : IAuthService
{
    private readonly string _keycloakUrl = Environment.GetEnvironmentVariable("KeycloakUrl")!;
    private readonly string _keycloakRealm = Environment.GetEnvironmentVariable("KeycloakRealm")!;
    private readonly string _keycloakBackendClientId = Environment.GetEnvironmentVariable("KeycloakBackendClientId")!;
    private readonly string _keycloakClientSecret = Environment.GetEnvironmentVariable("KeycloakClientSecret")!;

    /// <summary>
    /// Synchronizes local member data to an existing Keycloak user.
    /// </summary>
    /// <param name="keycloakId">The Keycloak user ID.</param>
    public async Task SyncMember(Guid keycloakId)
    {
        logger.LogInformation("Syncing member in Keycloak for KeycloakId {KeycloakId}.", keycloakId);
        var member = await db.Members.FirstOrDefaultAsync(m => m.AuthSystemUserId == keycloakId);

        if (member == null)
        {
            // Member is already deleted
            return;
        }

        var client = httpClientFactory.CreateClient("KeycloakAdmin");
        var tokenResponse = await GetServiceAccountToken();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResponse);

        var currentKeycloakUser = await client.GetFromJsonAsync<System.Text.Json.JsonElement>($"users/{keycloakId}");

        string currentEmail = currentKeycloakUser.GetProperty("email").GetString()!;

        bool emailChanged = !string.Equals(currentEmail, member.Email, StringComparison.OrdinalIgnoreCase);

        var memberships = await db.GroupMemberships
            .Include(gm => gm.RoleAlias!.Role)
            .Where(gm => gm.MemberId == member.Id && gm.Group.Active)
            .Select(gm => $"{gm.MembershipYear}:{gm.Group.Id};{gm.Group.Name}:{(gm.RoleAlias != null ? gm.RoleAlias.Id : "")};{(gm.RoleAlias != null ? gm.RoleAlias.Role.Name : "")};{(gm.RoleAlias != null ? gm.RoleAlias.Name : "")}")
            .ToListAsync();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResponse);

        var updatedUser = MapToKeycloakUser(member, currentEmail, null, memberships.ToArray());

        var response = await client.PutAsJsonAsync($"users/{member.AuthSystemUserId}", updatedUser);
        response.EnsureSuccessStatusCode();

        if (emailChanged)
        {
            using var transaction = await db.Database.BeginTransactionAsync();
            try
            {
                mailSubscriptionOutboxWorker.EnqueueTask(member.Email, 0, db);
                member.Email = currentEmail;
                mailSubscriptionOutboxWorker.EnqueueTask(currentEmail, member.MailSubscriptions, db);
                await db.SaveChangesAsync();
                logger.LogInformation("Updated local member email after Keycloak sync for KeycloakId {KeycloakId}.", keycloakId);
                await transaction.CommitAsync();
            }
            catch
            {
                logger.LogError("Failed to update local member email after Keycloak sync for KeycloakId {KeycloakId}. Rolling back transaction.", keycloakId);
                await transaction.RollbackAsync();
                throw;
            }
        }
    }

    /// <summary>
    /// Creates a Keycloak user for a local member.
    /// </summary>
    /// <param name="member">The member to provision.</param>
    /// <returns>The created Keycloak user ID when successful.</returns>
    public async Task<Guid?> CreateUser(Member member)
    {
        logger.LogInformation("Creating Keycloak user for member {MemberId}.", member.Id);
        var client = httpClientFactory.CreateClient("KeycloakAdmin");
        var token = await GetServiceAccountToken();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var newUser = MapToKeycloakUser(member, member.Email, false);
        var response = await client.PostAsJsonAsync("users", newUser);
        
        if (response.IsSuccessStatusCode)
        {
            var id = response.Headers.Location?.Segments.Last();
            if (id != null && Guid.TryParse(id, out var keycloakId))            
            {
                logger.LogInformation("Created Keycloak user {KeycloakId} for member {MemberId}.", keycloakId, member.Id);
                return keycloakId;
            }
        }
        else
        {
            response.EnsureSuccessStatusCode();
        }

        throw new Exception("Unexpected error creating user in Keycloak.");
    }

    private class KeycloakUserResponse { public string Id { get; set; } = default!; }

    /// <summary>
    /// Deletes a Keycloak user.
    /// </summary>
    /// <param name="keycloakId">The Keycloak user ID.</param>
    public async Task DeleteUser(Guid keycloakId)
    {
        logger.LogInformation("Deleting Keycloak user {KeycloakId}.", keycloakId);
        var client = httpClientFactory.CreateClient("KeycloakAdmin");
        var token = await GetServiceAccountToken();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.DeleteAsync($"users/{keycloakId}");
        response.EnsureSuccessStatusCode();
    }

    private async Task<string> GetServiceAccountToken()
    {
        var client = httpClientFactory.CreateClient();
        
        var url = $"{_keycloakUrl}/realms/{_keycloakRealm}/protocol/openid-connect/token";
        
        var dict = new Dictionary<string, string>
        {
            { "grant_type", "client_credentials" },
            { "client_id", _keycloakBackendClientId },
            { "client_secret", _keycloakClientSecret }
        };

        var content = new FormUrlEncodedContent(dict);
        
        var response = await client.PostAsync(url, content);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            logger.LogError("Failed retrieving Keycloak service token. Status: {StatusCode}", response.StatusCode);
            throw new Exception($"Keycloak Auth Failed: {error}");
        }

        var json = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        return json.GetProperty("access_token").GetString()!;
    }

    /// <summary>
    /// Sends a Keycloak execute-actions email to a user.
    /// </summary>
    /// <param name="keycloakId">The Keycloak user ID.</param>
    /// <param name="actions">The required actions to include.</param>
    private async Task SendActionEmail(Guid keycloakId, string[] actions)
    {
        logger.LogInformation("Sending Keycloak action email to {KeycloakId} with {ActionCount} actions.", keycloakId, actions.Length);
        var client = httpClientFactory.CreateClient("KeycloakAdmin");
        var token = await GetServiceAccountToken();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PutAsJsonAsync($"users/{keycloakId}/execute-actions-email", actions);
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            logger.LogError("Failed sending Keycloak action email to {KeycloakId}. Status: {StatusCode}", keycloakId, response.StatusCode);
            throw new Exception($"Keycloak Email Failed: {error}");
        }
    }

    /// <summary>
    /// Gets the email of a Keycloak user.
    /// </summary> 
    /// <param name="keycloakId">The Keycloak user ID.</param>
    /// <returns>The email address.</returns>
    public async Task<string> GetEmail(Guid keycloakId)
    {
        var client = httpClientFactory.CreateClient("KeycloakAdmin");
        var token = await GetServiceAccountToken();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync($"users/{keycloakId}");
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            logger.LogError("Failed fetching Keycloak user {KeycloakId}. Status: {StatusCode}", keycloakId, response.StatusCode);
            throw new Exception($"Keycloak User Fetch Failed: {error}");
        }

        var json = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        return json.GetProperty("email").GetString()!;
    }

    /// <summary>
    /// Refreshes the local member email from Keycloak.
    /// </summary>
    /// <param name="keycloakId">The Keycloak user ID.</param>
    public async Task RefreshEmail(Guid keycloakId)
    {
        logger.LogInformation("Refreshing local email from Keycloak for {KeycloakId}.", keycloakId);

        var email = await GetEmail(keycloakId);

        var member = await db.Members.FirstOrDefaultAsync(m => m.AuthSystemUserId == keycloakId);
        if (member != null)
        {
            member.Email = email;
            await db.SaveChangesAsync();
            logger.LogInformation("Updated local member email from Keycloak for {KeycloakId}.", keycloakId);
        }
    }

    private object MapToKeycloakUser(Member member, string currentEmail, bool? emailVerified = null, string[]? memberships = null)
    {
        var boardGroupIdStr = db.Settings.FirstOrDefault(s => s.Name == "BoardGroupId")?.Value;
        var candidateBoardGroupIdStr = db.Settings.FirstOrDefault(s => s.Name == "CandidateBoardGroupId")?.Value;
        uint boardGroupId = string.IsNullOrEmpty(boardGroupIdStr) ? 0 : uint.Parse(boardGroupIdStr);
        uint candidateBoardGroupId = string.IsNullOrEmpty(candidateBoardGroupIdStr) ? 0 : uint.Parse(candidateBoardGroupIdStr);
        uint currentBoardYear = YearUtils.GetBoardYear(db);

        var adminGroupIdsStr = db.Settings.FirstOrDefault(s => s.Name == "AdminGroupIds")?.Value ?? "";
        var extraAdminGroupIds = adminGroupIdsStr
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => uint.TryParse(s, out var id) ? id : 0u)
            .Where(id => id > 0)
            .ToHashSet();

        bool isAdmin = db.GroupMemberships.Any(gm =>
            gm.MemberId == member.Id &&
            gm.MembershipYear == currentBoardYear &&
            (gm.GroupId == boardGroupId || gm.GroupId == candidateBoardGroupId || extraAdminGroupIds.Contains(gm.GroupId)));

        return new
        {
            username = member.Email,
            email = currentEmail,
            firstName = member.FirstName,
            lastName = member.LastName,
            enabled = true,
            emailVerified = emailVerified,
            attributes = new Dictionary<string, List<string>> {
                { "koala_user_id", new List<string> { member.Id.ToString() } },
                { "access_level", new List<string> { member.Suspended ? "suspended" : paymentValidationService.HasPaidMembershipPaymentBeforeExpirationTime(member.Id) ? "full" : "not_paid" } },
                { "group_memberships", memberships?.ToList() ?? new List<string>() },
                { "student_number", new List<string> { member.StudentNumber.ToString() } },
                { "locale", new List<string> { member.PreferredLanguage.ToString() } },
                { "email", new List<string> { currentEmail } },
                { "is_admin", new List<string> { isAdmin.ToString().ToLower() } },
                { "full_name", new List<string> { $"{member.FirstName} {member.LastName}" } },
                { "birthday", new List<string> { member.DateOfBirth.ToString("yyyy-MM-dd") } }
            }
        };
    }
}
