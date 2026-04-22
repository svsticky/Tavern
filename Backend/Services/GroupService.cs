using Backend.Controllers.DTOs;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Backend.Projections;
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
        if (dto.MembershipYear == null || dto.IncludeInactive)
        {
            _permissionService.EnsureBoardOrCandidateBoardMember(userId);
        }

        return await _db.Groups
            .Select(GroupProjections.ToDto())
            .ToListAsync(cancellationToken);
    }

    public async Task<GroupResponseDTO?> GetGroup(uint id, CancellationToken cancellationToken)
    {
        return await _db.Groups
            .Where(g => g.Id == id)
            .Include(g => g.GroupMemberships)
            .ThenInclude(gm => gm.Member)
            .Select(GroupProjections.ToDto())
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Group> CreateGroup(PostGroupDTO dto, Guid userId, CancellationToken cancellationToken)
    {
        _permissionService.EnsureBoardOrCandidateBoardMember(userId);

        GroupValidator.ValidateName(dto.Name);

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
                await SaveGroupPicture(group, dto.GroupPicture);
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
        _permissionService.EnsureBoardOrCandidateBoardMember(userId);

        var group = await GetGroupOrThrow(id, cancellationToken);

        if (group.GroupPicturePath != null)
            await _storageService.DeleteFileAsync("group-pictures", group.GroupPicturePath);

        _db.Groups.Remove(group);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task PatchGroup(uint id, Guid userId, JsonPatchDocument<Group> patchDoc, CancellationToken cancellationToken)
    {
        _permissionService.EnsureBoardOrCandidateBoardMember(userId);

        if (patchDoc == null)
            throw new ArgumentException("Patch document is null");

        var group = await GetGroupOrThrow(id, cancellationToken);

        patchDoc.ApplyTo(group);

        StateValidator.Validate(group);

        GroupValidator.ValidateName(group.Name);

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateGroup(uint id, Guid userId, GroupUpdateDTO dto, CancellationToken cancellationToken)
    {
        _permissionService.EnsureBoardOrCandidateBoardMember(userId);

        GroupValidator.ValidateName(dto.Name);

        var group = await GetGroupOrThrow(id, cancellationToken);

        group.Name = dto.Name;
        group.Active = dto.Active;
        group.Type = dto.Type;

        StateValidator.Validate(group);

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<string?> UploadGroupPicture(uint groupId, Guid userId, IFormFile? image)
    {
        var group = await GetGroupOrThrow(groupId, default, "Group not found");

        _permissionService.EnsureBoardOrCandidateBoardMember(userId);

        ValidateGroupPicture(image);

        string? oldPath = group.GroupPicturePath;

        using var transaction = await _db.Database.BeginTransactionAsync();

        try
        {
            if (image != null)
            {
                await SaveGroupPicture(group, image);
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
                await _storageService.DeleteFileAsync("group-pictures", oldPath);
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

    private async Task<Group> GetGroupOrThrow(uint groupId, CancellationToken cancellationToken, string errorMessage = "")
    {
        var group = await _db.Groups.FindAsync(new object[] { groupId }, cancellationToken);
        if (group != null)
            return group;

        if (!string.IsNullOrEmpty(errorMessage))
            throw new Exception(errorMessage);

        throw new KeyNotFoundException();
    }

    private static void ValidateGroupPicture(IFormFile? image)
    {
        if (image != null)
        {
            ExtensionValidator.ValidateProfilePictureExtension(image);
        }
    }

    private async Task SaveGroupPicture(Group group, IFormFile image)
    {
        ValidateGroupPicture(image);
        var compressedImage = await _fileCompressService.CompressFileAsync(image);
        string path = await _storageService.SaveFileAsync(
            compressedImage.Stream,
            compressedImage.ContentType,
            "group-pictures"
        );

        group.GroupPicturePath = path;
        group.GroupPictureFileName = image.FileName;
    }
}
