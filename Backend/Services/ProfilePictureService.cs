using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Backend.Validators;
using Microsoft.Extensions.Logging;

namespace Backend.Services
{
    public class ProfilePictureService(
        PostgresDbContext db,
        IStorageService storageService,
        IPermissionService permissionService,
        IFileCompressService fileCompressor,
        ILogger<ProfilePictureService> logger
    ) : IProfilePictureService
    {
        public async Task<(Stream Stream, string ContentType)?> GetProfilePictureByPath(string path)
        {
            var decodedPath = Uri.UnescapeDataString(path);

            var file = await storageService.GetFileAsync("profile-pictures", decodedPath);
            if (file == null) return null;

            return (file.Stream, file.ContentType);
        }

        public async Task<string?> UploadProfilePicture(Guid fromUserId, Guid userId, IFormFile? image)
        {
            logger.LogInformation("Updating profile picture for member {MemberId}. Requested by {UserId}.", fromUserId, userId);
            var member = await GetMemberOrThrow(fromUserId);
            if(fromUserId != userId)
                permissionService.EnsureBoardOrCandidateBoardMember(userId);

            ValidateImage(image);

            string? oldPath = member.ProfilePicturePath;

            using var transaction = await db.Database.BeginTransactionAsync();

            try
            {
                await ApplyProfilePicture(member, image);

                await db.SaveChangesAsync();
                await transaction.CommitAsync();

                await DeleteOldProfilePicture(oldPath);
                logger.LogInformation("Updated profile picture for member {MemberId}.", fromUserId);

                return member.ProfilePicturePath;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                logger.LogError(ex, "Failed updating profile picture for member {MemberId}.", fromUserId);
                throw;
            }
        }

        private async Task<Member> GetMemberOrThrow(Guid memberId)
        {
            var member = await db.Members.FindAsync(memberId);
            return member ?? throw new Exception("Member not found");
        }

        private static void ValidateImage(IFormFile? image)
        {
            if (image != null)
            {
                ExtensionValidator.ValidateProfilePictureExtension(image);
            }
        }

        private async Task ApplyProfilePicture(Member member, IFormFile? image)
        {
            if (image == null)
            {
                member.ProfilePicturePath = null;
                member.ProfilePictureFileName = null;
                return;
            }

            var compressedImage = await fileCompressor.CompressFileAsync(image);
            string path = await storageService.SaveFileAsync(
                compressedImage.Stream,
                compressedImage.ContentType,
                "profile-pictures"
            );

            member.ProfilePicturePath = path;
            member.ProfilePictureFileName = image.FileName;
        }

        private async Task DeleteOldProfilePicture(string? oldPath)
        {
            if (!string.IsNullOrEmpty(oldPath))
            {
                await storageService.DeleteFileAsync("profile-pictures", oldPath);
            }
        }
    }
}
