using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Microsoft.AspNetCore.JsonPatch;

namespace Backend.Services;

public class SettingsService : ISettingsService
{
    private readonly PostgresDbContext _db;
    private readonly IPermissionService _permissionService;

    public SettingsService(PostgresDbContext db, IPermissionService permissionService)
    {
        _db = db;
        _permissionService = permissionService;
    }

    public Task<IEnumerable<Setting>> GetSettings(Guid UserId, CancellationToken ct)
    {
        _permissionService.EnsureBoardOrCandidateBoardMember(UserId);

        return Task.FromResult(_db.Settings.AsEnumerable());
    }

    public Task<Setting?> GetSetting(string name, Guid UserId, CancellationToken ct)
    {
        if(!name.Equals("CandidateBoardGroupId") && !name.Equals("BoardGroupId"))
        {
            _permissionService.EnsureBoardOrCandidateBoardMember(UserId);
        }

        return Task.FromResult(_db.Settings.Find(name));
    }

    public async Task<Setting> CreateSetting(string name, string value, Guid userId, CancellationToken ct)
    {
        _permissionService.EnsureBoardOrCandidateBoardMember(userId);

        var setting = new Setting
        {
            Name = name,
            Value = value
        };

        _db.Settings.Add(setting);
        await _db.SaveChangesAsync(ct);

        return setting;
    }

    public async Task UpdateSetting(string name, string value, Guid userId, CancellationToken ct)
    {
        _permissionService.EnsureBoardOrCandidateBoardMember(userId);


        var setting = await _db.Settings.FindAsync(new object[] { name }, ct);

        if (setting == null)
        {
            throw new KeyNotFoundException($"Setting with name '{name}' not found.");
        }

        setting.Value = value;
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteSetting(string name, Guid userId, CancellationToken ct)
    {
        _permissionService.EnsureBoardOrCandidateBoardMember(userId);

        var setting = await _db.Settings.FindAsync(new object[] { name }, ct);

        if (setting == null)
        {
            throw new KeyNotFoundException($"Setting with name '{name}' not found.");
        }

        _db.Settings.Remove(setting);
        await _db.SaveChangesAsync(ct);
    }

    public async Task PatchSetting(string name, JsonPatchDocument<Setting> patchDoc, Guid userId, CancellationToken ct)
    {
        _permissionService.EnsureBoardOrCandidateBoardMember(userId);

        var setting = await _db.Settings.FindAsync(new object[] { name }, ct);

        if (setting == null)
        {
            throw new KeyNotFoundException($"Setting with name '{name}' not found.");
        }

        patchDoc.ApplyTo(setting);

        await _db.SaveChangesAsync(ct);
    }
}