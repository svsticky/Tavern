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
        
        if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        var locationUri = response.Headers.Location;
        var keycloakId = locationUri?.Segments.Last();

        return keycloakId != null ? Guid.Parse(keycloakId) : null;
    }

    public async Task DeleteUserInKeycloak(Guid keycloakId)
    {
        var client = httpClientFactory.CreateClient("KeycloakAdmin");
        var token = await GetServiceAccountToken();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.DeleteAsync($"users/{keycloakId}");
        response.EnsureSuccessStatusCode();
    }

    private async Task<string> GetServiceAccountToken()
    {
        var client = httpClientFactory.CreateClient();
        var dict = new Dictionary<string, string>
        {
            {"grant_type", "client_credentials"},
            {"client_id", Environment.GetEnvironmentVariable("KeycloakAdminClientId")!},
            {"client_secret", Environment.GetEnvironmentVariable("KeycloakAdminClientSecret")!}
        };

        var response = await client.PostAsync($"{Environment.GetEnvironmentVariable("KeycloakAuthority")}/protocol/openid-connect/token", new FormUrlEncodedContent(dict));
        var content = await response.Content.ReadFromJsonAsync<dynamic>();
        return content!.access_token;
    }

    private object MapToKeycloakUser(Member member, string[]? memberships = null)
    {
        return new
        {
            username = member.Email,
            email = member.Email,
            firstName = member.FirstName,
            lastName = member.LastName,
            enabled = !member.Suspended && PaymentUtils.HasPaidMembershipPayment(member, db),
            attributes = new Dictionary<string, string[]> {
                { "member_memberships", memberships ?? [] },
                { "koala_user_id", new[] { member.Id.ToString() } }
            }
        };
    }
}