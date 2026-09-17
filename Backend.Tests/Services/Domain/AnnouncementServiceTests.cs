using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Backend.Controllers.DTOs;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models;
using Backend.Models.Domain;
using Backend.Services.Domain;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Serialization;
using NSubstitute;
using Xunit;

namespace Backend.Tests.Services.Domain;

public class AnnouncementServiceTests : IDisposable
{
    private readonly DbContextOptions<PostgresDbContext> _dbOptions;
    private readonly PostgresDbContext _db;
    private readonly IPermissionService _permissionService;
    private readonly AnnouncementService _service;
    private readonly Guid _userId = Guid.NewGuid();

    public AnnouncementServiceTests()
    {
        _dbOptions = new DbContextOptionsBuilder<PostgresDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new PostgresDbContext(_dbOptions);
        _db.Database.EnsureCreated();

        _permissionService = Substitute.For<IPermissionService>();
        _service = new AnnouncementService(
            _db,
            _permissionService,
            NullLogger<AnnouncementService>.Instance
        );
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    [Fact]
    public async Task GetAnnouncements_ReturnsTop20AnnouncementsOrderedByCreatedAt()
    {
        // Arrange
        var creator = new Member
        {
            Id = Guid.NewGuid(),
            StudentNumber = "s1234567",
            FirstName = "Jane",
            LastName = "Doe",
            Email = "jane@example.com",
            PhoneNumber = "0612345678",
            Street = "Main St",
            HouseNumber = "1",
            PostalCode = "1234AB",
            City = "Enschede"
        };
        _db.Members.Add(creator);

        for (uint i = 1; i <= 25; i++)
        {
            _db.Announcements.Add(new Announcement
            {
                Id = i,
                TitleDutch = $"Ann NL {i}",
                TitleEnglish = $"Ann EN {i}",
                ContentDutch = $"Inhoud {i}",
                ContentEnglish = $"Content {i}",
                CreatedById = creator.Id,
                CreatedAt = DateTimeOffset.UtcNow.AddMinutes(i)
            });
        }
        await _db.SaveChangesAsync();

        // Act
        var result = (await _service.GetAnnouncements(_userId, CancellationToken.None)).ToList();

        // Assert
        Assert.Equal(20, result.Count);
        // The most recently created announcement (i=25) should be first
        Assert.Equal("Ann NL 25", result[0].TitleDutch);
        Assert.Equal("Ann NL 6", result[19].TitleDutch); // index 19 corresponds to the 20th item, which is i=6
    }

    [Fact]
    public async Task GetAnnouncement_Found_ReturnsDto()
    {
        // Arrange
        var creator = new Member
        {
            Id = Guid.NewGuid(),
            StudentNumber = "s1234567",
            FirstName = "Jane",
            LastName = "Doe",
            Email = "jane@example.com",
            PhoneNumber = "0612345678",
            Street = "Main St",
            HouseNumber = "1",
            PostalCode = "1234AB",
            City = "Enschede"
        };
        _db.Members.Add(creator);

        var ann = new Announcement
        {
            TitleDutch = "Topic NL",
            TitleEnglish = "Topic EN",
            ContentDutch = "Body NL",
            ContentEnglish = "Body EN",
            CreatedById = creator.Id,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.Announcements.Add(ann);
        await _db.SaveChangesAsync();

        // Act
        var result = await _service.GetAnnouncement(ann.Id, _userId, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Topic NL", result.TitleDutch);
        Assert.Equal("Body NL", result.ContentDutch);
    }

    [Fact]
    public async Task GetAnnouncement_NotFound_ReturnsNull()
    {
        // Act
        var result = await _service.GetAnnouncement(999, _userId, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAnnouncement_UserNotBoard_ThrowsUnauthorized()
    {
        // Arrange
        var dto = new PostAnnouncementDTO { TitleDutch = "T NL", TitleEnglish = "T EN", ContentDutch = "C NL", ContentEnglish = "C EN" };
        _permissionService.When(p => p.EnsurePermission(_userId, Permission.EditAnnouncements, Arg.Any<uint?>()))
            .Do(x => throw new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.CreateAnnouncement(_userId, dto, CancellationToken.None));
    }

    [Fact]
    public async Task CreateAnnouncement_ValidData_CreatesAnnouncement()
    {
        // Arrange
        var dto = new PostAnnouncementDTO { TitleDutch = "Special Title NL", TitleEnglish = "Special Title EN", ContentDutch = "Special Content NL", ContentEnglish = "Special Content EN" };

        // Act
        var result = await _service.CreateAnnouncement(_userId, dto, CancellationToken.None);

        // Assert
        _permissionService.Received(1).EnsurePermission(_userId, Permission.EditAnnouncements, Arg.Any<uint?>());
        Assert.True(result.Id > 0);
        Assert.Equal("Special Title NL", result.TitleDutch);

        var saved = await _db.Announcements.FindAsync(result.Id);
        Assert.NotNull(saved);
        Assert.Equal("Special Title NL", saved.TitleDutch);
        Assert.Equal(_userId, saved.CreatedById);
    }

    [Fact]
    public async Task DeleteAnnouncement_AnnouncementNotFound_ThrowsKeyNotFoundException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.DeleteAnnouncement(999u, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteAnnouncement_AnnouncementExists_RemovesFromDatabase()
    {
        // Arrange
        var ann = new Announcement
        {
            TitleDutch = "Title NL",
            TitleEnglish = "Title EN",
            ContentDutch = "Body NL",
            ContentEnglish = "Body EN",
            CreatedById = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.Announcements.Add(ann);
        await _db.SaveChangesAsync();

        // Act
        await _service.DeleteAnnouncement(ann.Id, _userId, CancellationToken.None);

        // Assert
        _permissionService.Received(1).EnsurePermission(_userId, Permission.EditAnnouncements, Arg.Any<uint?>());
        var deleted = await _db.Announcements.FindAsync(ann.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task PatchAnnouncement_NullPatch_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.PatchAnnouncement(1u, null!, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task PatchAnnouncement_ModifiesRestrictedFields_ThrowsArgumentException()
    {
        // Arrange
        var patchDoc = new JsonPatchDocument<Announcement>(
            new List<Operation<Announcement>>
            {
                new Operation<Announcement>("replace", "/createdById", null, Guid.NewGuid())
            },
            new DefaultContractResolver()
        );

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.PatchAnnouncement(1u, patchDoc, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task PatchAnnouncement_ValidPatch_UpdatesDatabase()
    {
        // Arrange
        var ann = new Announcement
        {
            TitleDutch = "Old Title NL",
            TitleEnglish = "Old Title EN",
            ContentDutch = "Body NL",
            ContentEnglish = "Body EN",
            CreatedById = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.Announcements.Add(ann);
        await _db.SaveChangesAsync();

        var patchDoc = new JsonPatchDocument<Announcement>();
        patchDoc.Replace(a => a.TitleDutch, "New Title NL");

        // Act
        await _service.PatchAnnouncement(ann.Id, patchDoc, _userId, CancellationToken.None);

        // Assert
        _permissionService.Received(1).EnsurePermission(_userId, Permission.EditAnnouncements, Arg.Any<uint?>());
        var updated = await _db.Announcements.FindAsync(ann.Id);
        Assert.NotNull(updated);
        Assert.Equal("New Title NL", updated.TitleDutch);
    }

    [Fact]
    public async Task UpdateAnnouncement_AnnouncementExists_UpdatesDatabase()
    {
        // Arrange
        var ann = new Announcement
        {
            TitleDutch = "Old Title NL",
            TitleEnglish = "Old Title EN",
            ContentDutch = "Old Content NL",
            ContentEnglish = "Old Content EN",
            CreatedById = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.Announcements.Add(ann);
        await _db.SaveChangesAsync();

        var dto = new UpdateAnnouncementDTO { TitleDutch = "New Title NL", TitleEnglish = "New Title EN", ContentDutch = "New Content NL", ContentEnglish = "New Content EN" };

        // Act
        await _service.UpdateAnnouncement(ann.Id, dto, _userId, CancellationToken.None);

        // Assert
        _permissionService.Received(1).EnsurePermission(_userId, Permission.EditAnnouncements, Arg.Any<uint?>());
        var updated = await _db.Announcements.FindAsync(ann.Id);
        Assert.NotNull(updated);
        Assert.Equal("New Title NL", updated.TitleDutch);
        Assert.Equal("New Content NL", updated.ContentDutch);
    }
}
