using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Backend.Controllers.DTOs;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models;
using Backend.Models.Domain;
using Backend.Services;
using Backend.Services.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Serialization;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Backend.Tests.Services.Domain;

public class GroupServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<PostgresDbContext> _dbOptions;
    private readonly PostgresDbContext _db;
    private readonly IPermissionService _permissionService;
    private readonly IFileCompressService _fileCompressor;
    private readonly IStorageService _storageService;
    private readonly IMemoryCache _memoryCache;
    private readonly AuthOutboxWorker _authOutboxWorker;
    private readonly GroupService _service;
    private readonly Guid _userId = Guid.NewGuid();

    public GroupServiceTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        _dbOptions = new DbContextOptionsBuilder<PostgresDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new TestPostgresDbContext(_dbOptions);
        _db.Database.EnsureCreated();

        _permissionService = Substitute.For<IPermissionService>();
        _fileCompressor = Substitute.For<IFileCompressService>();
        _storageService = Substitute.For<IStorageService>();
        _memoryCache = Substitute.For<IMemoryCache>();
        _authOutboxWorker = Substitute.For<AuthOutboxWorker>(null, null);

        _service = new GroupService(
            _db,
            _permissionService,
            _fileCompressor,
            _storageService,
            _memoryCache,
            _authOutboxWorker,
            NullLogger<GroupService>.Instance
        );
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task GetGroups_ReturnsAllGroups()
    {
        // Arrange
        _db.Groups.Add(new Group { Id = 1, Name = "Committee A", Type = GroupType.Committee });
        _db.Groups.Add(new Group { Id = 2, Name = "Committee B", Type = GroupType.Dispute });
        await _db.SaveChangesAsync();

        var dto = new GetGroupDTO();

        // Act
        var result = await _service.GetGroups(_userId, dto, CancellationToken.None);

        // Assert
        var list = result.ToList();
        Assert.Equal(2, list.Count);
    }

    [Fact]
    public async Task GetGroups_WithMembershipYear_ReturnsOnlyGroupsUserBelongsToInThatYear()
    {
        // Arrange
        var currentMember = new Member
        {
            Id = _userId,
            StudentNumber = "s7654321",
            FirstName = "Current",
            LastName = "Member",
            Email = "current@example.com",
            PhoneNumber = "0612345678",
            Street = "Street",
            HouseNumber = "1",
            PostalCode = "1234AB",
            City = "Enschede",
            DateOfBirth = DateTimeOffset.UtcNow.AddYears(-20)
        };
        var otherMember = new Member
        {
            Id = Guid.NewGuid(),
            StudentNumber = "s1234567",
            FirstName = "Other",
            LastName = "Member",
            Email = "other@example.com",
            PhoneNumber = "0612345678",
            Street = "Street",
            HouseNumber = "1",
            PostalCode = "1234AB",
            City = "Enschede",
            DateOfBirth = DateTimeOffset.UtcNow.AddYears(-20)
        };
        _db.Members.AddRange(currentMember, otherMember);

        var groupUserBelongsTo = new Group { Id = 1, Name = "Committee A", Type = GroupType.Committee };
        var groupUserDoesNotBelongTo = new Group { Id = 2, Name = "Committee B", Type = GroupType.Committee };
        var groupWrongYear = new Group { Id = 3, Name = "Committee C", Type = GroupType.Committee };
        _db.Groups.AddRange(groupUserBelongsTo, groupUserDoesNotBelongTo, groupWrongYear);

        _db.GroupMemberships.Add(new GroupMembership { MemberId = _userId, GroupId = groupUserBelongsTo.Id, MembershipYear = 2026 });
        _db.GroupMemberships.Add(new GroupMembership { MemberId = otherMember.Id, GroupId = groupUserDoesNotBelongTo.Id, MembershipYear = 2026 });
        _db.GroupMemberships.Add(new GroupMembership { MemberId = _userId, GroupId = groupWrongYear.Id, MembershipYear = 2025 });
        await _db.SaveChangesAsync();

        var dto = new GetGroupDTO { MembershipYear = 2026 };

        // Act
        var result = await _service.GetGroups(_userId, dto, CancellationToken.None);

        // Assert
        var list = result.ToList();
        var single = Assert.Single(list);
        Assert.Equal(groupUserBelongsTo.Id, single.Id);
        _permissionService.DidNotReceive().EnsurePermission(_userId, Permission.ManageGroups, Arg.Any<uint?>());
    }

    [Fact]
    public async Task GetGroups_MembershipYearNull_RequiresBoardPermission()
    {
        // Arrange
        _db.Groups.Add(new Group { Id = 1, Name = "Committee A", Type = GroupType.Committee });
        await _db.SaveChangesAsync();

        _permissionService.When(p => p.EnsurePermission(_userId, Permission.ManageGroups, Arg.Any<uint?>()))
            .Do(x => throw new UnauthorizedAccessException());

        var dto = new GetGroupDTO();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.GetGroups(_userId, dto, CancellationToken.None));
    }

    [Fact]
    public async Task GetGroup_Found_ReturnsDto()
    {
        // Arrange
        _db.Groups.Add(new Group { Id = 10, Name = "Group 10", Type = GroupType.Committee });
        await _db.SaveChangesAsync();

        // Act
        var result = await _service.GetGroup(10, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Group 10", result.Name);
    }

    [Fact]
    public async Task GetGroup_NotFound_ReturnsNull()
    {
        // Act
        var result = await _service.GetGroup(999, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateGroup_InvalidName_ThrowsArgumentException()
    {
        // Arrange
        var dto = new PostGroupDTO { Name = "Group;Invalid", Type = GroupType.Committee, GroupPicture = null! };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateGroup(dto, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task CreateGroup_ValidRequest_SavesGroup()
    {
        // Arrange
        var dto = new PostGroupDTO { Name = "Valid Group", Type = GroupType.Committee, GroupPicture = null! };

        // Act
        var result = await _service.CreateGroup(dto, _userId, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Valid Group", result.Name);
        _db.ChangeTracker.Clear();
        var saved = await _db.Groups.FindAsync(result.Id);
        Assert.NotNull(saved);
        Assert.Equal("Valid Group", saved.Name);
    }

    [Fact]
    public async Task CreateGroup_WithImage_SavesGroupAndCompressesImage()
    {
        // Arrange
        var formFile = Substitute.For<IFormFile>();
        formFile.FileName.Returns("group.png");
        formFile.ContentType.Returns("image/png");

        var dto = new PostGroupDTO { Name = "Group with Image", Type = GroupType.Committee, GroupPicture = formFile };

        var compressedStream = new MemoryStream(new byte[] { 1, 2, 3 });
        _fileCompressor.CompressFileAsync(formFile)
            .Returns(Task.FromResult((Stream: (Stream)compressedStream, ContentType: "image/webp")));

        _storageService.SaveFileAsync(Arg.Any<Stream>(), "image/webp", "group-pictures")
            .Returns(Task.FromResult("saved.webp"));

        // Act
        var result = await _service.CreateGroup(dto, _userId, CancellationToken.None);

        // Assert
        Assert.Equal("saved.webp", result.GroupPicturePath);
        Assert.Equal("group.png", result.GroupPictureFileName);
    }

    [Fact]
    public async Task CreateGroup_ThrowsOnSave_RollsBack()
    {
        // Arrange
        var formFile = Substitute.For<IFormFile>();
        formFile.FileName.Returns("group.png");
        formFile.ContentType.Returns("image/png");

        var dto = new PostGroupDTO { Name = "Rollback Group", Type = GroupType.Committee, GroupPicture = formFile };

        _fileCompressor.CompressFileAsync(formFile)
            .Throws(new InvalidOperationException("Compress failed"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateGroup(dto, _userId, CancellationToken.None));

        _db.ChangeTracker.Clear();
        var count = await _db.Groups.CountAsync();
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task GetGroupPictureFile_Cached_ReturnsCachedFile()
    {
        // Arrange
        var cachedBytes = new byte[] { 4, 5, 6 };
        object? cachedVal = (cachedBytes, "image/webp");
        _memoryCache.TryGetValue("group-pic-path.webp", out Arg.Any<object?>())
            .Returns(x =>
            {
                x[1] = cachedVal;
                return true;
            });

        // Act
        var result = await _service.GetGroupPictureFile("path.webp");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("image/webp", result.ContentType);
        using var ms = new MemoryStream();
        await result.Stream.CopyToAsync(ms);
        Assert.Equal(cachedBytes, ms.ToArray());
    }

    [Fact]
    public async Task GetGroupPictureFile_NotCached_LoadsAndCaches()
    {
        // Arrange
        _memoryCache.TryGetValue("group-pic-path.webp", out Arg.Any<object?>()).Returns(false);

        var fileStream = new MemoryStream(new byte[] { 10, 11 });
        var storageFile = new StorageFile(fileStream, "image/png", "path.webp");
        _storageService.GetFileAsync("group-pictures", "path.webp").Returns(Task.FromResult<StorageFile?>(storageFile));

        var mockEntry = Substitute.For<ICacheEntry>();
        _memoryCache.CreateEntry(Arg.Any<object>()).Returns(mockEntry);

        // Act
        var result = await _service.GetGroupPictureFile("path.webp");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("image/png", result.ContentType);
        _memoryCache.Received(1).CreateEntry("group-pic-path.webp");
        mockEntry.Received(1).Value = Arg.Is<(byte[] bytes, string contentType)>(val => val.contentType == "image/png" && val.bytes.SequenceEqual(new byte[] { 10, 11 }));
    }

    [Fact]
    public async Task GetGroupPictureFile_NotFound_ReturnsNull()
    {
        // Arrange
        _memoryCache.TryGetValue("group-pic-path.webp", out Arg.Any<object?>()).Returns(false);
        _storageService.GetFileAsync("group-pictures", "path.webp").Returns(Task.FromResult<StorageFile?>(null));

        // Act
        var result = await _service.GetGroupPictureFile("path.webp");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteGroup_RemovesFromDbAndStorage()
    {
        // Arrange
        var group = new Group { Id = 1, Name = "Delete Me", Type = GroupType.Committee, GroupPicturePath = "pic.webp", GroupPictureFileName = "pic.png" };
        _db.Groups.Add(group);
        await _db.SaveChangesAsync();

        // Act
        await _service.DeleteGroup(1, _userId, CancellationToken.None);

        // Assert
        _db.ChangeTracker.Clear();
        var deleted = await _db.Groups.FindAsync(1u);
        Assert.Null(deleted);
        await _storageService.Received(1).DeleteFileAsync("group-pictures", "pic.webp");
        _memoryCache.Received(1).Remove("group-pic-pic.webp");
    }

    [Fact]
    public async Task PatchGroup_NullPatch_ThrowsArgumentException()
    {
        // Arrange
        var group = new Group { Id = 1, Name = "Group", Type = GroupType.Committee };
        _db.Groups.Add(group);
        await _db.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.PatchGroup(1, _userId, null!, CancellationToken.None));
    }

    [Fact]
    public async Task PatchGroup_ModifiesId_ThrowsArgumentException()
    {
        // Arrange
        var group = new Group { Id = 1, Name = "Group", Type = GroupType.Committee };
        _db.Groups.Add(group);
        await _db.SaveChangesAsync();

        var patchDoc = new JsonPatchDocument<Group>(
            new List<Operation<Group>>
            {
                new Operation<Group>("replace", "/Id", null, 99u)
            },
            new DefaultContractResolver()
        );

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.PatchGroup(1, _userId, patchDoc, CancellationToken.None));
    }

    [Fact]
    public async Task PatchGroup_ValidPatch_UpdatesFields()
    {
        // Arrange
        var group = new Group { Id = 1, Name = "Old Name", Type = GroupType.Committee };
        _db.Groups.Add(group);
        await _db.SaveChangesAsync();

        var patchDoc = new JsonPatchDocument<Group>();
        patchDoc.Replace(g => g.Name, "New Name");

        // Act
        await _service.PatchGroup(1, _userId, patchDoc, CancellationToken.None);

        // Assert
        _db.ChangeTracker.Clear();
        var updated = await _db.Groups.FindAsync(1u);
        Assert.NotNull(updated);
        Assert.Equal("New Name", updated.Name);
    }

    [Fact]
    public async Task UpdateGroup_UpdatesFields()
    {
        // Arrange
        var group = new Group { Id = 1, Name = "Old Name", Type = GroupType.Committee, Active = true };
        _db.Groups.Add(group);
        await _db.SaveChangesAsync();

        var dto = new GroupUpdateDTO { Name = "New Name", Active = false, Type = GroupType.Dispute };

        // Act
        await _service.UpdateGroup(1, _userId, dto, CancellationToken.None);

        // Assert
        _db.ChangeTracker.Clear();
        var updated = await _db.Groups.FindAsync(1u);
        Assert.NotNull(updated);
        Assert.Equal("New Name", updated.Name);
        Assert.False(updated.Active);
        Assert.Equal(GroupType.Dispute, updated.Type);
    }

    [Fact]
    public async Task UploadGroupPicture_NullImage_ClearsPicture()
    {
        // Arrange
        var group = new Group { Id = 1, Name = "Group", Type = GroupType.Committee, GroupPicturePath = "old.webp", GroupPictureFileName = "old.png" };
        _db.Groups.Add(group);
        await _db.SaveChangesAsync();

        // Act
        var result = await _service.UploadGroupPicture(1, _userId, null);

        // Assert
        Assert.Null(result);
        _db.ChangeTracker.Clear();
        var updated = await _db.Groups.FindAsync(1u);
        Assert.NotNull(updated);
        Assert.Null(updated.GroupPicturePath);
        Assert.Null(updated.GroupPictureFileName);
        await _storageService.Received(1).DeleteFileAsync("group-pictures", "old.webp");
        _memoryCache.Received(1).Remove("group-pic-old.webp");
    }

    [Fact]
    public async Task UploadGroupPicture_ValidImage_SavesAndDeletesOld()
    {
        // Arrange
        var group = new Group { Id = 1, Name = "Group", Type = GroupType.Committee, GroupPicturePath = "old.webp", GroupPictureFileName = "old.png" };
        _db.Groups.Add(group);
        await _db.SaveChangesAsync();

        var formFile = Substitute.For<IFormFile>();
        formFile.FileName.Returns("new.png");
        formFile.ContentType.Returns("image/png");

        var compressedStream = new MemoryStream(new byte[] { 1, 2, 3 });
        _fileCompressor.CompressFileAsync(formFile)
            .Returns(Task.FromResult((Stream: (Stream)compressedStream, ContentType: "image/webp")));

        _storageService.SaveFileAsync(Arg.Any<Stream>(), "image/webp", "group-pictures")
            .Returns(Task.FromResult("new.webp"));

        // Act
        var result = await _service.UploadGroupPicture(1, _userId, formFile);

        // Assert
        Assert.Equal("new.webp", result);
        _db.ChangeTracker.Clear();
        var updated = await _db.Groups.FindAsync(1u);
        Assert.NotNull(updated);
        Assert.Equal("new.webp", updated.GroupPicturePath);
        Assert.Equal("new.png", updated.GroupPictureFileName);

        await _storageService.Received(1).DeleteFileAsync("group-pictures", "old.webp");
        _memoryCache.Received(1).Remove("group-pic-old.webp");
    }

    [Fact]
    public async Task UploadGroupPicture_GroupNotFound_ThrowsException()
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() =>
            _service.UploadGroupPicture(999, _userId, null));
        Assert.Equal("Group not found", ex.Message);
    }

    [Fact]
    public async Task UploadGroupPicture_SaveFails_RollsBackTransaction()
    {
        // Arrange
        var group = new Group { Id = 1, Name = "Group", Type = GroupType.Committee, GroupPicturePath = "old.webp", GroupPictureFileName = "old.png" };
        _db.Groups.Add(group);
        await _db.SaveChangesAsync();

        var formFile = Substitute.For<IFormFile>();
        formFile.FileName.Returns("new.png");
        formFile.ContentType.Returns("image/png");

        _fileCompressor.CompressFileAsync(formFile)
            .Returns(Task.FromException<(Stream Stream, string ContentType)>(new Exception("Compression failed")));

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _service.UploadGroupPicture(1, _userId, formFile));

        _db.ChangeTracker.Clear();
        var updated = await _db.Groups.FindAsync(1u);
        Assert.NotNull(updated);
        Assert.Equal("old.webp", updated.GroupPicturePath); // Unchanged - rolled back
    }

    [Fact]
    public async Task GetBoardGroupId_Found_ReturnsValue()
    {
        // Arrange
        _db.Settings.Add(new Setting { Name = "BoardGroupId", Value = "42" });
        await _db.SaveChangesAsync();

        // Act
        var result = await _service.GetBoardGroupId(CancellationToken.None);

        // Assert
        Assert.Equal(42u, result);
    }

    [Fact]
    public async Task GetBoardGroupId_NotFound_ThrowsKeyNotFoundException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.GetBoardGroupId(CancellationToken.None));
    }

    [Fact]
    public async Task GetCandidateBoardGroupId_Found_ReturnsValue()
    {
        // Arrange
        _db.Settings.Add(new Setting { Name = "CandidateBoardGroupId", Value = "84" });
        await _db.SaveChangesAsync();

        // Act
        var result = await _service.GetCandidateBoardGroupId(CancellationToken.None);

        // Assert
        Assert.Equal(84u, result);
    }

    [Fact]
    public async Task GetCandidateBoardGroupId_NotFound_ThrowsKeyNotFoundException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.GetCandidateBoardGroupId(CancellationToken.None));
    }

    [Fact]
    public async Task GetGroupPermissions_NoPermissionsGranted_ReturnsEmptyList()
    {
        var group = new Group { Id = 50, Name = "Empty", Type = GroupType.Committee };
        _db.Groups.Add(group);
        await _db.SaveChangesAsync();

        var result = await _service.GetGroupPermissions(50u, CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetGroupPermissions_ReturnsGrantedPermissionKeys()
    {
        var group = new Group { Id = 51, Name = "WithPerms", Type = GroupType.Committee };
        _db.Groups.Add(group);
        _db.GroupPermissions.Add(new GroupPermission { GroupId = 51, PermissionKey = "ViewFinances" });
        _db.GroupPermissions.Add(new GroupPermission { GroupId = 51, PermissionKey = "CanApproveBudget" });
        await _db.SaveChangesAsync();

        var result = await _service.GetGroupPermissions(51u, CancellationToken.None);

        Assert.Equal(new[] { "CanApproveBudget", "ViewFinances" }, result.OrderBy(p => p));
    }

    [Fact]
    public async Task SetGroupPermissions_ValidKnownAndCustomKeys_ReplacesPermissions()
    {
        var group = new Group { Id = 52, Name = "Target", Type = GroupType.Committee };
        _db.Groups.Add(group);
        _db.GroupPermissions.Add(new GroupPermission { GroupId = 52, PermissionKey = "ManageMembers" });
        await _db.SaveChangesAsync();

        await _service.SetGroupPermissions(
            52u,
            new List<string> { "ViewMembers", "CanApproveBudget" },
            _userId,
            CancellationToken.None);

        _permissionService.Received(1).EnsurePermission(_userId, Permission.ManageGroupPermissions);
        var stored = _db.GroupPermissions.Where(gp => gp.GroupId == 52).Select(gp => gp.PermissionKey).OrderBy(p => p);
        Assert.Equal(new[] { "CanApproveBudget", "ViewMembers" }, stored);
    }

    [Fact]
    public async Task SetGroupPermissions_TooManyCustomPermissions_ThrowsArgumentException()
    {
        var group = new Group { Id = 53, Name = "TooMany", Type = GroupType.Committee };
        _db.Groups.Add(group);
        await _db.SaveChangesAsync();

        var tooMany = Enumerable.Range(0, Backend.Validators.PermissionValidator.MaxCustomPermissionCount + 1)
            .Select(i => $"Custom{i}")
            .ToList();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.SetGroupPermissions(53u, tooMany, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task SetGroupPermissions_CustomPermissionTooLong_ThrowsArgumentException()
    {
        var group = new Group { Id = 54, Name = "TooLong", Type = GroupType.Committee };
        _db.Groups.Add(group);
        await _db.SaveChangesAsync();

        var tooLong = new string('a', Backend.Validators.PermissionValidator.MaxCustomPermissionLength + 1);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.SetGroupPermissions(54u, new List<string> { tooLong }, _userId, CancellationToken.None));
    }
}
