using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Backend.Utils.DateTime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Backend.Services.AuthServices;

/// <summary>
/// Implements the auth-service contract against Keycloak.
/// </summary>
public class KeycloakAPIService(
    PostgresDbContext db,
    IPermissionService permissionService,
    MailSubscriptionOutboxWorker mailSubscriptionOutboxWorker,
    IHttpClientFactory httpClientFactory,
    [FromServices] IPaymentValidationService paymentValidationService,
    ILogger<KeycloakAPIService> logger,
    IEnumerable<IMailSyncOutboxWorker>? mailSyncWorkers = null) : IAuthService
{
    private readonly IEnumerable<IMailSyncOutboxWorker> _mailSyncWorkers = mailSyncWorkers ?? (mailSubscriptionOutboxWorker != null ? [mailSubscriptionOutboxWorker] : []);
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

        // Preserve the legacy_bcrypt_hash attribute across syncs. It's set on migrated members by
        // the Koala import and cleared by the custom BCrypt authenticator once they log in with
        // their old password - but this method replaces the whole attributes object on every sync,
        // so without carrying it forward here it gets silently destroyed before the member ever
        // gets a chance to log in with it.
        string? legacyBcryptHash = null;
        if (currentKeycloakUser.TryGetProperty("attributes", out var existingAttributes) &&
            existingAttributes.TryGetProperty("legacy_bcrypt_hash", out var legacyBcryptHashValues) &&
            legacyBcryptHashValues.GetArrayLength() > 0)
        {
            legacyBcryptHash = legacyBcryptHashValues[0].GetString();
        }

        bool emailChanged = !string.Equals(currentEmail, member.Email, StringComparison.OrdinalIgnoreCase);

        var currentCommitteeYear = YearUtils.GetYearForDate(System.DateTime.UtcNow, YearUtils.CommitteeCreationDate);

        var membershipClaims = await db.GroupMemberships
            .Include(gm => gm.RoleAlias!.Role)
            .Where(gm => gm.MemberId == member.Id && gm.Group.Active && gm.MembershipYear == currentCommitteeYear)
            .Select(gm => new GroupMembershipClaim
            {
                Id = gm.GroupId,
                Name = gm.Group.Name,
                Permissions = db.GroupPermissions.Where(gp => gp.GroupId == gm.GroupId).Select(gp => gp.PermissionKey).ToList(),
                Role = gm.RoleAlias == null ? null : new RoleClaim
                {
                    Id = gm.RoleAlias.RoleId,
                    Name = gm.RoleAlias.Role.Name,
                    Alias = gm.RoleAlias.Name,
                    Permissions = db.RolePermissions.Where(rp => rp.RoleId == gm.RoleAlias.RoleId).Select(rp => rp.PermissionKey).ToList()
                }
            })
            .ToListAsync();

        var membershipsJson = JsonSerializer.Serialize(membershipClaims);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResponse);

        var updatedUser = MapToKeycloakUser(member, currentEmail, null, membershipsJson, legacyBcryptHash);

        var response = await client.PutAsJsonAsync($"users/{member.AuthSystemUserId}", updatedUser);
        response.EnsureSuccessStatusCode();

        if (emailChanged)
        {
            using var transaction = await db.Database.BeginTransactionAsync();
            try
            {
                foreach (var worker in _mailSyncWorkers)
                {
                    worker.EnqueueSyncMail(member.Email, currentEmail, db);
                }
                member.Email = currentEmail;
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

        // Deleting is retried on failure (e.g. AuthOutboxWorker retrying after the auth-system
        // deletion succeeded but a later step in the same task failed). Treat "already gone" as
        // success rather than throwing, so a retry doesn't get stuck failing forever.
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            logger.LogInformation("Keycloak user {KeycloakId} was already deleted.", keycloakId);
            return;
        }

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

    /// <inheritdoc />
    public Task SendActivationEmail(Guid keycloakId) => SendActionEmail(keycloakId, ["VERIFY_EMAIL", "UPDATE_PASSWORD"]);

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

    /// <summary>
    /// Determines whether a member currently counts as having paid, for the purposes of the Keycloak
    /// access_level attribute. Begunstigers pay their own separate fee (checked against the last board
    /// rotation) instead of the regular membership fee, so this mirrors PaymentService.GetMemberPaymentStatus's
    /// branching rather than calling HasPaidMembershipPaymentBeforeExpirationTime directly.
    /// </summary>
    private bool HasPaidMembership(Member member)
    {
        return member.Begunstiger
            ? paymentValidationService.HasPaidBegunstigerFeeSinceLastBoardChange(member.Id)
            : paymentValidationService.HasPaidMembershipPaymentBeforeExpirationTime(member.Id);
    }

    /// <summary>
    /// A single group_memberships claim entry, matching the shape consumed by the frontend's group.util.ts.
    /// Permission entries are raw string keys: either one of the 12 known Permission names, or an
    /// arbitrary custom string for other applications sharing this Keycloak instance to interpret.
    /// </summary>
    private class GroupMembershipClaim
    {
        [JsonPropertyName("id")] public uint Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("permissions")] public List<string> Permissions { get; set; } = new();
        [JsonPropertyName("role")] public RoleClaim? Role { get; set; }
    }

    /// <summary>
    /// The role portion of a group_memberships claim entry.
    /// </summary>
    private class RoleClaim
    {
        [JsonPropertyName("id")] public uint Id { get; set; }
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("alias")] public string Alias { get; set; } = "";
        [JsonPropertyName("permissions")] public List<string> Permissions { get; set; } = new();
    }

    private object MapToKeycloakUser(Member member, string currentEmail, bool? emailVerified = null, string? membershipsJson = null, string? legacyBcryptHash = null)
    {
        var attributes = new Dictionary<string, List<string>> {
                { "koala_user_id", new List<string> { member.Id.ToString() } },
                { "access_level", new List<string> { member.Suspended ? "suspended" : HasPaidMembership(member) ? "full" : "not_paid" } },
                { "group_memberships", new List<string> { membershipsJson ?? "[]" } },
                { "student_number", new List<string> { member.StudentNumber.ToString() } },
                { "locale", new List<string> { member.PreferredLanguage.ToString() } },
                { "email", new List<string> { currentEmail } },
                { "is_admin", new List<string> { permissionService.IsBoardOrCandidateBoardMember(member.Id).ToString().ToLowerInvariant() } },
                { "full_name", new List<string> { $"{member.FirstName} {member.LastName}" } },
                { "birthday", new List<string> { member.DateOfBirth.ToString("yyyy-MM-dd") } }
        };

        if (!string.IsNullOrEmpty(legacyBcryptHash))
        {
            attributes["legacy_bcrypt_hash"] = new List<string> { legacyBcryptHash };
        }

        return new
        {
            username = member.Email,
            email = currentEmail,
            firstName = member.FirstName,
            lastName = member.LastName,
            enabled = true,
            emailVerified = emailVerified,
            attributes
        };
    }
}
