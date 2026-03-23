using System.Net.Http.Headers;
using Backend.Database;
using Backend.Models;
using Backend.Utils;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class KeycloakAPIService(PostgresDbContext db, IHttpClientFactory httpClientFactory)
{
    public async Task SyncMemberInKeyCloak(Guid memberId)
    {
        var member = await db.Members.FindAsync(memberId);

        if (member == null)
        {
            throw new Exception($"Member with id {memberId} not found.");
        }

        var memberships = await db.GroupMemberships
            .Where(gm => gm.MemberId == memberId && gm.Group.Active)
            .Select(gm => $"{gm.MembershipYear}:{gm.Group.Name}:{(gm.RoleAlias != null ? gm.RoleAlias.Id : "")}")
            .ToListAsync();

        var client = httpClientFactory.CreateClient("KeycloakAdmin");

        var tokenResponse = await GetServiceAccountToken();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResponse);

        var updatedUser = MapToKeycloakUser(member, memberships.ToArray());

        var response = await client.PutAsJsonAsync($"users/{member.KeycloakId}", updatedUser);
        response.EnsureSuccessStatusCode();
    }

    public async Task<Guid?> CreateUserInKeycloak(Member member)
    {
        var client = httpClientFactory.CreateClient("KeycloakAdmin");
        var token = await GetServiceAccountToken();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var newUser = MapToKeycloakUser(member);
        var response = await client.PostAsJsonAsync("users", newUser);
        
        string? keycloakId = null;

        if (response.IsSuccessStatusCode)
        {
            // User created
            keycloakId = response.Headers.Location?.Segments.Last();
        }
        else if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            // User was already created, so continue for sending the mail
            var searchResponse = await client.GetFromJsonAsync<List<KeycloakUserResponse>>($"users?email={member.Email}");
            keycloakId = searchResponse?.FirstOrDefault()?.Id;
        }
        else
        {
            response.EnsureSuccessStatusCode();
        }

        if (keycloakId != null)
        {
            await client.PutAsJsonAsync($"users/{keycloakId}", newUser);
            var actions = new[] { "UPDATE_PASSWORD", "VERIFY_EMAIL" };
            var emailResponse = await client.PutAsJsonAsync($"users/{keycloakId}/execute-actions-email", actions);
            emailResponse.EnsureSuccessStatusCode();
            return Guid.Parse(keycloakId);
        }

        return null;
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
        
        var url = $"{Environment.GetEnvironmentVariable("KeycloakUrl")}/realms/{Environment.GetEnvironmentVariable("KeycloakRealm")}/protocol/openid-connect/token";
        
        var dict = new Dictionary<string, string>
        {
            { "grant_type", "client_credentials" },
            { "client_id", Environment.GetEnvironmentVariable("KeycloakBackendClientId")! },
            { "client_secret", Environment.GetEnvironmentVariable("KeycloakClientSecret")! }
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

    private object MapToKeycloakUser(Member member, string[]? memberships = null)
    {
        return new
        {
            username = member.Email,
            email = member.Email,
            firstName = member.FirstName,
            lastName = member.LastName,
            enabled = true,
            attributes = new Dictionary<string, List<string>> {
                { "koala_user_id", new List<string> { member.Id.ToString() } },
                { "access_level", new List<string> { member.Suspended ? "suspended" : PaymentUtils.HasPaidMembershipPayment(member, db) ? "notpaid" : "full" } },
                { "member_memberships", memberships?.ToList() ?? new List<string>() },
                { "student_number", new List<string> { member.StudentNumber.ToString() } },
                { "locale", new List<string> { member.PreferredLanguage.ToString() } }
            }
        };
    }
}