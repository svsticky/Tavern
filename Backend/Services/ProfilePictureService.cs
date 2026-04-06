using Backend.Database;
using Backend.Interfaces;
using Backend.Models;
using Backend.Utils;

namespace Backend.Services
{
    public class ProfilePictureService(
        PostgresDbContext db,
        IStorageService storageService,
        IPermissionService permissionService,
        IFileCompressor fileCompressor
    ) : IProfilePictureService
    {
        public async Task<(Stream Stream, string ContentType)?> GetProfilePictureByPath(string path)
        {
            var decodedPath = Uri.UnescapeDataString(path);

            var file = await storageService.GetFileAsync("profile-pictures", decodedPath);
            if (file == null) return null;

            return (file.Stream, file.ContentType);
        }

        public async Task<string?> UploadProfilePicture(Guid memberId, Guid userId, IFormFile? image)
        {
            var member = await db.Members.FindAsync(memberId);
            if (member == null) throw new Exception("Member not found");

            // Authorization check
            if (memberId != userId &&
                !permissionService.IsInGroupInCurrentYear(userId, PredefinedGroups.Board))
            {
                throw new UnauthorizedAccessException("You can only update your own profile picture.");
            }

            // Validate file
            if (image != null && !ExtensionUtils.IsValidProfilePictureExtension(image))
            {
                throw new Exception("Invalid file type. Allowed types are: .jpg, .jpeg, .png, .gif");
            }

            string? oldPath = member.ProfilePicturePath;

            using var transaction = await db.Database.BeginTransactionAsync();

            try
            {
                if (image != null)
                {
                    var compressedImage = await fileCompressor.CompressFileAsync(image);

                    string path = await storageService.SaveFileAsync(
                        compressedImage.Stream,
                        compressedImage.ContentType,
                        "profile-pictures"
                    );

                    member.ProfilePicturePath = path;
                    member.ProfilePictureFileName = image.FileName;
                }
                else
                {
                    member.ProfilePicturePath = null;
                    member.ProfilePictureFileName = null;
                }

                await db.SaveChangesAsync();
                await transaction.CommitAsync();

                // Cleanup old file after successful commit
                if (!string.IsNullOrEmpty(oldPath))
                {
                    await storageService.DeleteFileAsync("profile-pictures", oldPath);
                }

                return member.ProfilePicturePath;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}