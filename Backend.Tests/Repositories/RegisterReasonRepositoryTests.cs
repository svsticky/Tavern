using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Backend.Controllers.DTOs;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Backend.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Backend.Tests.Repositories;

public class RegisterReasonRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<PostgresDbContext> _dbOptions;
    private readonly PostgresDbContext _db;
    private readonly IPermissionService _permissionService;
    private readonly IFileCompressService _fileCompressor;
    private readonly IStorageService _storageService;
    private readonly IMemoryCache _memoryCache;
    private readonly RegisterReasonRepository _repository;
    private readonly Guid _userId = Guid.NewGuid();

    public RegisterReasonRepositoryTests()
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

        _repository = new RegisterReasonRepository(
            _db,
            _permissionService,
            _fileCompressor,
            _storageService,
            _memoryCache,
            NullLogger<RegisterReasonRepository>.Instance
        );
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task GetRegisterReasons_ReturnsSortedReasons()
    {
        // Arrange
        _db.RegisterReasons.Add(new RegisterReason { Id = 1, TitleDutch = "A", TitleEnglish = "A", DescriptionDutch = "D", DescriptionEnglish = "E", SortOrder = 2 });
        _db.RegisterReasons.Add(new RegisterReason { Id = 2, TitleDutch = "B", TitleEnglish = "B", DescriptionDutch = "D", DescriptionEnglish = "E", SortOrder = 1 });
        await _db.SaveChangesAsync();

        // Act
        var result = await _repository.GetRegisterReasons(CancellationToken.None);

        // Assert
        var list = result.ToList();
        Assert.Equal(2, list.Count);
        Assert.Equal(2, list[0].Id); // SortOrder 1 comes first
        Assert.Equal(1, list[1].Id); // SortOrder 2 comes second
    }

    [Fact]
    public async Task GetRegisterReason_Found_ReturnsDto()
    {
        // Arrange
        _db.RegisterReasons.Add(new RegisterReason { Id = 10, TitleDutch = "A", TitleEnglish = "A", DescriptionDutch = "D", DescriptionEnglish = "E", SortOrder = 1 });
        await _db.SaveChangesAsync();

        // Act
        var result = await _repository.GetRegisterReason(10, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("A", result.TitleDutch);
    }

    [Fact]
    public async Task GetRegisterReason_NotFound_ReturnsNull()
    {
        // Act
        var result = await _repository.GetRegisterReason(999, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateRegisterReason_UserNotBoard_ThrowsUnauthorized()
    {
        // Arrange
        var dto = new PostRegisterReasonDTO { TitleDutch = "A", TitleEnglish = "A", DescriptionDutch = "D", DescriptionEnglish = "E" };
        _permissionService.When(p => p.EnsureBoardOrCandidateBoardMember(_userId))
            .Do(x => throw new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _repository.CreateRegisterReason(dto, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task CreateRegisterReason_WithSortOrder_SavesCorrectly()
    {
        // Arrange
        var dto = new PostRegisterReasonDTO
        {
            TitleDutch = "A",
            TitleEnglish = "A",
            DescriptionDutch = "D",
            DescriptionEnglish = "D",
            SortOrder = 5
        };

        // Act
        var result = await _repository.CreateRegisterReason(dto, _userId, CancellationToken.None);

        // Assert
        _permissionService.Received(1).EnsureBoardOrCandidateBoardMember(_userId);
        Assert.Equal(5, result.SortOrder);
        var saved = await _db.RegisterReasons.FindAsync(result.Id);
        Assert.NotNull(saved);
        Assert.Equal("A", saved.TitleDutch);
    }

    [Fact]
    public async Task CreateRegisterReason_WithoutSortOrder_AutoIncrements()
    {
        // Arrange
        _db.RegisterReasons.Add(new RegisterReason { Id = 1, TitleDutch = "A", TitleEnglish = "A", DescriptionDutch = "D", DescriptionEnglish = "E", SortOrder = 10 });
        await _db.SaveChangesAsync();

        var dto = new PostRegisterReasonDTO { TitleDutch = "B", TitleEnglish = "B", DescriptionDutch = "D", DescriptionEnglish = "E" };

        // Act
        var result = await _repository.CreateRegisterReason(dto, _userId, CancellationToken.None);

        // Assert
        Assert.Equal(11, result.SortOrder);
    }

    [Fact]
    public async Task UpdateRegisterReason_NotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var dto = new RegisterReasonUpdateDTO { TitleDutch = "A", TitleEnglish = "A", DescriptionDutch = "D", DescriptionEnglish = "E", SortOrder = 1 };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _repository.UpdateRegisterReason(999, dto, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateRegisterReason_UpdatesFields()
    {
        // Arrange
        var reason = new RegisterReason { Id = 1, TitleDutch = "Old", TitleEnglish = "Old", DescriptionDutch = "Old", DescriptionEnglish = "Old", SortOrder = 1 };
        _db.RegisterReasons.Add(reason);
        await _db.SaveChangesAsync();

        var dto = new RegisterReasonUpdateDTO
        {
            TitleDutch = "New",
            TitleEnglish = "New",
            DescriptionDutch = "Desc",
            DescriptionEnglish = "Desc",
            SortOrder = 2
        };

        // Act
        await _repository.UpdateRegisterReason(1, dto, _userId, CancellationToken.None);

        // Assert
        _db.ChangeTracker.Clear();
        var updated = await _db.RegisterReasons.FindAsync(1);
        Assert.NotNull(updated);
        Assert.Equal("New", updated.TitleDutch);
        Assert.Equal(2, updated.SortOrder);
    }

    [Fact]
    public async Task DeleteRegisterReason_RemovesFromDbAndStorage()
    {
        // Arrange
        var reason = new RegisterReason { Id = 1, TitleDutch = "A", TitleEnglish = "A", DescriptionDutch = "D", DescriptionEnglish = "E", SortOrder = 1, IconPath = "icon.webp" };
        _db.RegisterReasons.Add(reason);
        await _db.SaveChangesAsync();

        // Act
        await _repository.DeleteRegisterReason(1, _userId, CancellationToken.None);

        // Assert
        _db.ChangeTracker.Clear();
        var deleted = await _db.RegisterReasons.FindAsync(1);
        Assert.Null(deleted);
        await _storageService.Received(1).DeleteFileAsync("register-reason-icons", "icon.webp");
        _memoryCache.Received(1).Remove("reason-icon-icon.webp");
    }

    [Fact]
    public async Task UploadRegisterReasonIcon_NullIcon_ClearsIcon()
    {
        // Arrange
        var reason = new RegisterReason { Id = 1, TitleDutch = "A", TitleEnglish = "A", DescriptionDutch = "D", DescriptionEnglish = "E", SortOrder = 1, IconPath = "old.webp", IconFileName = "old.png" };
        _db.RegisterReasons.Add(reason);
        await _db.SaveChangesAsync();

        // Act
        var result = await _repository.UploadRegisterReasonIcon(1, _userId, null);

        // Assert
        Assert.Null(result);
        _db.ChangeTracker.Clear();
        var updated = await _db.RegisterReasons.FindAsync(1);
        Assert.NotNull(updated);
        Assert.Null(updated.IconPath);
        Assert.Null(updated.IconFileName);
        await _storageService.Received(1).DeleteFileAsync("register-reason-icons", "old.webp");
        _memoryCache.Received(1).Remove("reason-icon-old.webp");
    }

    [Fact]
    public async Task UploadRegisterReasonIcon_ValidIcon_SavesAndDeletesOld()
    {
        // Arrange
        var reason = new RegisterReason { Id = 1, TitleDutch = "A", TitleEnglish = "A", DescriptionDutch = "D", DescriptionEnglish = "E", SortOrder = 1, IconPath = "old.webp", IconFileName = "old.png" };
        _db.RegisterReasons.Add(reason);
        await _db.SaveChangesAsync();

        var formFile = Substitute.For<IFormFile>();
        formFile.FileName.Returns("new.png");
        formFile.ContentType.Returns("image/png");

        var compressedStream = new MemoryStream(new byte[] { 1, 2, 3 });
        _fileCompressor.CompressFileAsync(formFile)
            .Returns(Task.FromResult((Stream: (Stream)compressedStream, ContentType: "image/webp")));

        _storageService.SaveFileAsync(Arg.Any<Stream>(), "image/webp", "register-reason-icons")
            .Returns(Task.FromResult("new.webp"));

        // Act
        var result = await _repository.UploadRegisterReasonIcon(1, _userId, formFile);

        // Assert
        Assert.Equal("new.webp", result);
        _db.ChangeTracker.Clear();
        var updated = await _db.RegisterReasons.FindAsync(1);
        Assert.NotNull(updated);
        Assert.Equal("new.webp", updated.IconPath);
        Assert.Equal("new.png", updated.IconFileName);

        await _storageService.Received(1).DeleteFileAsync("register-reason-icons", "old.webp");
        _memoryCache.Received(1).Remove("reason-icon-old.webp");
    }

    [Fact]
    public async Task UploadRegisterReasonIcon_CompressThrows_RollsBackTransaction()
    {
        // Arrange
        var reason = new RegisterReason { Id = 1, TitleDutch = "A", TitleEnglish = "A", DescriptionDutch = "D", DescriptionEnglish = "E", SortOrder = 1 };
        _db.RegisterReasons.Add(reason);
        await _db.SaveChangesAsync();

        var formFile = Substitute.For<IFormFile>();
        formFile.FileName.Returns("new.png");
        formFile.ContentType.Returns("image/png");

        _fileCompressor.CompressFileAsync(formFile)
            .Throws(new InvalidOperationException("Failed compress"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _repository.UploadRegisterReasonIcon(1, _userId, formFile));

        _db.ChangeTracker.Clear();
        var updated = await _db.RegisterReasons.FindAsync(1);
        Assert.NotNull(updated);
        Assert.Null(updated.IconPath); // Verification that rollback worked and changes weren't saved
    }

    [Fact]
    public async Task GetRegisterReasonIconFile_Cached_ReturnsCachedFile()
    {
        // Arrange
        var cachedBytes = new byte[] { 4, 5, 6 };
        object? cachedVal = (cachedBytes, "image/webp");
        _memoryCache.TryGetValue("reason-icon-path.webp", out Arg.Any<object?>())
            .Returns(x => {
                x[1] = cachedVal;
                return true;
            });

        // Act
        var result = await _repository.GetRegisterReasonIconFile("path.webp");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("image/webp", result.ContentType);
        using var ms = new MemoryStream();
        await result.Stream.CopyToAsync(ms);
        Assert.Equal(cachedBytes, ms.ToArray());
    }

    [Fact]
    public async Task GetRegisterReasonIconFile_NotCached_LoadsFromStorageAndCaches()
    {
        // Arrange
        _memoryCache.TryGetValue("reason-icon-path.webp", out Arg.Any<object?>()).Returns(false);

        var mockEntry = Substitute.For<ICacheEntry>();
        _memoryCache.CreateEntry(Arg.Any<object>()).Returns(mockEntry);

        var fileStream = new MemoryStream(new byte[] { 10, 11 });
        var storageFile = new StorageFile(fileStream, "image/png", "path.webp");
        _storageService.GetFileAsync("register-reason-icons", "path.webp").Returns(Task.FromResult<StorageFile?>(storageFile));

        // Act
        var result = await _repository.GetRegisterReasonIconFile("path.webp");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("image/png", result.ContentType);
        _memoryCache.Received(1).CreateEntry("reason-icon-path.webp");
        mockEntry.Received(1).Value = Arg.Is<(byte[] bytes, string contentType)>(val => val.contentType == "image/png" && val.bytes.SequenceEqual(new byte[] { 10, 11 }));
    }

    [Fact]
    public async Task GetRegisterReasonIconFile_NotFound_ReturnsNull()
    {
        // Arrange
        _memoryCache.TryGetValue("reason-icon-path.webp", out Arg.Any<object?>()).Returns(false);
        _storageService.GetFileAsync("register-reason-icons", "path.webp").Returns(Task.FromResult<StorageFile?>(null));

        // Act
        var result = await _repository.GetRegisterReasonIconFile("path.webp");

        // Assert
        Assert.Null(result);
    }
}
