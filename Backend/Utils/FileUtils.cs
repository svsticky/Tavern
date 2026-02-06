namespace Backend.Utils;

public static class FileUtils
{
    /// <summary>
    /// Saves a file to the specified path, creating directories if necessary.
    /// </summary>
    /// <param name="file">The file to be saved.</param>
    /// <param name="path">The path where the file should be saved.</param>
    public static async Task SaveFileAsync(IFormFile file, string path)
    {
        string directory = Path.GetDirectoryName(path) ?? throw new InvalidOperationException("Invalid path provided.");

        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using FileStream stream = new(path, FileMode.Create);
        await file.CopyToAsync(stream);
    }
}