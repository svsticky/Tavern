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
using Backend.Services.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Backend.Tests.Services.Domain;

public class RegisterSlideServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<PostgresDbContext> _dbOptions;
    private readonly PostgresDbContext _db;
    private readonly IPermissionService _permissionService;
    private readonly IFileCompressService _fileCompressor;
    private readonly IStorageService _storageService;
    private readonly IMemoryCache _memoryCache;
    private readonly RegisterSlideService _service;
    private readonly Guid _userId = Guid.NewGuid();

    public RegisterSlideServiceTests()
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

        _service = new RegisterSlideService(
            _db,
            _permissionService,
            _fileCompressor,
            _storageService,
            _memoryCache,
            NullLogger<RegisterSlideService>.Instance
        );
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task GetRegisterSlides_ReturnsSortedSlides()
    {
        // Arrange
        _db.RegisterSlides.Add(new RegisterSlide { Id = 1, SortOrder = 2, ImagePath = "p1", ImageFileName = "f1" });
        _db.RegisterSlides.Add(new RegisterSlide { Id = 2, SortOrder = 1, ImagePath = "p2", ImageFileName = "f2" });
        await _db.SaveChangesAsync();

        // Act
        var result = await _service.GetRegisterSlides(CancellationToken.None);

        // Assert
        var list = result.ToList();
        Assert.Equal(2, list.Count);
        Assert.Equal(2, list[0].Id); // SortOrder 1 comes first
        Assert.Equal(1, list[1].Id); // SortOrder 2 comes second
    }

    [Fact]
    public async Task GetRegisterSlide_Found_ReturnsDto()
    {
        // Arrange
        _db.RegisterSlides.Add(new RegisterSlide { Id = 10, SortOrder = 1, ImagePath = "p10", ImageFileName = "f10" });
        await _db.SaveChangesAsync();

        // Act
        var result = await _service.GetRegisterSlide(10, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("p10", result.ImagePath);
    }

    [Fact]
    public async Task GetRegisterSlide_NotFound_ReturnsNull()
    {
        // Act
        var result = await _service.GetRegisterSlide(999, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateRegisterSlide_UserNotBoard_ThrowsUnauthorized()
    {
        // Arrange
        var formFile = Substitute.For<IFormFile>();
        formFile.FileName.Returns("pic.png");
        formFile.ContentType.Returns("image/png");

        var dto = new PostRegisterSlideDTO { Image = formFile };
        _permissionService.When(p => p.EnsureBoardOrCandidateBoardMember(_userId))
            .Do(x => throw new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.CreateRegisterSlide(dto, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task CreateRegisterSlide_ValidRequest_SavesCorrectly()
    {
        // Arrange
        var formFile = Substitute.For<IFormFile>();
        formFile.FileName.Returns("pic.png");
        formFile.ContentType.Returns("image/png");

        var dto = new PostRegisterSlideDTO
        {
            Image = formFile,
            SortOrder = 5
        };

        var compressedStream = new MemoryStream(new byte[] { 1, 2, 3 });
        _fileCompressor.CompressFileAsync(formFile)
            .Returns(Task.FromResult((Stream: (Stream)compressedStream, ContentType: "image/webp")));

        _storageService.SaveFileAsync(Arg.Any<Stream>(), "image/webp", "register-slides")
            .Returns(Task.FromResult("saved.webp"));

        // Act
        var result = await _service.CreateRegisterSlide(dto, _userId, CancellationToken.None);

        // Assert
        _permissionService.Received(1).EnsureBoardOrCandidateBoardMember(_userId);
        Assert.Equal(5, result.SortOrder);
        Assert.Equal("saved.webp", result.ImagePath);

        _db.ChangeTracker.Clear();
        var saved = await _db.RegisterSlides.FindAsync(result.Id);
        Assert.NotNull(saved);
        Assert.Equal("saved.webp", saved.ImagePath);
        Assert.Equal("pic.png", saved.ImageFileName);
    }

    [Fact]
    public async Task CreateRegisterSlide_WithoutSortOrder_AutoIncrements()
    {
        // Arrange
        _db.RegisterSlides.Add(new RegisterSlide { Id = 1, SortOrder = 10, ImagePath = "p1", ImageFileName = "f1" });
        await _db.SaveChangesAsync();

        var formFile = Substitute.For<IFormFile>();
        formFile.FileName.Returns("pic.png");
        formFile.ContentType.Returns("image/png");

        var dto = new PostRegisterSlideDTO { Image = formFile };

        var compressedStream = new MemoryStream(new byte[] { 1, 2, 3 });
        _fileCompressor.CompressFileAsync(formFile)
            .Returns(Task.FromResult((Stream: (Stream)compressedStream, ContentType: "image/webp")));

        _storageService.SaveFileAsync(Arg.Any<Stream>(), "image/webp", "register-slides")
            .Returns(Task.FromResult("saved.webp"));

        // Act
        var result = await _service.CreateRegisterSlide(dto, _userId, CancellationToken.None);

        // Assert
        Assert.Equal(11, result.SortOrder);
    }

    [Fact]
    public async Task CreateRegisterSlide_CompressThrows_RollsBackTransaction()
    {
        // Arrange
        var formFile = Substitute.For<IFormFile>();
        formFile.FileName.Returns("pic.png");
        formFile.ContentType.Returns("image/png");

        var dto = new PostRegisterSlideDTO { Image = formFile };

        _fileCompressor.CompressFileAsync(formFile)
            .Throws(new InvalidOperationException("Failed compress"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateRegisterSlide(dto, _userId, CancellationToken.None));

        _db.ChangeTracker.Clear();
        var count = await _db.RegisterSlides.CountAsync();
        Assert.Equal(0, count); // Verification that nothing was saved
    }

    [Fact]
    public async Task UpdateRegisterSlide_NotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var dto = new RegisterSlideUpdateDTO { SortOrder = 5 };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.UpdateRegisterSlide(999, dto, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateRegisterSlide_UpdatesSortOrder()
    {
        // Arrange
        var slide = new RegisterSlide { Id = 1, SortOrder = 1, ImagePath = "p1", ImageFileName = "f1" };
        _db.RegisterSlides.Add(slide);
        await _db.SaveChangesAsync();

        var dto = new RegisterSlideUpdateDTO { SortOrder = 2 };

        // Act
        await _service.UpdateRegisterSlide(1, dto, _userId, CancellationToken.None);

        // Assert
        _db.ChangeTracker.Clear();
        var updated = await _db.RegisterSlides.FindAsync(1);
        Assert.NotNull(updated);
        Assert.Equal(2, updated.SortOrder);
    }

    [Fact]
    public async Task DeleteRegisterSlide_RemovesFromDbAndStorage()
    {
        // Arrange
        var slide = new RegisterSlide { Id = 1, SortOrder = 1, ImagePath = "img.webp", ImageFileName = "img.png" };
        _db.RegisterSlides.Add(slide);
        await _db.SaveChangesAsync();

        // Act
        await _service.DeleteRegisterSlide(1, _userId, CancellationToken.None);

        // Assert
        _db.ChangeTracker.Clear();
        var deleted = await _db.RegisterSlides.FindAsync(1);
        Assert.Null(deleted);
        await _storageService.Received(1).DeleteFileAsync("register-slides", "img.webp");
        _memoryCache.Received(1).Remove("slide-img-img.webp");
    }

    [Fact]
    public async Task UploadRegisterSlideImage_NullImage_ClearsImage()
    {
        // Arrange
        var slide = new RegisterSlide { Id = 1, SortOrder = 1, ImagePath = "old.webp", ImageFileName = "old.png" };
        _db.RegisterSlides.Add(slide);
        await _db.SaveChangesAsync();

        // Act
        var result = await _service.UploadRegisterSlideImage(1, _userId, null);

        // Assert
        Assert.Null(result);
        _db.ChangeTracker.Clear();
        var updated = await _db.RegisterSlides.FindAsync(1);
        Assert.NotNull(updated);
        Assert.Null(updated.ImagePath);
        Assert.Null(updated.ImageFileName);
        await _storageService.Received(1).DeleteFileAsync("register-slides", "old.webp");
        _memoryCache.Received(1).Remove("slide-img-old.webp");
    }

    [Fact]
    public async Task UploadRegisterSlideImage_ValidImage_SavesAndDeletesOld()
    {
        // Arrange
        var slide = new RegisterSlide { Id = 1, SortOrder = 1, ImagePath = "old.webp", ImageFileName = "old.png" };
        _db.RegisterSlides.Add(slide);
        await _db.SaveChangesAsync();

        var formFile = Substitute.For<IFormFile>();
        formFile.FileName.Returns("new.png");
        formFile.ContentType.Returns("image/png");

        var compressedStream = new MemoryStream(new byte[] { 1, 2, 3 });
        _fileCompressor.CompressFileAsync(formFile)
            .Returns(Task.FromResult((Stream: (Stream)compressedStream, ContentType: "image/webp")));

        _storageService.SaveFileAsync(Arg.Any<Stream>(), "image/webp", "register-slides")
            .Returns(Task.FromResult("new.webp"));

        // Act
        var result = await _service.UploadRegisterSlideImage(1, _userId, formFile);

        // Assert
        Assert.Equal("new.webp", result);
        _db.ChangeTracker.Clear();
        var updated = await _db.RegisterSlides.FindAsync(1);
        Assert.NotNull(updated);
        Assert.Equal("new.webp", updated.ImagePath);
        Assert.Equal("new.png", updated.ImageFileName);

        await _storageService.Received(1).DeleteFileAsync("register-slides", "old.webp");
        _memoryCache.Received(1).Remove("slide-img-old.webp");
    }

    [Fact]
    public async Task UploadRegisterSlideImage_CompressThrows_RollsBackTransaction()
    {
        // Arrange
        var slide = new RegisterSlide { Id = 1, SortOrder = 1, ImagePath = "old.webp", ImageFileName = "old.png" };
        _db.RegisterSlides.Add(slide);
        await _db.SaveChangesAsync();

        var formFile = Substitute.For<IFormFile>();
        formFile.FileName.Returns("new.png");
        formFile.ContentType.Returns("image/png");

        _fileCompressor.CompressFileAsync(formFile)
            .Throws(new InvalidOperationException("Failed compress"));

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.UploadRegisterSlideImage(1, _userId, formFile));

        _db.ChangeTracker.Clear();
        var updated = await _db.RegisterSlides.FindAsync(1);
        Assert.NotNull(updated);
        Assert.Equal("old.webp", updated.ImagePath); // Verification that rollback worked and changes weren't saved
    }

    [Fact]
    public async Task GetRegisterSlideImageFile_Cached_ReturnsCachedFile()
    {
        // Arrange
        var cachedBytes = new byte[] { 4, 5, 6 };
        object? cachedVal = (cachedBytes, "image/webp");
        _memoryCache.TryGetValue("slide-img-path.webp", out Arg.Any<object?>())
            .Returns(x =>
            {
                x[1] = cachedVal;
                return true;
            });

        // Act
        var result = await _service.GetRegisterSlideImageFile("path.webp");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("image/webp", result.ContentType);
        using var ms = new MemoryStream();
        await result.Stream.CopyToAsync(ms);
        Assert.Equal(cachedBytes, ms.ToArray());
    }

    [Fact]
    public async Task GetRegisterSlideImageFile_NotCached_LoadsFromStorageAndCaches()
    {
        // Arrange
        _memoryCache.TryGetValue("slide-img-path.webp", out Arg.Any<object?>()).Returns(false);

        var mockEntry = Substitute.For<ICacheEntry>();
        _memoryCache.CreateEntry(Arg.Any<object>()).Returns(mockEntry);

        var fileStream = new MemoryStream(new byte[] { 10, 11 });
        var storageFile = new StorageFile(fileStream, "image/png", "path.webp");
        _storageService.GetFileAsync("register-slides", "path.webp").Returns(Task.FromResult<StorageFile?>(storageFile));

        // Act
        var result = await _service.GetRegisterSlideImageFile("path.webp");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("image/png", result.ContentType);
        _memoryCache.Received(1).CreateEntry("slide-img-path.webp");
        mockEntry.Received(1).Value = Arg.Is<(byte[] bytes, string contentType)>(val => val.contentType == "image/png" && val.bytes.SequenceEqual(new byte[] { 10, 11 }));
    }

    [Fact]
    public async Task GetRegisterSlideImageFile_NotFound_ReturnsNull()
    {
        // Arrange
        _memoryCache.TryGetValue("slide-img-path.webp", out Arg.Any<object?>()).Returns(false);
        _storageService.GetFileAsync("register-slides", "path.webp").Returns(Task.FromResult<StorageFile?>(null));

        // Act
        var result = await _service.GetRegisterSlideImageFile("path.webp");

        // Assert
        Assert.Null(result);
    }
}
