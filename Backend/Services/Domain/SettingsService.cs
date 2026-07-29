using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Microsoft.AspNetCore.JsonPatch;
using Hangfire;

namespace Backend.Services.Domain;

/// <summary>
/// Implements application setting management.
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly PostgresDbContext _db;
    private readonly IPermissionService _permissionService;
    private readonly ILogger<SettingsService> _logger;
    private readonly IRecurringJobManager _recurringJobManager;
    private readonly string[] _openSettings = new []
    {
        "boardgroupid",
        "candidateboardgroupid",
        "membershipprice",
        "mastersshouldpaymembership",
        "membershippaymentexpirationtime",
        "financialyearstartdate",
        "boardchangedate",
        "boardprimarylight",
        "boardprimary",
        "boardprimarydark",
        "studystartdates"
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsService"/> class with the specified dependencies.
    /// </summary>
    /// <param name="db">The database context used for data access.</param>
    /// <param name="permissionService">The permission service used to enforce authorization.</param>
    /// <param name="logger">The logger used for logging operations.</param>
    /// <param name="recurringJobManager">The Hangfire recurring job manager.</param>
    public SettingsService(PostgresDbContext db, IPermissionService permissionService, ILogger<SettingsService> logger, IRecurringJobManager recurringJobManager)
    {
        _db = db;
        _permissionService = permissionService;
        _logger = logger;
        _recurringJobManager = recurringJobManager;
    }

    /// <inheritdoc />
    public Task<IEnumerable<Setting>> GetSettings(Guid UserId, CancellationToken ct)
    {
        _permissionService.EnsureBoardOrCandidateBoardMember(UserId);

        return Task.FromResult(_db.Settings.AsEnumerable());
    }

    /// <inheritdoc />
    public Task<Setting?> GetSetting(string name, Guid? UserId, CancellationToken ct)
    {
        if(!_openSettings.Contains(name, StringComparer.InvariantCultureIgnoreCase))
        {
            if(UserId == null) throw new UnauthorizedAccessException();
            _permissionService.EnsureBoardOrCandidateBoardMember(UserId.Value);
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

        if (name.Equals("FinancialYearStartDate", StringComparison.OrdinalIgnoreCase))
        {
            Backend.Utils.DateTime.YearUtils.FinancialYearStartDate = value;
        }
        else if (name.Equals("CommitteeCreationDate", StringComparison.OrdinalIgnoreCase))
        {
            Backend.Utils.DateTime.YearUtils.CommitteeCreationDate = value;
        }

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

        if (name.Equals("FinancialYearStartDate", StringComparison.OrdinalIgnoreCase))
        {
            Backend.Utils.DateTime.YearUtils.FinancialYearStartDate = value;
        }
        else if (name.Equals("CommitteeCreationDate", StringComparison.OrdinalIgnoreCase))
        {
            Backend.Utils.DateTime.YearUtils.CommitteeCreationDate = value;
        }
    }

    /// <inheritdoc />
    public async Task DeleteSetting(string name, Guid userId, CancellationToken ct)
    {
        _permissionService.EnsureBoardOrCandidateBoardMember(userId);
        _logger.LogInformation("Deleting setting {SettingName} by user {UserId}.", name, userId);
        var setting = await GetSettingOrThrow(name, ct);

        _db.Settings.Remove(setting);
        await _db.SaveChangesAsync(ct);

        if (name.Equals("FinancialYearStartDate", StringComparison.OrdinalIgnoreCase))
        {
            Backend.Utils.DateTime.YearUtils.FinancialYearStartDate = "08-01";
        }
        else if (name.Equals("CommitteeCreationDate", StringComparison.OrdinalIgnoreCase))
        {
            Backend.Utils.DateTime.YearUtils.CommitteeCreationDate = "08-01";
        }
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

        if (name.Equals("FinancialYearStartDate", StringComparison.OrdinalIgnoreCase))
        {
            Backend.Utils.DateTime.YearUtils.FinancialYearStartDate = setting.Value;
        }
        else if (name.Equals("CommitteeCreationDate", StringComparison.OrdinalIgnoreCase))
        {
            Backend.Utils.DateTime.YearUtils.CommitteeCreationDate = setting.Value;
        }
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
