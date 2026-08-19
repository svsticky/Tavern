using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models;
using Backend.Models.Domain;
using Backend.Services.Domain;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Backend.Tests.Services.Domain;

public class MailinglistCurationServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly PostgresDbContext _db;
    private readonly IMailSubscriptionService _mailSubscriptionService;
    private readonly IPermissionService _permissionService;
    private readonly MailinglistCurationService _service;
    private readonly Guid _userId = Guid.NewGuid();

    public MailinglistCurationServiceTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        var dbOptions = new DbContextOptionsBuilder<PostgresDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new TestPostgresDbContext(dbOptions);
        _db.Database.EnsureCreated();

        _mailSubscriptionService = Substitute.For<IMailSubscriptionService>();
        _permissionService = Substitute.For<IPermissionService>();

        _service = new MailinglistCurationService(
            _db,
            _mailSubscriptionService,
            _permissionService,
            NullLogger<MailinglistCurationService>.Instance
        );
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task AddMailinglist_Success_CreatesCurationRecord()
    {
        // Arrange
        var providerLists = new List<MailinglistDto> { new("id_news", "Newsletter") };
        _mailSubscriptionService.GetAvailableMailinglistsAsync(Arg.Any<CancellationToken>()).Returns(providerLists);

        // Act
        var result = await _service.AddMailinglist("id_news", MailinglistVisibility.General, _userId, CancellationToken.None);

        // Assert
        Assert.Equal("id_news", result.ProviderListId);
        Assert.Equal("Newsletter", result.Name);
        Assert.Equal(MailinglistVisibility.General, result.Visibility);
        Assert.False(result.Orphaned);
        _permissionService.Received(1).EnsureBoardOrCandidateBoardMember(_userId);
        Assert.Single(_db.CuratedMailinglists);
    }

    [Fact]
    public async Task AddMailinglist_NotAtProvider_ThrowsArgumentException()
    {
        // Arrange
        _mailSubscriptionService.GetAvailableMailinglistsAsync(Arg.Any<CancellationToken>()).Returns(new List<MailinglistDto>());

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.AddMailinglist("id_missing", MailinglistVisibility.General, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task AddMailinglist_AlreadyCurated_ThrowsArgumentException()
    {
        // Arrange
        _db.CuratedMailinglists.Add(new CuratedMailinglist { ProviderListId = "id_news", Visibility = MailinglistVisibility.General });
        await _db.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.AddMailinglist("id_news", MailinglistVisibility.General, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task AddMailinglist_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _permissionService.When(p => p.EnsureBoardOrCandidateBoardMember(_userId))
            .Do(x => throw new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.AddMailinglist("id_news", MailinglistVisibility.General, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task GetAddableProviderMailinglists_ExcludesAlreadyCurated()
    {
        // Arrange
        _db.CuratedMailinglists.Add(new CuratedMailinglist { ProviderListId = "id_news", Visibility = MailinglistVisibility.General });
        await _db.SaveChangesAsync();

        var providerLists = new List<MailinglistDto> { new("id_news", "Newsletter"), new("id_events", "Events") };
        _mailSubscriptionService.GetAvailableMailinglistsAsync(Arg.Any<CancellationToken>()).Returns(providerLists);

        // Act
        var result = await _service.GetAddableProviderMailinglists(_userId, CancellationToken.None);

        // Assert
        var single = Assert.Single(result);
        Assert.Equal("id_events", single.Id);
    }

    [Fact]
    public async Task GetAddableProviderMailinglists_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _permissionService.When(p => p.EnsureBoardOrCandidateBoardMember(_userId))
            .Do(x => throw new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.GetAddableProviderMailinglists(_userId, CancellationToken.None));
    }

    [Fact]
    public async Task GetCuratedMailinglists_MarksMissingProviderListAsOrphaned()
    {
        // Arrange
        _db.CuratedMailinglists.Add(new CuratedMailinglist { ProviderListId = "id_news", Visibility = MailinglistVisibility.General });
        _db.CuratedMailinglists.Add(new CuratedMailinglist { ProviderListId = "id_deleted", Visibility = MailinglistVisibility.YearlyRenewalOnly });
        await _db.SaveChangesAsync();

        var providerLists = new List<MailinglistDto> { new("id_news", "Newsletter") };
        _mailSubscriptionService.GetAvailableMailinglistsAsync(Arg.Any<CancellationToken>()).Returns(providerLists);

        // Act
        var result = (await _service.GetCuratedMailinglists(_userId, CancellationToken.None)).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        var news = result.Single(r => r.ProviderListId == "id_news");
        Assert.False(news.Orphaned);
        Assert.Equal("Newsletter", news.Name);

        var deleted = result.Single(r => r.ProviderListId == "id_deleted");
        Assert.True(deleted.Orphaned);
        Assert.Null(deleted.Name);
    }

    [Fact]
    public async Task GetCuratedMailinglists_Unauthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        _permissionService.When(p => p.EnsureBoardOrCandidateBoardMember(_userId))
            .Do(x => throw new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.GetCuratedMailinglists(_userId, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateMailinglistVisibility_Success_UpdatesVisibility()
    {
        // Arrange
        var curated = new CuratedMailinglist { ProviderListId = "id_news", Visibility = MailinglistVisibility.General };
        _db.CuratedMailinglists.Add(curated);
        await _db.SaveChangesAsync();

        // Act
        await _service.UpdateMailinglistVisibility(curated.Id, MailinglistVisibility.YearlyRenewalOnly, _userId, CancellationToken.None);

        // Assert
        _db.ChangeTracker.Clear();
        var updated = await _db.CuratedMailinglists.FindAsync(curated.Id);
        Assert.Equal(MailinglistVisibility.YearlyRenewalOnly, updated!.Visibility);
    }

    [Fact]
    public async Task UpdateMailinglistVisibility_NotFound_ThrowsKeyNotFoundException()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.UpdateMailinglistVisibility(999, MailinglistVisibility.General, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteMailinglist_Success_RemovesCurationRecordOnly()
    {
        // Arrange
        var curated = new CuratedMailinglist { ProviderListId = "id_news", Visibility = MailinglistVisibility.General };
        _db.CuratedMailinglists.Add(curated);
        await _db.SaveChangesAsync();

        // Act
        await _service.DeleteMailinglist(curated.Id, _userId, CancellationToken.None);

        // Assert
        Assert.Empty(_db.CuratedMailinglists);
        // Deleting curation never calls the provider - it only touches the local record.
        await _mailSubscriptionService.DidNotReceiveWithAnyArgs().DeleteMemberAsync(default!, default);
    }

    [Fact]
    public async Task DeleteMailinglist_NotFound_ThrowsKeyNotFoundException()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.DeleteMailinglist(999, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task GetVisibleProviderListIds_GeneralOnly_ExcludesYearlyRenewalOnly()
    {
        // Arrange
        _db.CuratedMailinglists.Add(new CuratedMailinglist { ProviderListId = "id_news", Visibility = MailinglistVisibility.General });
        _db.CuratedMailinglists.Add(new CuratedMailinglist { ProviderListId = "id_alumni", Visibility = MailinglistVisibility.YearlyRenewalOnly });
        await _db.SaveChangesAsync();

        // Act
        var result = await _service.GetVisibleProviderListIds(includeYearlyRenewalOnly: false, CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Contains("id_news", result);
    }

    [Fact]
    public async Task GetVisibleProviderListIds_IncludeYearlyRenewal_IncludesBoth()
    {
        // Arrange
        _db.CuratedMailinglists.Add(new CuratedMailinglist { ProviderListId = "id_news", Visibility = MailinglistVisibility.General });
        _db.CuratedMailinglists.Add(new CuratedMailinglist { ProviderListId = "id_alumni", Visibility = MailinglistVisibility.YearlyRenewalOnly });
        await _db.SaveChangesAsync();

        // Act
        var result = await _service.GetVisibleProviderListIds(includeYearlyRenewalOnly: true, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains("id_news", result);
        Assert.Contains("id_alumni", result);
    }
}
