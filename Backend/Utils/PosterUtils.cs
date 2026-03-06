namespace Backend.Utils;

public static class PosterUtils
{
    private static readonly HashSet<string> _allowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".gif",
        ".pdf"
    };

    public static async Task<string?> SavePosterAsync(IFormFile? poster)
    {
        if (poster == null)
        {
            return null;
        }

        if (!IsValidPoster(poster))
        {
            throw new InvalidOperationException("Invalid file type. Only JPG, JPEG, PNG, and GIF are allowed.");
        }

        string path = new Guid().ToString() + Path.GetExtension(poster.FileName);
        string fullPath = Path.Combine("Posters", path);
        await FileUtils.SaveFileAsync(poster, fullPath);
        return path;
    }

    /// <summary>
    /// Validates if the provided file is an allowed image type.
    /// </summary>
    /// <param name="file">The file to validate.</param>
    /// <returns>True if the file is valid; otherwise, false.</returns>
    private static bool IsValidPoster(IFormFile file)
    {
        string extension = Path.GetExtension(file.FileName);
        return _allowedExtensions.Contains(extension);
    }
}