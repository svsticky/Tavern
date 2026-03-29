namespace Backend.Interfaces;

public interface IFileCompressor
{
    Task<(Stream Stream, string ContentType)> CompressFileAsync(IFormFile file);
}