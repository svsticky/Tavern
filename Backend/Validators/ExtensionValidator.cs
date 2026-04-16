namespace Backend.Validators;

public static class ExtensionValidator
{
    private static readonly string[] _allowedPosterExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
    private static readonly string[] _allowedPosterMimeTypes = { "image/jpeg", "image/png", "image/gif" };

    private static readonly string[] _allowedProfilePictureExtensions = { ".jpg", ".jpeg", ".png" };
    private static readonly string[] _allowedProfilePictureMimeTypes = { "image/jpeg", "image/png" };

    public static void ValidatePosterExtension(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!_allowedPosterExtensions.Contains(extension) || !_allowedPosterMimeTypes.Contains(file.ContentType))
            throw new ArgumentException("Invalid poster extension.");
    }

    public static void ValidateProfilePictureExtension(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!_allowedProfilePictureExtensions.Contains(extension) || !_allowedProfilePictureMimeTypes.Contains(file.ContentType))
            throw new ArgumentException("Invalid profile picture extension.");
    }
}