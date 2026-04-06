using Backend.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

public class FileCompressor : IFileCompressor
{
    private const int _maxWidth = 1280;

    public async Task<(Stream Stream, string ContentType)> CompressFileAsync(IFormFile file)
    {
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
        
        return (outputStream, contentType);
    }
}