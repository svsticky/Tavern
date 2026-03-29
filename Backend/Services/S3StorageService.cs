using Amazon.S3;
using Amazon.S3.Model;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Webp;
using Backend.Interfaces;

public class S3StorageService(IAmazonS3 s3Client) : IStorageService
{
    public async Task<string> SaveFileAsync(IFormFile file, string bucketname)
    {
        var fileKey = Guid.NewGuid().ToString();
        
        using var outputStream = new MemoryStream();
        using (var inputStream = file.OpenReadStream())
        {
            using var image = await Image.LoadAsync(inputStream);

            image.Mutate(x => x.Resize(new ResizeOptions
            {
                Size = new Size(1200, 0),
                Mode = ResizeMode.Max
            }));

            await image.SaveAsWebpAsync(outputStream, new WebpEncoder
            {
                Quality = 75
            });
        }

        outputStream.Position = 0;

        var request = new PutObjectRequest
        {
            BucketName = bucketname,
            Key = fileKey,
            InputStream = outputStream,
            ContentType = file.ContentType
        };

        await s3Client.PutObjectAsync(request);
        return fileKey;
    }

    public async Task<string> SaveFileAsync(Stream stream, string contentType, string bucketname)
    {
        var fileKey = Guid.NewGuid().ToString();

        var request = new PutObjectRequest
        {
            BucketName = bucketname,
            Key = fileKey,
            InputStream = stream,
            ContentType = contentType
        };

        await s3Client.PutObjectAsync(request);
        return fileKey;
    }

    public async Task<StorageFile?> GetFileAsync(string bucketname, string fileKey)
    {
        try
        {
            var request = new GetObjectRequest
            {
                BucketName = bucketname,
                Key = fileKey
            };
            var response = await s3Client.GetObjectAsync(request);
        
            return new StorageFile(
                response.ResponseStream, 
                response.Headers.ContentType, 
                Path.GetFileName(fileKey)
            );
        }
        catch (AmazonS3Exception e) when (e.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task DeleteFileAsync(string bucketname, string? fileKey)
    {
        if (string.IsNullOrEmpty(fileKey)) return;
        
        await s3Client.DeleteObjectAsync(bucketname, fileKey);
    }
}