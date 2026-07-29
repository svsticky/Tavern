using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Backend.Services.Domain;
using Hangfire;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Serialization;
using NSubstitute;
using Xunit;

namespace Backend.Tests.Services.Domain;

public class SettingsServiceTests : IDisposable
{
    private readonly PostgresDbContext _db;
    private readonly IPermissionService _permissionServiceMock;
    private readonly ILogger<SettingsService> _loggerMock;
    private readonly IRecurringJobManager _recurringJobManagerMock;
    private readonly SettingsService _service;
    private readonly Guid _userId = Guid.NewGuid();

    public SettingsServiceTests()
    {
        var options = new DbContextOptionsBuilder<PostgresDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db = new PostgresDbContext(options);
        _db.Database.EnsureCreated();

        _permissionServiceMock = Substitute.For<IPermissionService>();
        _loggerMock = Substitute.For<ILogger<SettingsService>>();
        _recurringJobManagerMock = Substitute.For<IRecurringJobManager>();
        _service = new SettingsService(_db, _permissionServiceMock, _loggerMock, _recurringJobManagerMock);
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    [Fact]
    public async Task GetSettings_EnsuresBoardMember_AndReturnsAllSettings()
    {
        // Arrange
        _db.Settings.Add(new Setting { Name = "Setting1", Value = "Val1" });
        _db.Settings.Add(new Setting { Name = "Setting2", Value = "Val2" });
        await _db.SaveChangesAsync();

        // Act
        var result = await _service.GetSettings(_userId, CancellationToken.None);

        // Assert
        _permissionServiceMock.Received(1).EnsureBoardOrCandidateBoardMember(_userId);
        Assert.Equal(2, result.Count());
        Assert.Contains(result, s => s.Name == "Setting1" && s.Value == "Val1");
    }

    [Fact]
    public async Task GetSetting_WithOpenSetting_DoesNotRequireAuthorization()
    {
        // Arrange
        var settingName = "boardgroupid";
        var settingValue = "10";
        _db.Settings.Add(new Setting { Name = settingName, Value = settingValue });
        await _db.SaveChangesAsync();

        // Act
        var result = await _service.GetSetting(settingName, null, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(settingValue, result.Value);
        _permissionServiceMock.DidNotReceiveWithAnyArgs().EnsureBoardOrCandidateBoardMember(default);
    }

    [Fact]
    public async Task GetSetting_WithClosedSetting_AndNoUserId_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var settingName = "closed_setting";

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(async () =>
            await _service.GetSetting(settingName, null, CancellationToken.None));
    }

    [Fact]
    public async Task GetSetting_WithClosedSetting_AndValidUserId_EnsuresBoardMember_AndReturnsSetting()
    {
        // Arrange
        var settingName = "closed_setting";
        var settingValue = "secret";
        _db.Settings.Add(new Setting { Name = settingName, Value = settingValue });
        await _db.SaveChangesAsync();

        // Act
        var result = await _service.GetSetting(settingName, _userId, CancellationToken.None);

        // Assert
        _permissionServiceMock.Received(1).EnsureBoardOrCandidateBoardMember(_userId);
        Assert.NotNull(result);
        Assert.Equal(settingValue, result.Value);
    }

    [Fact]
    public async Task CreateSetting_EnsuresBoardMember_AddsToDb_AndReturnsSetting()
    {
        // Arrange
        var name = "NewSetting";
        var value = "NewValue";

        // Act
        var result = await _service.CreateSetting(name, value, _userId, CancellationToken.None);

        // Assert
        _permissionServiceMock.Received(1).EnsureBoardOrCandidateBoardMember(_userId);
        Assert.NotNull(result);
        Assert.Equal(name, result.Name);
        Assert.Equal(value, result.Value);

        var dbSetting = await _db.Settings.FindAsync(name);
        Assert.NotNull(dbSetting);
        Assert.Equal(value, dbSetting.Value);
    }

    [Fact]
    public async Task UpdateSetting_EnsuresBoardMember_AndUpdatesValue()
    {
        // Arrange
        var name = "boardgroupid"; // Open setting so we can resolve inside UpdateSetting via GetSettingOrThrow
        _db.Settings.Add(new Setting { Name = name, Value = "OldValue" });
        await _db.SaveChangesAsync();

        // Act
        await _service.UpdateSetting(name, "NewValue", _userId, CancellationToken.None);

        // Assert
        _permissionServiceMock.Received(1).EnsureBoardOrCandidateBoardMember(_userId);
        var dbSetting = await _db.Settings.FindAsync(name);
        Assert.Equal("NewValue", dbSetting!.Value);
    }

    [Fact]
    public async Task DeleteSetting_EnsuresBoardMember_AndRemovesFromDb()
    {
        // Arrange
        var name = "boardgroupid";
        _db.Settings.Add(new Setting { Name = name, Value = "Val" });
        await _db.SaveChangesAsync();

        // Act
        await _service.DeleteSetting(name, _userId, CancellationToken.None);

        // Assert
        _permissionServiceMock.Received(1).EnsureBoardOrCandidateBoardMember(_userId);
        var dbSetting = await _db.Settings.FindAsync(name);
        Assert.Null(dbSetting);
    }

    [Fact]
    public async Task DeleteSetting_WhenNotFound_ThrowsKeyNotFoundException()
    {
        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(async () =>
            await _service.DeleteSetting("NonExistent", _userId, CancellationToken.None));
        
        Assert.Contains("Setting with name 'NonExistent' not found.", exception.Message);
    }

    [Fact]
    public async Task PatchSetting_EnsuresBoardMember_AppliesPatchSuccessfully()
    {
        // Arrange
        var name = "boardgroupid";
        _db.Settings.Add(new Setting { Name = name, Value = "Val" });
        await _db.SaveChangesAsync();

        var patchDoc = new JsonPatchDocument<Setting>(new List<Operation<Setting>>
        {
            new Operation<Setting>("replace", "/value", null, "NewPatchedVal")
        }, new DefaultContractResolver());

        // Act
        await _service.PatchSetting(name, patchDoc, _userId, CancellationToken.None);

        // Assert
        _permissionServiceMock.Received(1).EnsureBoardOrCandidateBoardMember(_userId);
        var dbSetting = await _db.Settings.FindAsync(name);
        Assert.Equal("NewPatchedVal", dbSetting!.Value);
    }

    [Fact]
    public async Task PatchSetting_WhenPatchDocNull_ThrowsArgumentException()
    {
        // Arrange
        var name = "boardgroupid";
        _db.Settings.Add(new Setting { Name = name, Value = "Val" });
        await _db.SaveChangesAsync();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _service.PatchSetting(name, null!, _userId, CancellationToken.None));
        
        Assert.Equal("Patch document is null", exception.Message);
    }

    [Fact]
    public async Task PatchSetting_WhenModifyingNameField_ThrowsArgumentException()
    {
        // Arrange
        var name = "boardgroupid";
        _db.Settings.Add(new Setting { Name = name, Value = "Val" });
        await _db.SaveChangesAsync();

        var patchDoc = new JsonPatchDocument<Setting>(new List<Operation<Setting>>
        {
            new Operation<Setting>("replace", "/name", null, "NewName")
        }, new DefaultContractResolver());

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
            await _service.PatchSetting(name, patchDoc, _userId, CancellationToken.None));
        
        Assert.Equal("Cannot modify Name field.", exception.Message);
    }

    [Fact]
    public async Task CreateUpdateDeletePatchSetting_SideEffects_FinancialYearAndCommitteeDates()
    {
        try
        {
            // Create FinancialYearStartDate
            await _service.CreateSetting("FinancialYearStartDate", "09-01", _userId, CancellationToken.None);
            Assert.Equal("09-01", Backend.Utils.DateTime.YearUtils.FinancialYearStartDate);

            // Create CommitteeCreationDate
            await _service.CreateSetting("CommitteeCreationDate", "09-15", _userId, CancellationToken.None);
            Assert.Equal("09-15", Backend.Utils.DateTime.YearUtils.CommitteeCreationDate);

            // Update FinancialYearStartDate
            await _service.UpdateSetting("FinancialYearStartDate", "10-01", _userId, CancellationToken.None);
            Assert.Equal("10-01", Backend.Utils.DateTime.YearUtils.FinancialYearStartDate);

            // Update CommitteeCreationDate
            await _service.UpdateSetting("CommitteeCreationDate", "10-15", _userId, CancellationToken.None);
            Assert.Equal("10-15", Backend.Utils.DateTime.YearUtils.CommitteeCreationDate);

            // Patch FinancialYearStartDate
            var patchFy = new JsonPatchDocument<Setting>(new List<Operation<Setting>>
            {
                new Operation<Setting>("replace", "/value", null, "11-01")
            }, new DefaultContractResolver());
            await _service.PatchSetting("FinancialYearStartDate", patchFy, _userId, CancellationToken.None);
            Assert.Equal("11-01", Backend.Utils.DateTime.YearUtils.FinancialYearStartDate);

            // Patch CommitteeCreationDate
            var patchCc = new JsonPatchDocument<Setting>(new List<Operation<Setting>>
            {
                new Operation<Setting>("replace", "/value", null, "11-15")
            }, new DefaultContractResolver());
            await _service.PatchSetting("CommitteeCreationDate", patchCc, _userId, CancellationToken.None);
            Assert.Equal("11-15", Backend.Utils.DateTime.YearUtils.CommitteeCreationDate);

            // Delete FinancialYearStartDate resets to "08-01"
            await _service.DeleteSetting("FinancialYearStartDate", _userId, CancellationToken.None);
            Assert.Equal("08-01", Backend.Utils.DateTime.YearUtils.FinancialYearStartDate);

            // Delete CommitteeCreationDate resets to "08-01"
            await _service.DeleteSetting("CommitteeCreationDate", _userId, CancellationToken.None);
            Assert.Equal("08-01", Backend.Utils.DateTime.YearUtils.CommitteeCreationDate);
        }
        finally
        {
            Backend.Utils.DateTime.YearUtils.FinancialYearStartDate = "08-01";
            Backend.Utils.DateTime.YearUtils.CommitteeCreationDate = "08-01";
        }
    }
}
