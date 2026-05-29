using Backend.Services.FileCompressServices;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Backend.Tests.Services;

public class FileCompressServiceTests
{
    private readonly ILogger<FileCompressService> _loggerMock;
    private readonly FileCompressService _service;

    public FileCompressServiceTests()
    {
        _loggerMock = Substitute.For<ILogger<FileCompressService>>();
        _service = new FileCompressService(_loggerMock);
    }

    private IFormFile CreateDummyImageFile(string fileName, string contentType)
    {
        var fileMock = Substitute.For<IFormFile>();
        fileMock.FileName.Returns(fileName);
        fileMock.ContentType.Returns(contentType);

        var ms = new MemoryStream();
        using (var image = new Image<Rgba32>(1, 1))
        {
            if (contentType.Equals("image/gif", StringComparison.OrdinalIgnoreCase))
            {
                image.SaveAsGif(ms);
            }
            else
            {
                image.SaveAsPng(ms);
            }
        }
        ms.Position = 0;
        fileMock.OpenReadStream().Returns(ms);
        fileMock.Length.Returns(ms.Length);

        return fileMock;
    }

    [Fact]
    public async Task CompressFileAsync_WithGif_ReturnsGifStream()
    {
        var file = CreateDummyImageFile("test.gif", "image/gif");

        var (stream, contentType) = await _service.CompressFileAsync(file);

        Assert.NotNull(stream);
        Assert.Equal("image/gif", contentType);
        Assert.True(stream.Length > 0);
    }

    [Fact]
    public async Task CompressFileAsync_WithPng_ConvertsToWebp()
    {
        var file = CreateDummyImageFile("test.png", "image/png");

        var (stream, contentType) = await _service.CompressFileAsync(file);

        Assert.NotNull(stream);
        Assert.Equal("image/webp", contentType);
        Assert.True(stream.Length > 0);
    }
}
