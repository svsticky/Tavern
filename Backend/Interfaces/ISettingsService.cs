using Backend.Models.Domain;
using Microsoft.AspNetCore.JsonPatch;

namespace Backend.Interfaces;

public interface ISettingsService
{
    Task<IEnumerable<Setting>> GetSettings(Guid UserId, CancellationToken ct);
    Task<Setting?> GetSetting(string name, Guid UserId, CancellationToken ct);
    Task<Setting> CreateSetting(string name, string value, Guid userId, CancellationToken ct);
    Task UpdateSetting(string name, string value, Guid userId, CancellationToken ct);
    Task DeleteSetting(string name, Guid userId, CancellationToken ct);
    Task PatchSetting(string name, JsonPatchDocument<Setting> patchDoc, Guid userId, CancellationToken ct);
}