using System.Net.Http.Headers;
using Backend.Database;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class KeycloakSyncService(PostgresDbContext db, IHttpClientFactory httpClientFactory)
{
    public async Task SyncUserMemberships(Guid memberId)
    {
        var memberships = await db.GroupMemberships
            .Where(gm => gm.MemberId == memberId && gm.Group.Active)
            .Select(gm => $"{gm.MembershipYear}:{gm.Group.Name}:{(gm.RoleAlias != null ? gm.RoleAlias.Id : "")}")
            .ToListAsync();

        var client = httpClientFactory.CreateClient("KeycloakAdmin");

        var tokenResponse = await GetServiceAccountToken();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResponse);

        var updateData = new
        {
            attributes = new Dictionary<string, string[]> {
                { "member_memberships", memberships.ToArray() }
            }
        };

        var response = await client.PutAsJsonAsync($"users/{memberId}", updateData);
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
}