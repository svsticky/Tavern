using Backend.Interfaces;
using NetVips;

namespace Backend.Services.FileCompressServices;

/// <summary>
/// Implements image compression for uploaded files.
/// </summary>
public class FileCompressService : IFileCompressService
{
    private const int _maxWidth = 1280;
    private const int _webpQuality = 75;

    // libvips needs a bounding box; this is its maximum coordinate, so only the width constrains the result.
    private const int _unboundedHeight = 10_000_000;

    private const string _gifLoader = "VipsForeignLoadNsgif";
    private const string _webpLoader = "VipsForeignLoadWebp";

    // Only these formats are decoded; everything else libvips can read is rejected.
    private static readonly string[] _allowedLoaders =
    [
        "VipsForeignLoadJpeg",
        "VipsForeignLoadPng",
        _webpLoader,
        _gifLoader
    ];

    private readonly ILogger<FileCompressService> _logger;

    /// <summary>
    /// Initializes a new instance of the FileCompressService class with the specified logger. The constructor sets up the necessary dependency for logging operations within the service, allowing it to log important events and errors that occur during file compression. This setup is essential for monitoring the service's behavior and troubleshooting any issues that may arise during the compression process.
    /// </summary>
    /// <param name="logger">The logger used for logging operations within the service.</param>
    public FileCompressService(ILogger<FileCompressService> logger)
    {
        _logger = logger;
    }

    static FileCompressService()
    {
        NetVips.NetVips.BlockUntrusted = true;
    }

    /// <inheritdoc />
    public async Task<(Stream Stream, string ContentType)> CompressFileAsync(IFormFile file)
    {
        _logger.LogInformation("Compressing file {FileName} with content type {ContentType}.", file.FileName, file.ContentType);
        using var inputBuffer = new MemoryStream();
        using (var inputStream = file.OpenReadStream())
        {
            await inputStream.CopyToAsync(inputBuffer);
        }

        var (data, contentType) = Compress(inputBuffer.ToArray(), _maxWidth, keepGif: true);
        var outputStream = new MemoryStream(data);

        _logger.LogInformation("Compressed file {FileName}. Output content type: {OutputContentType}.", file.FileName, contentType);

        return (outputStream, contentType);
    }

    /// <summary>
    /// Shrinks an image to at most <paramref name="maxWidth"/> pixels wide, keeping the aspect ratio, and encodes it as WebP.
    /// Images that are already narrower keep their size.
    /// </summary>
    /// <param name="input">The encoded source image. Must be a JPEG, PNG, WebP or GIF.</param>
    /// <param name="maxWidth">The maximum width of the result in pixels.</param>
    /// <param name="keepGif">When true, GIF input is encoded as GIF instead of WebP.</param>
    /// <returns>The encoded image and its content type.</returns>
    /// <exception cref="ArgumentException">Thrown when the input is not a supported or valid image.</exception>
    internal static (byte[] Data, string ContentType) Compress(byte[] input, int maxWidth, bool keepGif)
    {
        var loader = Image.FindLoadBuffer(input);
        if (loader is null || !_allowedLoaders.Any(loader.StartsWith))
        {
            throw new ArgumentException("File is not a supported image. Supported formats are JPEG, PNG, WebP and GIF.");
        }

        var isGif = loader.StartsWith(_gifLoader);

        // Load every frame of formats that can be animated, so animations survive the resize.
        var loadOptions = isGif || loader.StartsWith(_webpLoader) ? "n=-1" : string.Empty;

        try
        {
            using var image = Image.ThumbnailBuffer(input, maxWidth, loadOptions, height: _unboundedHeight, size: Enums.Size.Down);

            return isGif && keepGif
                ? (image.GifsaveBuffer(), "image/gif")
                : (image.WebpsaveBuffer(q: _webpQuality), "image/webp");
        }
        catch (VipsException ex)
        {
            throw new ArgumentException("File is not a valid image.", ex);
        }
    }
}
