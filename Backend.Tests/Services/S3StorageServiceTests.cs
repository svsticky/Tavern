using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Backend.Services.StorageServices;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;
using SixLabors.ImageSharp;

namespace Backend.Tests.Services;

public class S3StorageServiceTests
{
    private readonly IAmazonS3 _s3Client;
    private readonly S3StorageService _service;

    // Valid 1x1 PNG byte array
    private static readonly byte[] SmallPng;

    static S3StorageServiceTests()
    {
        using var image = new Image<SixLabors.ImageSharp.PixelFormats.Rgba32>(1, 1);
        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        SmallPng = ms.ToArray();
    }

    public S3StorageServiceTests()
    {
        _s3Client = Substitute.For<IAmazonS3>();
        _service = new S3StorageService(_s3Client, NullLogger<S3StorageService>.Instance);
    }

    [Fact]
    public async Task SaveFileAsync_WithFormFile_ProcessesAndSavesAsWebp()
    {
        // Arrange
        var fileMock = Substitute.For<IFormFile>();
        var stream = new MemoryStream(SmallPng);
        fileMock.OpenReadStream().Returns(stream);
        fileMock.ContentType.Returns("image/png");

        _s3Client.PutObjectAsync(Arg.Any<PutObjectRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new PutObjectResponse()));

        // Act
        var result = await _service.SaveFileAsync(fileMock, "my-bucket");

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(result));
        await _s3Client.Received(1).PutObjectAsync(
            Arg.Is<PutObjectRequest>(r => r.BucketName == "my-bucket" && r.Key == result && r.ContentType == "image/png"),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task SaveFileAsync_WithStream_SavesDirectly()
    {
        // Arrange
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        _s3Client.PutObjectAsync(Arg.Any<PutObjectRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new PutObjectResponse()));

        // Act
        var result = await _service.SaveFileAsync(stream, "text/plain", "my-bucket");

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(result));
        await _s3Client.Received(1).PutObjectAsync(
            Arg.Is<PutObjectRequest>(r => r.BucketName == "my-bucket" && r.Key == result && r.ContentType == "text/plain"),
            Arg.Any<CancellationToken>()
        );
    }

    [Fact]
    public async Task GetFileAsync_FileExists_ReturnsFile()
    {
        // Arrange
        var expectedStream = new MemoryStream(new byte[] { 4, 5, 6 });
        var getObjectResponse = new GetObjectResponse
        {
            ResponseStream = expectedStream
        };
        getObjectResponse.Headers.ContentType = "application/pdf";

        _s3Client.GetObjectAsync(Arg.Any<GetObjectRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(getObjectResponse));

        // Act
        var file = await _service.GetFileAsync("my-bucket", "some-key");

        // Assert
        Assert.NotNull(file);
        Assert.Equal("application/pdf", file.ContentType);
        Assert.Equal("some-key", file.FileName);
        Assert.Equal(expectedStream, file.Stream);
    }

    [Fact]
    public async Task GetFileAsync_S3NotFoundException_ReturnsNull()
    {
        // Arrange
        var ex = new AmazonS3Exception("Not Found", Amazon.Runtime.ErrorType.Receiver, "404", "request-id", HttpStatusCode.NotFound);
        _s3Client.GetObjectAsync(Arg.Any<GetObjectRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(ex);

        // Act
        var file = await _service.GetFileAsync("my-bucket", "missing-key");

        // Assert
        Assert.Null(file);
    }

    [Fact]
    public async Task GetFileAsync_S3OtherException_Rethrows()
    {
        // Arrange
        var ex = new AmazonS3Exception("Internal Error", Amazon.Runtime.ErrorType.Receiver, "500", "request-id", HttpStatusCode.InternalServerError);
        _s3Client.GetObjectAsync(Arg.Any<GetObjectRequest>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(ex);

        // Act & Assert
        await Assert.ThrowsAsync<AmazonS3Exception>(() => _service.GetFileAsync("my-bucket", "error-key"));
    }

    [Fact]
    public async Task DeleteFileAsync_NullOrEmptyKey_DoesNothing()
    {
        // Act
        await _service.DeleteFileAsync("my-bucket", null);
        await _service.DeleteFileAsync("my-bucket", "");

        // Assert
        await _s3Client.DidNotReceiveWithAnyArgs().DeleteObjectAsync(default!, default!, default);
    }

    [Fact]
    public async Task DeleteFileAsync_WithKey_CallsDeleteObjectAsync()
    {
        // Act
        await _service.DeleteFileAsync("my-bucket", "my-key");

        // Assert
        await _s3Client.Received(1).DeleteObjectAsync("my-bucket", "my-key", Arg.Any<CancellationToken>());
    }
}
