namespace Backend.Interfaces;

public interface IFileCompressService
{
    Task<(Stream Stream, string ContentType)> CompressFileAsync(IFormFile file);
}