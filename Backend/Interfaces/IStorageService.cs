namespace Backend.Interfaces;

public interface IStorageService
{
    Task<string> SaveFileAsync(IFormFile file, string bucketname);
    Task<string> SaveFileAsync(Stream stream, string contentType, string bucketname);
    Task<StorageFile?> GetFileAsync(string bucketname, string fileKey);
    Task DeleteFileAsync(string bucketname, string? fileKey);
}

public record StorageFile(Stream Stream, string ContentType, string FileName);