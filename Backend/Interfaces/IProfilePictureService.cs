namespace Backend.Interfaces
{
    /// <summary>
    /// Defines operations for retrieving and uploading member profile pictures.
    /// </summary>
    public interface IProfilePictureService
    {
        /// <summary>
        /// Retrieves a profile picture by storage path.
        /// </summary>
        /// <param name="path">The storage path of the profile picture.</param>
        /// <returns>The picture stream and content type when found; otherwise <c>null</c>.</returns>
        Task<(Stream Stream, string ContentType)?> GetProfilePictureByPath(string path);

        /// <summary>
        /// Uploads and assigns a profile picture for a member.
        /// </summary>
        /// <param name="fromMemberId">The ID of the member performing the upload.</param>
        /// <param name="userId">The ID of the member whose picture is updated.</param>
        /// <param name="image">The image file to upload.</param>
        /// <returns>The stored file path when uploaded; otherwise <c>null</c>.</returns>
        Task<string?> UploadProfilePicture(Guid fromMemberId, Guid userId, IFormFile? image);
    }
}
