namespace Backend.Interfaces
{
    public interface IProfilePictureService
    {
        Task<(Stream Stream, string ContentType)?> GetProfilePictureByPath(string path, CancellationToken cancellationToken);

        Task<string?> UploadProfilePicture(Guid memberId, Guid userId, IFormFile? image);
    }
}