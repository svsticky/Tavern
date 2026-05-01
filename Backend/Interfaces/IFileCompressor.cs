namespace Backend.Interfaces;

/// <summary>
/// Defines operations for compressing uploaded files for download or export.
/// </summary>
public interface IFileCompressService
{
    /// <summary>
    /// Compresses an uploaded file and returns the compressed stream and content type.
    /// </summary>
    /// <param name="file">The uploaded file to compress.</param>
    /// <returns>The compressed file stream and content type.</returns>
    Task<(Stream Stream, string ContentType)> CompressFileAsync(IFormFile file);
}
