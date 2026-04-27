using System.Net.Http.Headers;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Backend.Services;

public class KeycloakAPIService(
    PostgresDbContext db,
    IHttpClientFactory httpClientFactory,
    [FromServices] IPaymentValidationService paymentValidationService,
    ILogger<KeycloakAPIService> logger)
{
    private readonly string _keycloakUrl = Environment.GetEnvironmentVariable("KeycloakUrl")!;
    private readonly string _keycloakRealm = Environment.GetEnvironmentVariable("KeycloakRealm")!;
    private readonly string _keycloakBackendClientId = Environment.GetEnvironmentVariable("KeycloakBackendClientId")!;
    private readonly string _keycloakClientSecret = Environment.GetEnvironmentVariable("KeycloakClientSecret")!;

    public async Task SyncMemberInKeyCloak(Guid keycloakId)
    {
        logger.LogInformation("Syncing member in Keycloak for KeycloakId {KeycloakId}.", keycloakId);
        var member = await db.Members.FirstOrDefaultAsync(m => m.KeycloakId == keycloakId);

        if (member == null)
        {
            throw new Exception($"Member with id {keycloakId} not found.");
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

        var response = await client.PutAsJsonAsync($"users/{member.KeycloakId}", updatedUser);
        response.EnsureSuccessStatusCode();

        if (emailChanged)
        {
            member.Email = currentEmail;
            await db.SaveChangesAsync();
            logger.LogInformation("Updated local member email after Keycloak sync for KeycloakId {KeycloakId}.", keycloakId);
        }
    }

    public async Task<Guid?> CreateUserInKeycloak(Member member)
    {
        logger.LogInformation("Creating Keycloak user for member {MemberId}.", member.Id);
        var client = httpClientFactory.CreateClient("KeycloakAdmin");
        var token = await GetServiceAccountToken();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var newUser = MapToKeycloakUser(member, member.Email, true);
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

    public async Task DeleteUserInKeycloak(Guid keycloakId)
    {
        logger.LogInformation("Deleting Keycloak user {KeycloakId}.", keycloakId);
        var client = httpClientFactory.CreateClient("KeycloakAdmin");
        var token = await GetServiceAccountToken();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.DeleteAsync($"users/{keycloakId}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<string> GetServiceAccountToken()
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

    public async Task SendActionEmail(Guid keycloakId, string[] actions)
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

    public async Task RefreshEmail(Guid keycloakId)
    {
        logger.LogInformation("Refreshing local email from Keycloak for {KeycloakId}.", keycloakId);
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
        var email = json.GetProperty("email").GetString()!;

        var member = await db.Members.FirstOrDefaultAsync(m => m.KeycloakId == keycloakId);
        if (member != null)
        {
            member.Email = email;
            await db.SaveChangesAsync();
            logger.LogInformation("Updated local member email from Keycloak for {KeycloakId}.", keycloakId);
        }
    }

    private object MapToKeycloakUser(Member member, string currentEmail, bool? emailVerified = null, string[]? memberships = null)
    {
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
                { "access_level", new List<string> { member.Suspended ? "suspended" : paymentValidationService.HasPaidMembershipPayment(member.Id) ? "full" : "not_paid" } },
                { "group_memberships", memberships?.ToList() ?? new List<string>() },
                { "student_number", new List<string> { member.StudentNumber.ToString() } },
                { "locale", new List<string> { member.PreferredLanguage.ToString() } }
            }
        };
    }
}
