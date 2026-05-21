namespace Backend.Interfaces;

/// <summary>
/// Defines file-storage operations used by backend services.
/// </summary>
public interface IStorageService
{
    /// <summary>
    /// Saves an uploaded form file to a storage bucket.
    /// </summary>
    /// <param name="file">The file to store.</param>
    /// <param name="bucketname">The destination bucket name.</param>
    /// <returns>The generated storage key.</returns>
    Task<string> SaveFileAsync(IFormFile file, string bucketname);

    /// <summary>
    /// Saves a stream to a storage bucket.
    /// </summary>
    /// <param name="stream">The stream to store.</param>
    /// <param name="contentType">The stream content type.</param>
    /// <param name="bucketname">The destination bucket name.</param>
    /// <returns>The generated storage key.</returns>
    Task<string> SaveFileAsync(Stream stream, string contentType, string bucketname);

    /// <summary>
    /// Retrieves a file from a storage bucket.
    /// </summary>
    /// <param name="bucketname">The source bucket name.</param>
    /// <param name="fileKey">The storage key of the file.</param>
    /// <returns>The file stream and metadata when found; otherwise <c>null</c>.</returns>
    Task<StorageFile?> GetFileAsync(string bucketname, string fileKey);

    /// <summary>
    /// Deletes a file from a storage bucket.
    /// </summary>
    /// <param name="bucketname">The source bucket name.</param>
    /// <param name="fileKey">The storage key of the file to delete.</param>
    Task DeleteFileAsync(string bucketname, string? fileKey);
}

/// <summary>
/// Represents a file retrieved from storage, including its content stream, content type, and original file name. This record is used to encapsulate the necessary information about a stored file when it is retrieved, allowing services to access the file's data and metadata in a structured way. The Stream property provides access to the file's content, while the ContentType and FileName properties offer additional context about the file for processing or delivery purposes.
/// </summary>
/// <param name="Stream">The stream containing the file's content.</param>
/// <param name="ContentType">The content type of the file.</param>
/// <param name="FileName">The original name of the file.</param>
public record StorageFile(Stream Stream, string ContentType, string FileName);
