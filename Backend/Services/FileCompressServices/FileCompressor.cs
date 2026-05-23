using Backend.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace Backend.Services.FileCompressServices;

/// <summary>
/// Implements image compression for uploaded files.
/// </summary>
public class FileCompressService : IFileCompressService
{
    private const int _maxWidth = 1280;
    private readonly ILogger<FileCompressService> _logger;

    /// <summary>
    /// Initializes a new instance of the FileCompressService class with the specified logger. The constructor sets up the necessary dependency for logging operations within the service, allowing it to log important events and errors that occur during file compression. This setup is essential for monitoring the service's behavior and troubleshooting any issues that may arise during the compression process.
    /// </summary>
    /// <param name="logger">The logger used for logging operations within the service.</param>
    public FileCompressService(ILogger<FileCompressService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<(Stream Stream, string ContentType)> CompressFileAsync(IFormFile file)
    {
        _logger.LogInformation("Compressing file {FileName} with content type {ContentType}.", file.FileName, file.ContentType);
        var outputStream = new MemoryStream();
        var contentType = file.ContentType.ToLower();

        using (var inputStream = file.OpenReadStream())
        {
            using var image = await Image.LoadAsync(inputStream);

            var resizeOptions = new ResizeOptions
            {
                Size = new Size(_maxWidth, 0),
                Mode = ResizeMode.Max
            };

            if (contentType == "image/gif")
            {
                image.Mutate(x => x.Resize(resizeOptions));

                await image.SaveAsGifAsync(outputStream, new GifEncoder());
            }
            else
            {
                image.Mutate(x => x.Resize(resizeOptions));

                await image.SaveAsWebpAsync(outputStream, new WebpEncoder
                {
                    Quality = 75
                });
                
                contentType = "image/webp";
            }
        }

        outputStream.Position = 0;
        _logger.LogInformation("Compressed file {FileName}. Output content type: {OutputContentType}.", file.FileName, contentType);
        
        return (outputStream, contentType);
    }
}
