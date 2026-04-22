namespace Backend.Interfaces
{
    public interface IProfilePictureService
    {
        Task<(Stream Stream, string ContentType)?> GetProfilePictureByPath(string path);

        Task<string?> UploadProfilePicture(Guid fromMemberId, Guid userId, IFormFile? image);
    }
}