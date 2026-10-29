using Backend.Services.FileCompressServices;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NetVips;
using NSubstitute;

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

        using var image = Image.Black(1, 1);
        var ms = new MemoryStream(contentType.Equals("image/gif", StringComparison.OrdinalIgnoreCase)
            ? image.GifsaveBuffer()
            : image.PngsaveBuffer());
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

    private static IFormFile CreateFile(byte[] content, string contentType)
    {
        var fileMock = Substitute.For<IFormFile>();
        fileMock.FileName.Returns("test");
        fileMock.ContentType.Returns(contentType);
        fileMock.OpenReadStream().Returns(new MemoryStream(content));
        fileMock.Length.Returns(content.Length);

        return fileMock;
    }

    [Fact]
    public async Task CompressFileAsync_WithWideImage_ShrinksToMaxWidth()
    {
        using var source = Image.Black(2560, 1000);
        var file = CreateFile(source.PngsaveBuffer(), "image/png");

        var (stream, _) = await _service.CompressFileAsync(file);

        using var result = Image.NewFromStream(stream);
        Assert.Equal(1280, result.Width);
        Assert.Equal(500, result.Height);
    }

    [Fact]
    public async Task CompressFileAsync_WithTallImage_OnlyConstrainsWidth()
    {
        using var source = Image.Black(100, 3000);
        var file = CreateFile(source.PngsaveBuffer(), "image/png");

        var (stream, _) = await _service.CompressFileAsync(file);

        using var result = Image.NewFromStream(stream);
        Assert.Equal(100, result.Width);
        Assert.Equal(3000, result.Height);
    }

    [Fact]
    public async Task CompressFileAsync_WithAnimatedGif_KeepsAllFrames()
    {
        // Three distinct 2000x10 frames stacked vertically, saved as an animation.
        var shades = new[] { 0, 100, 200 }.Select(shade => (Image.Black(2000, 10) + shade).Cast(Enums.BandFormat.Uchar)).ToArray();
        using var frames = Image.Arrayjoin(shades, across: 1).Mutate(m => m.Set(GValue.GIntType, "page-height", 10));
        var file = CreateFile(frames.GifsaveBuffer(), "image/gif");

        var (stream, contentType) = await _service.CompressFileAsync(file);

        Assert.Equal("image/gif", contentType);
        using var result = Image.NewFromStream(stream, "n=-1");
        Assert.Equal(1280, result.Width);
        Assert.Equal(3, (int)result.Get("n-pages"));
    }

    [Fact]
    public async Task CompressFileAsync_WithUltraHdrJpeg_ConvertsToWebp()
    {
        // Ultra HDR is a JPEG with an HDR gain map (the default on recent iPhones); libvips reads it with a separate loader.
        using var hdr = (Image.Black(2560, 1000, 3) + 0.5).Cast(Enums.BandFormat.Float).Copy(interpretation: Enums.Interpretation.Scrgb);
        var bytes = hdr.UhdrsaveBuffer();
        Assert.StartsWith("VipsForeignLoadUhdr", Image.FindLoadBuffer(bytes));
        var file = CreateFile(bytes, "image/jpeg");

        var (stream, contentType) = await _service.CompressFileAsync(file);

        Assert.Equal("image/webp", contentType);
        using var result = Image.NewFromStream(stream);
        Assert.Equal(1280, result.Width);
        Assert.Equal(500, result.Height);
    }

    [Fact]
    public async Task CompressFileAsync_WithNonImage_ThrowsArgumentException()
    {
        var file = CreateFile([1, 2, 3, 4, 5, 6, 7, 8], "image/png");

        await Assert.ThrowsAsync<ArgumentException>(() => _service.CompressFileAsync(file));
    }

    [Fact]
    public async Task CompressFileAsync_WithUnsupportedImageFormat_ThrowsArgumentException()
    {
        using var source = Image.Black(1, 1);
        var file = CreateFile(source.TiffsaveBuffer(), "image/tiff");

        await Assert.ThrowsAsync<ArgumentException>(() => _service.CompressFileAsync(file));
    }

    [Fact]
    public async Task CompressFileAsync_WithCorruptImage_ThrowsArgumentException()
    {
        using var source = Image.Black(64, 64);
        var png = source.PngsaveBuffer();
        var file = CreateFile(png[..(png.Length / 2)], "image/png");

        await Assert.ThrowsAsync<ArgumentException>(() => _service.CompressFileAsync(file));
    }
}
