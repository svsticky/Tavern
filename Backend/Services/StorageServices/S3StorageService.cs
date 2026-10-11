using Amazon.S3;
using Amazon.S3.Model;
using Backend.Interfaces;
using Backend.Services.FileCompressServices;

namespace Backend.Services.StorageServices;

/// <summary>
/// Implements file storage operations using Amazon S3.
/// </summary>
public class S3StorageService(
    IAmazonS3 s3Client,
    ILogger<S3StorageService> logger) : IStorageService
{
    /// <inheritdoc />
    public async Task<string> SaveFileAsync(IFormFile file, string bucketname)
    {
        var fileKey = Guid.NewGuid().ToString();

        using var inputBuffer = new MemoryStream();
        using (var inputStream = file.OpenReadStream())
        {
            await inputStream.CopyToAsync(inputBuffer);
        }

        var (data, _) = FileCompressService.Compress(inputBuffer.ToArray(), 1200, keepGif: false);
        using var outputStream = new MemoryStream(data);

        var request = new PutObjectRequest
        {
            BucketName = bucketname,
            Key = fileKey,
            InputStream = outputStream,
            ContentType = file.ContentType
        };

        await s3Client.PutObjectAsync(request);
        logger.LogInformation("Saved image file {FileKey} to bucket {BucketName}.", fileKey, bucketname);
        return fileKey;
    }

    /// <inheritdoc />
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
        logger.LogInformation("Saved stream file {FileKey} to bucket {BucketName}.", fileKey, bucketname);
        return fileKey;
    }

    /// <inheritdoc />
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
            logger.LogWarning("File {FileKey} not found in bucket {BucketName}.", fileKey, bucketname);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving file {FileKey} from bucket {BucketName}.", fileKey, bucketname);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task DeleteFileAsync(string bucketname, string? fileKey)
    {
        if (string.IsNullOrEmpty(fileKey)) return;

        await s3Client.DeleteObjectAsync(bucketname, fileKey);
        logger.LogInformation("Deleted file {FileKey} from bucket {BucketName}.", fileKey, bucketname);
    }
}
