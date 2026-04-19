using Backend.Controllers.DTOs;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Backend.Validators;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class GroupService : IGroupService
{
    private readonly PostgresDbContext _db;
    private readonly IPermissionService _permissionService;
    private readonly IFileCompressService _fileCompressService;
    private readonly IStorageService _storageService;

    public GroupService(PostgresDbContext db, IPermissionService permissionService, IFileCompressService fileCompressService, IStorageService storageService)
    {
        _db = db;
        _permissionService = permissionService;
        _fileCompressService = fileCompressService;
        _storageService = storageService;
    }

    public async Task<IEnumerable<GroupResponseDTO>> GetGroups(Guid userId, GetGroupDTO dto, CancellationToken cancellationToken)
    {
        if ((dto.MembershipYear == null || dto.IncludeInactive) &&
            !_permissionService.IsBoardOrCandidateBoardMember(userId))
        {
            throw new UnauthorizedAccessException();
        }

        return await _db.Groups
            .Select(c => new GroupResponseDTO
            {
                Id = c.Id,
                Name = c.Name,
                Type = c.Type,
                Active = c.Active,
                GroupPicturePath = c.GroupPicturePath
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<GroupResponseDTO?> GetGroup(uint id, CancellationToken cancellationToken)
    {
        return await _db.Groups
            .Where(g => g.Id == id)
            .Include(g => g.GroupMemberships)
            .ThenInclude(gm => gm.Member)
            .Select(g => new GroupResponseDTO
            {
                Id = g.Id,
                Name = g.Name,
                Active = g.Active,
                Type = g.Type,
                GroupPicturePath = g.GroupPicturePath
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Group> CreateGroup(PostGroupDTO dto, Guid userId, CancellationToken cancellationToken)
    {
        if (!_permissionService.IsBoardOrCandidateBoardMember(userId))
            throw new UnauthorizedAccessException("Only board members can create groups.");

        if (dto.Name.Contains(';') || dto.Name.Contains(':'))
            throw new ArgumentException("Group names cannot contain ';' or ':'.");

        using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var group = new Group
            {
                Name = dto.Name,
                Type = dto.Type
            };

            if (dto.GroupPicture != null)
            {
                ExtensionValidator.ValidateProfilePictureExtension(dto.GroupPicture);

                var compressed = await _fileCompressService.CompressFileAsync(dto.GroupPicture);
                group.GroupPicturePath = await _storageService.SaveFileAsync(
                    compressed.Stream, 
                    compressed.ContentType, 
                    "group-pictures"
                );
                group.GroupPictureFileName = dto.GroupPicture.FileName;
            }

            StateValidator.Validate(group);

            _db.Groups.Add(group);
            await _db.SaveChangesAsync(cancellationToken);
            
            await transaction.CommitAsync(cancellationToken);

            return group;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<FileResultDto?> GetGroupPictureFile(string path)
    {
        var file = await _storageService.GetFileAsync("group-pictures", path);

        if (file == null) return null;

        return new FileResultDto
        {
            Stream = file.Stream,
            ContentType = file.ContentType
        };
    }

    public async Task DeleteGroup(uint id, Guid userId, CancellationToken cancellationToken)
    {
        if (!_permissionService.IsBoardOrCandidateBoardMember(userId))
            throw new UnauthorizedAccessException("Only board members can delete groups.");

        var group = await _db.Groups.FindAsync(id, cancellationToken);
        if (group == null)
            throw new KeyNotFoundException();

        if (group.GroupPicturePath != null)
            await _storageService.DeleteFileAsync("group-pictures", group.GroupPicturePath);

        _db.Groups.Remove(group);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task PatchGroup(uint id, Guid userId, JsonPatchDocument<Group> patchDoc, CancellationToken cancellationToken)
    {
        if (!_permissionService.IsBoardOrCandidateBoardMember(userId))
            throw new UnauthorizedAccessException("Only board members can update groups.");

        if (patchDoc == null)
            throw new ArgumentException("Patch document is null");

        var group = await _db.Groups.FindAsync(new object[] { id }, cancellationToken);
        if (group == null)
            throw new KeyNotFoundException();

        patchDoc.ApplyTo(group);

        StateValidator.Validate(group);

        if (group.Name.Contains(';') || group.Name.Contains(':'))
            throw new ArgumentException("Group names cannot contain ';' or ':'.");

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateGroup(uint id, Guid userId, GroupUpdateDTO dto, CancellationToken cancellationToken)
    {
        if (!_permissionService.IsBoardOrCandidateBoardMember(userId))
            throw new UnauthorizedAccessException("Only board members can update groups.");

        if (dto.Name.Contains(';') || dto.Name.Contains(':'))
            throw new ArgumentException("Group names cannot contain ';' or ':'.");

        var group = await _db.Groups.FindAsync(id, cancellationToken);
        if (group == null)
            throw new KeyNotFoundException();

        group.Name = dto.Name;
        group.Active = dto.Active;
        group.Type = dto.Type;

        StateValidator.Validate(group);

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<string?> UploadGroupPicture(uint groupId, Guid userId, IFormFile? image)
    {
        var group = await _db.Groups.FindAsync(groupId);
        if (group == null) throw new Exception("Group not found");

        // Authorization check
        if (!_permissionService.IsBoardOrCandidateBoardMember(userId))
        {
            throw new UnauthorizedAccessException("You can only update your own profile picture.");
        }

        // Validate file
        if (image != null)
        {
            ExtensionValidator.ValidateProfilePictureExtension(image);
        }

        string? oldPath = group.GroupPicturePath;

        using var transaction = await _db.Database.BeginTransactionAsync();

        try
        {
            if (image != null)
            {
                var compressedImage = await _fileCompressService.CompressFileAsync(image);

                string path = await _storageService.SaveFileAsync(
                    compressedImage.Stream,
                    compressedImage.ContentType,
                    "group-pictures"
                );

                group.GroupPicturePath = path;
                group.GroupPictureFileName = image.FileName;
            }
            else
            {
                group.GroupPicturePath = null;
                group.GroupPictureFileName = null;
            }

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            // Cleanup old file after successful commit
            if (!string.IsNullOrEmpty(oldPath))
            {
                await _storageService.DeleteFileAsync("profile-pictures", oldPath);
            }

            return group.GroupPicturePath;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<uint> GetBoardGroupId(CancellationToken cancellationToken)
    {
        var setting = await _db.Settings.FindAsync(new [] { "BoardGroupId" }, cancellationToken);
        if (setting == null || !uint.TryParse(setting.Value, out uint boardGroupId))
            throw new KeyNotFoundException("BoardGroupId setting is missing or invalid.");

        return boardGroupId;
    }

    public async Task<uint> GetCandidateBoardGroupId(CancellationToken cancellationToken)
    {
        var setting = await _db.Settings.FindAsync(new[] { "CandidateBoardGroupId" }, cancellationToken);
        if (setting == null || !uint.TryParse(setting.Value, out uint candidateBoardGroupId))
            throw new KeyNotFoundException("CandidateBoardGroupId setting is missing or invalid.");

        return candidateBoardGroupId;
    }
}