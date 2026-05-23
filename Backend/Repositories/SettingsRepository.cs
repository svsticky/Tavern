using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Microsoft.AspNetCore.JsonPatch;

namespace Backend.Repositories;

/// <summary>
/// Implements application setting management.
/// </summary>
public class SettingsRepository : ISettingsService
{
    private readonly PostgresDbContext _db;
    private readonly IPermissionService _permissionService;
    private readonly ILogger<SettingsRepository> _logger;
    private readonly string[] _openSettings = new string[] { "boardgroupid", "candidateboardgroupid" };

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsRepository"/> class with the specified dependencies. The constructor takes in a database context for data access, a permission service for enforcing authorization, and a logger for logging operations. It also initializes an array of open setting names that can be accessed without board membership. This setup allows the service to manage application settings while ensuring that only authorized users can create, update, or delete settings, while still allowing certain settings to be accessed by all users.
    /// </summary>
    /// <param name="db">The database context used for data access.</param>
    /// <param name="permissionService">The permission service used to enforce authorization.</param>
    /// <param name="logger">The logger used for logging operations.</param>
    public SettingsRepository(PostgresDbContext db, IPermissionService permissionService, ILogger<SettingsRepository> logger)
    {
        _db = db;
        _permissionService = permissionService;
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<IEnumerable<Setting>> GetSettings(Guid UserId, CancellationToken ct)
    {
        _permissionService.EnsureBoardOrCandidateBoardMember(UserId);

        return Task.FromResult(_db.Settings.AsEnumerable());
    }

    /// <inheritdoc />
    public Task<Setting?> GetSetting(string name, Guid UserId, CancellationToken ct)
    {
        if(!_openSettings.Contains(name.ToLower()))
        {
            _permissionService.EnsureBoardOrCandidateBoardMember(UserId);
        }

        return Task.FromResult(_db.Settings.Find(name));
    }

    /// <inheritdoc />
    public async Task<Setting> CreateSetting(string name, string value, Guid userId, CancellationToken ct)
    {
        _permissionService.EnsureBoardOrCandidateBoardMember(userId);
        _logger.LogInformation("Creating setting {SettingName} by user {UserId}.", name, userId);

        var setting = new Setting
        {
            Name = name,
            Value = value
        };

        _db.Settings.Add(setting);
        await _db.SaveChangesAsync(ct);

        return setting;
    }

    /// <inheritdoc />
    public async Task UpdateSetting(string name, string value, Guid userId, CancellationToken ct)
    {
        _permissionService.EnsureBoardOrCandidateBoardMember(userId);
        _logger.LogInformation("Updating setting {SettingName} by user {UserId}.", name, userId);
        var setting = await GetSettingOrThrow(name, ct);

        setting.Value = value;
        await _db.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task DeleteSetting(string name, Guid userId, CancellationToken ct)
    {
        _permissionService.EnsureBoardOrCandidateBoardMember(userId);
        _logger.LogInformation("Deleting setting {SettingName} by user {UserId}.", name, userId);
        var setting = await GetSettingOrThrow(name, ct);

        _db.Settings.Remove(setting);
        await _db.SaveChangesAsync(ct);
    }

    /// <inheritdoc />
    public async Task PatchSetting(string name, JsonPatchDocument<Setting> patchDoc, Guid userId, CancellationToken ct)
    {
        _permissionService.EnsureBoardOrCandidateBoardMember(userId);
        _logger.LogInformation("Patching setting {SettingName} by user {UserId}.", name, userId);
        var setting = await GetSettingOrThrow(name, ct);

        if (patchDoc == null)
            throw new ArgumentException("Patch document is null");

        if(patchDoc.Operations.Any(op => op.path.Equals("/name", StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("Cannot modify Name field.");

        patchDoc.ApplyTo(setting);

        await _db.SaveChangesAsync(ct);
    }

    private async Task<Setting> GetSettingOrThrow(string name, CancellationToken ct)
    {
        var setting = await _db.Settings.FindAsync(new object[] { name }, ct);

        if (setting == null)
        {
            throw new KeyNotFoundException($"Setting with name '{name}' not found.");
        }

        return setting;
    }
}
