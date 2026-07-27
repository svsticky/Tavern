namespace Backend.Validators;

/// <summary>
/// Provides file-extension and MIME-type validation for uploaded images.
/// </summary>
public static class ExtensionValidator
{
    private static readonly string[] _allowedPosterExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
    private static readonly string[] _allowedPosterMimeTypes = { "image/jpeg", "image/png", "image/gif" };

    private static readonly string[] _allowedProfilePictureExtensions = { ".jpg", ".jpeg", ".png" };
    private static readonly string[] _allowedProfilePictureMimeTypes = { "image/jpeg", "image/png" };

    /// <summary>
    /// Validates that an uploaded poster file has an allowed extension and MIME type.
    /// </summary>
    /// <param name="file">The uploaded poster file.</param>
    /// <exception cref="ArgumentException">Thrown when the file type is not allowed.</exception>
    public static void ValidatePosterExtension(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!_allowedPosterExtensions.Contains(extension) || !_allowedPosterMimeTypes.Contains(file.ContentType))
            throw new ArgumentException("Invalid file extension.");
    }

    /// <summary>
    /// Validates that an uploaded profile picture has an allowed extension and MIME type.
    /// </summary>
    /// <param name="file">The uploaded profile picture file.</param>
    /// <exception cref="ArgumentException">Thrown when the file type is not allowed.</exception>
    public static void ValidateProfilePictureExtension(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!_allowedProfilePictureExtensions.Contains(extension) || !_allowedProfilePictureMimeTypes.Contains(file.ContentType))
            throw new ArgumentException("Invalid profile picture extension.");
    }
}
