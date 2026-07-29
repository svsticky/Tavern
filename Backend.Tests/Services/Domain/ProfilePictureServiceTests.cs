using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Backend.Services.Domain;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Backend.Tests.Services.Domain;

public class ProfilePictureServiceTests : IDisposable
{
    private readonly DbContextOptions<PostgresDbContext> _dbOptions;
    private readonly PostgresDbContext _db;
    private readonly IStorageService _storageService;
    private readonly IPermissionService _permissionService;
    private readonly IFileCompressService _fileCompressor;
    private readonly IMemoryCache _memoryCache;
    private readonly ProfilePictureService _service;
    private readonly Guid _userId = Guid.NewGuid();

    public ProfilePictureServiceTests()
    {
        _dbOptions = new DbContextOptionsBuilder<PostgresDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new PostgresDbContext(_dbOptions);
        _db.Database.EnsureCreated();

        _storageService = Substitute.For<IStorageService>();
        _permissionService = Substitute.For<IPermissionService>();
        _fileCompressor = Substitute.For<IFileCompressService>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());

        _service = new ProfilePictureService(
            _db,
            _storageService,
            _permissionService,
            _fileCompressor,
            _memoryCache,
            NullLogger<ProfilePictureService>.Instance
        );
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
        _memoryCache.Dispose();
    }

    private Member CreateTestMember(Guid id)
    {
        return new Member
        {
            Id = id,
            StudentNumber = "s1234567",
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            PhoneNumber = "0612345678",
            Street = "Main St",
            HouseNumber = "1",
            PostalCode = "1234AB",
            City = "Enschede"
        };
    }

    [Fact]
    public async Task GetProfilePictureByPath_CacheHit_ReturnsFromCache()
    {
        // Arrange
        var path = "cached-pic.jpg";
        var cacheKey = $"prof-pic-{path}";
        var expectedBytes = new byte[] { 1, 2, 3 };
        var contentType = "image/jpeg";
        _memoryCache.Set(cacheKey, (expectedBytes, contentType), TimeSpan.FromHours(1));

        // Act
        var result = await _service.GetProfilePictureByPath(path);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(contentType, result.Value.ContentType);
        using var ms = new MemoryStream();
        await result.Value.Stream.CopyToAsync(ms);
        Assert.Equal(expectedBytes, ms.ToArray());

        await _storageService.DidNotReceiveWithAnyArgs().GetFileAsync(default!, default!);
    }

    [Fact]
    public async Task GetProfilePictureByPath_CacheMiss_FileExists_RetrievesAndCaches()
    {
        // Arrange
        var path = "s3-pic.jpg";
        var expectedBytes = new byte[] { 4, 5, 6 };
        var contentType = "image/jpeg";
        var fileStream = new MemoryStream(expectedBytes);
        var storageFile = new StorageFile(fileStream, contentType, path);

        _storageService.GetFileAsync("profile-pictures", path)
            .Returns(Task.FromResult<StorageFile?>(storageFile));

        // Act
        var result = await _service.GetProfilePictureByPath(path);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(contentType, result.Value.ContentType);
        using var ms = new MemoryStream();
        await result.Value.Stream.CopyToAsync(ms);
        Assert.Equal(expectedBytes, ms.ToArray());

        // Check it was added to cache
        var cacheKey = $"prof-pic-{path}";
        Assert.True(_memoryCache.TryGetValue(cacheKey, out (byte[] bytes, string contentType) cached));
        Assert.Equal(expectedBytes, cached.bytes);
        Assert.Equal(contentType, cached.contentType);
    }

    [Fact]
    public async Task GetProfilePictureByPath_FileNotFound_ReturnsNull()
    {
        // Arrange
        var path = "missing.jpg";
        _storageService.GetFileAsync("profile-pictures", path)
            .Returns(Task.FromResult<StorageFile?>(null));

        // Act
        var result = await _service.GetProfilePictureByPath(path);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UploadProfilePicture_MemberNotFound_ThrowsException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() =>
            _service.UploadProfilePicture(Guid.NewGuid(), _userId, null));
    }

    [Fact]
    public async Task UploadProfilePicture_DifferentUserNotBoard_ThrowsUnauthorized()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var member = CreateTestMember(memberId);
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        _permissionService.When(p => p.EnsureBoardOrCandidateBoardMember(_userId))
            .Do(x => throw new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.UploadProfilePicture(memberId, _userId, null));
    }

    [Fact]
    public async Task UploadProfilePicture_NullImage_ClearsProfilePicture()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var member = CreateTestMember(memberId);
        member.ProfilePicturePath = "old-path.jpg";
        member.ProfilePictureFileName = "old-name.jpg";
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        // Act
        var result = await _service.UploadProfilePicture(memberId, memberId, null);

        // Assert
        Assert.Null(result);
        var updated = await _db.Members.FindAsync(memberId);
        Assert.NotNull(updated);
        Assert.Null(updated.ProfilePicturePath);
        Assert.Null(updated.ProfilePictureFileName);

        await _storageService.Received(1).DeleteFileAsync("profile-pictures", "old-path.jpg");
    }

    [Fact]
    public async Task UploadProfilePicture_ValidImage_CompressesAndSaves()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var member = CreateTestMember(memberId);
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        var formFile = Substitute.For<IFormFile>();
        formFile.FileName.Returns("my-pic.png");
        formFile.ContentType.Returns("image/png");

        var compressedStream = new MemoryStream(new byte[] { 7, 8, 9 });
        _fileCompressor.CompressFileAsync(formFile)
            .Returns(Task.FromResult((Stream: (Stream)compressedStream, ContentType: "image/webp")));

        _storageService.SaveFileAsync(Arg.Any<Stream>(), "image/webp", "profile-pictures")
            .Returns(Task.FromResult("saved-path.webp"));

        // Act
        var result = await _service.UploadProfilePicture(memberId, memberId, formFile);

        // Assert
        Assert.Equal("saved-path.webp", result);
        var updated = await _db.Members.FindAsync(memberId);
        Assert.NotNull(updated);
        Assert.Equal("saved-path.webp", updated.ProfilePicturePath);
        Assert.Equal("my-pic.png", updated.ProfilePictureFileName);
    }
}
