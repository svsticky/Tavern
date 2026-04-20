using System.Net.Http.Headers;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class KeycloakAPIService(PostgresDbContext db, IHttpClientFactory httpClientFactory, [FromServices] IPaymentValidationService paymentValidationService)
{
    private readonly string _keycloakUrl = Environment.GetEnvironmentVariable("KeycloakUrl")!;
    private readonly string _keycloakRealm = Environment.GetEnvironmentVariable("KeycloakRealm")!;
    private readonly string _keycloakBackendClientId = Environment.GetEnvironmentVariable("KeycloakBackendClientId")!;
    private readonly string _keycloakClientSecret = Environment.GetEnvironmentVariable("KeycloakClientSecret")!;

    public async Task SyncMemberInKeyCloak(Guid keycloakId)
    {
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
        }
    }

    public async Task<Guid?> CreateUserInKeycloak(Member member)
    {
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
            throw new Exception($"Keycloak Auth Failed: {error}");
        }

        var json = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        return json.GetProperty("access_token").GetString()!;
    }

    public async Task SendActionEmail(Guid keycloakId, string[] actions)
    {
        var client = httpClientFactory.CreateClient("KeycloakAdmin");
        var token = await GetServiceAccountToken();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.PutAsJsonAsync($"users/{keycloakId}/execute-actions-email", actions);
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Keycloak Email Failed: {error}");
        }
    }

    public async Task RefreshEmail(Guid keycloakId)
    {
        var client = httpClientFactory.CreateClient("KeycloakAdmin");
        var token = await GetServiceAccountToken();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync($"users/{keycloakId}");
        
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Keycloak User Fetch Failed: {error}");
        }

        var json = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        var email = json.GetProperty("email").GetString()!;

        var member = await db.Members.FirstOrDefaultAsync(m => m.KeycloakId == keycloakId);
        if (member != null)
        {
            member.Email = email;
            await db.SaveChangesAsync();
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