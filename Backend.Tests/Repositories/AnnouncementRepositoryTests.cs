using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Backend.Controllers.DTOs;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Backend.Repositories;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Serialization;
using NSubstitute;
using Xunit;

namespace Backend.Tests.Repositories;

public class AnnouncementRepositoryTests : IDisposable
{
    private readonly DbContextOptions<PostgresDbContext> _dbOptions;
    private readonly PostgresDbContext _db;
    private readonly IPermissionService _permissionService;
    private readonly AnnouncementRepository _repository;
    private readonly Guid _userId = Guid.NewGuid();

    public AnnouncementRepositoryTests()
    {
        _dbOptions = new DbContextOptionsBuilder<PostgresDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new PostgresDbContext(_dbOptions);
        _db.Database.EnsureCreated();

        _permissionService = Substitute.For<IPermissionService>();
        _repository = new AnnouncementRepository(
            _db,
            _permissionService,
            NullLogger<AnnouncementRepository>.Instance
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
                Title = $"Ann {i}",
                Content = $"Content {i}",
                CreatedById = creator.Id,
                CreatedAt = DateTimeOffset.UtcNow.AddMinutes(i)
            });
        }
        await _db.SaveChangesAsync();

        // Act
        var result = (await _repository.GetAnnouncements(_userId, CancellationToken.None)).ToList();

        // Assert
        Assert.Equal(20, result.Count);
        // The most recently created announcement (i=25) should be first
        Assert.Equal("Ann 25", result[0].Title);
        Assert.Equal("Ann 6", result[19].Title); // index 19 corresponds to the 20th item, which is i=6
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
            Title = "Topic",
            Content = "Body",
            CreatedById = creator.Id,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.Announcements.Add(ann);
        await _db.SaveChangesAsync();

        // Act
        var result = await _repository.GetAnnouncement(ann.Id, _userId, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Topic", result.Title);
        Assert.Equal("Body", result.Content);
    }

    [Fact]
    public async Task GetAnnouncement_NotFound_ReturnsNull()
    {
        // Act
        var result = await _repository.GetAnnouncement(999, _userId, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAnnouncement_UserNotBoard_ThrowsUnauthorized()
    {
        // Arrange
        var dto = new PostAnnouncementDTO { Title = "T", Content = "C" };
        _permissionService.When(p => p.EnsureBoardOrCandidateBoardMember(_userId))
            .Do(x => throw new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _repository.CreateAnnouncement(_userId, dto, CancellationToken.None));
    }

    [Fact]
    public async Task CreateAnnouncement_ValidData_CreatesAnnouncement()
    {
        // Arrange
        var dto = new PostAnnouncementDTO { Title = "Special Title", Content = "Special Content" };

        // Act
        var result = await _repository.CreateAnnouncement(_userId, dto, CancellationToken.None);

        // Assert
        _permissionService.Received(1).EnsureBoardOrCandidateBoardMember(_userId);
        Assert.True(result.Id > 0);
        Assert.Equal("Special Title", result.Title);

        var saved = await _db.Announcements.FindAsync(result.Id);
        Assert.NotNull(saved);
        Assert.Equal("Special Title", saved.Title);
        Assert.Equal(_userId, saved.CreatedById);
    }

    [Fact]
    public async Task DeleteAnnouncement_AnnouncementNotFound_ThrowsKeyNotFoundException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _repository.DeleteAnnouncement(999u, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteAnnouncement_AnnouncementExists_RemovesFromDatabase()
    {
        // Arrange
        var ann = new Announcement
        {
            Title = "Title",
            Content = "Body",
            CreatedById = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.Announcements.Add(ann);
        await _db.SaveChangesAsync();

        // Act
        await _repository.DeleteAnnouncement(ann.Id, _userId, CancellationToken.None);

        // Assert
        _permissionService.Received(1).EnsureBoardOrCandidateBoardMember(_userId);
        var deleted = await _db.Announcements.FindAsync(ann.Id);
        Assert.Null(deleted);
    }

    [Fact]
    public async Task PatchAnnouncement_NullPatch_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _repository.PatchAnnouncement(1u, null!, _userId, CancellationToken.None));
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
            _repository.PatchAnnouncement(1u, patchDoc, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task PatchAnnouncement_ValidPatch_UpdatesDatabase()
    {
        // Arrange
        var ann = new Announcement
        {
            Title = "Old Title",
            Content = "Body",
            CreatedById = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.Announcements.Add(ann);
        await _db.SaveChangesAsync();

        var patchDoc = new JsonPatchDocument<Announcement>();
        patchDoc.Replace(a => a.Title, "New Title");

        // Act
        await _repository.PatchAnnouncement(ann.Id, patchDoc, _userId, CancellationToken.None);

        // Assert
        _permissionService.Received(1).EnsureBoardOrCandidateBoardMember(_userId);
        var updated = await _db.Announcements.FindAsync(ann.Id);
        Assert.NotNull(updated);
        Assert.Equal("New Title", updated.Title);
    }

    [Fact]
    public async Task UpdateAnnouncement_AnnouncementExists_UpdatesDatabase()
    {
        // Arrange
        var ann = new Announcement
        {
            Title = "Old Title",
            Content = "Old Content",
            CreatedById = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.Announcements.Add(ann);
        await _db.SaveChangesAsync();

        var dto = new UpdateAnnouncementDTO { Title = "New Title", Content = "New Content" };

        // Act
        await _repository.UpdateAnnouncement(ann.Id, dto, _userId, CancellationToken.None);

        // Assert
        _permissionService.Received(1).EnsureBoardOrCandidateBoardMember(_userId);
        var updated = await _db.Announcements.FindAsync(ann.Id);
        Assert.NotNull(updated);
        Assert.Equal("New Title", updated.Title);
        Assert.Equal("New Content", updated.Content);
    }
}
