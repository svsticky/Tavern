namespace Backend.Utils;

public static class ExtensionUtils
{
    private static readonly string[] _allowedPosterExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".pdf" };
    private static readonly string[] _allowedPosterMimeTypes = { "image/jpeg", "image/png", "image/gif", "application/pdf" };

    private static readonly string[] _allowedProfilePictureExtensions = { ".jpg", ".jpeg", ".png" };
    private static readonly string[] _allowedProfilePictureMimeTypes = { "image/jpeg", "image/png" };

    public static bool IsValidPosterExtension(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        return _allowedPosterExtensions.Contains(extension) && _allowedPosterMimeTypes.Contains(file.ContentType);
    }

    public static bool IsValidProfilePictureExtension(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        return _allowedProfilePictureExtensions.Contains(extension) && _allowedProfilePictureMimeTypes.Contains(file.ContentType);
    }
}