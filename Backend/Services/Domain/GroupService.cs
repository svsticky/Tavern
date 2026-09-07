using Backend.Controllers.DTOs;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models;
using Backend.Models.Domain;
using Backend.Utils.DateTime;
using Backend.Validators;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Backend.Services.Domain;

/// <summary>
/// Implements group management and group picture operations.
/// </summary>
public class GroupService : IGroupService
{
    private readonly PostgresDbContext _db;
    private readonly IPermissionService _permissionService;
    private readonly IFileCompressService _fileCompressService;
    private readonly IStorageService _storageService;
    private readonly IMemoryCache _memoryCache;
    private readonly AuthOutboxWorker _authOutboxWorker;
    private readonly ILogger<GroupService> _logger;

    /// <summary>
    /// Initializes a new instance of the GroupService class with the specified dependencies. The constructor sets up the necessary services for managing groups, including database access, permission checks, file compression for group pictures, storage service for saving group pictures, and logging for monitoring group-related operations. This setup allows the GroupService to effectively handle group creation, retrieval, updating, deletion, and picture management while ensuring that only authorized users can perform these actions and that any significant events are logged for auditing and debugging purposes.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="permissionService">The permission service.</param>
    /// <param name="fileCompressService">The file compression service.</param>
    /// <param name="storageService">The storage service.</param>
    /// <param name="memoryCache">The memory cache service.</param>
    /// <param name="authOutboxWorker">The authentication outbox worker.</param>
    /// <param name="logger">The logger.</param>
    public GroupService(
        PostgresDbContext db,
        IPermissionService permissionService,
        IFileCompressService fileCompressService,
        IStorageService storageService,
        IMemoryCache memoryCache,
        AuthOutboxWorker authOutboxWorker,
        ILogger<GroupService> logger)
    {
        _db = db;
        _permissionService = permissionService;
        _fileCompressService = fileCompressService;
        _storageService = storageService;
        _memoryCache = memoryCache;
        _authOutboxWorker = authOutboxWorker;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<GroupResponseDTO>> GetGroups(Guid userId, GetGroupDTO dto, CancellationToken cancellationToken)
    {
        if (dto.MembershipYear == null || dto.IncludeInactive)
        {
            _permissionService.EnsurePermission(userId, Permission.ManageGroups);
        }

        bool isBoardMember = _permissionService.IsBoardOrCandidateBoardMember(userId);

        return await _db.Groups
            .Where(g => dto.IncludeInactive || g.Active)
            .Where(g => isBoardMember || dto.MembershipYear == null || g.GroupMemberships.Any(gm => gm.MembershipYear == dto.MembershipYear && userId == gm.MemberId))
            .Select(GroupResponseDTO.ToDto())
            .OrderBy(g => g.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<GroupResponseDTO?> GetGroup(uint id, CancellationToken cancellationToken)
    {
        return await _db.Groups
            .Where(g => g.Id == id)
            .Include(g => g.GroupMemberships)
            .ThenInclude(gm => gm.Member)
            .Select(GroupResponseDTO.ToDto())
            .FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Group> CreateGroup(PostGroupDTO dto, Guid userId, CancellationToken cancellationToken)
    {
        _permissionService.EnsurePermission(userId, Permission.ManageGroups);
        _logger.LogInformation("Creating group {GroupName} by user {UserId}.", dto.Name, userId);

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
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Failed creating group {GroupName}.", dto.Name);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<FileResultDto?> GetGroupPictureFile(string path)
    {
        string cacheKey = $"group-pic-{path}";
        if (_memoryCache.TryGetValue(cacheKey, out (byte[] bytes, string contentType) cached))
        {
            return new FileResultDto
            {
                Stream = new MemoryStream(cached.bytes),
                ContentType = cached.contentType
            };
        }

        var file = await _storageService.GetFileAsync("group-pictures", path);

        if (file == null) return null;

        using var memoryStream = new MemoryStream();
        await file.Stream.CopyToAsync(memoryStream);
        byte[] bytes = memoryStream.ToArray();

        _memoryCache.Set(cacheKey, (bytes, file.ContentType), TimeSpan.FromHours(1));

        return new FileResultDto
        {
            Stream = new MemoryStream(bytes),
            ContentType = file.ContentType
        };
    }

    /// <inheritdoc />
    public async Task DeleteGroup(uint id, Guid userId, CancellationToken cancellationToken)
    {
        _permissionService.EnsurePermission(userId, Permission.ManageGroups);
        _logger.LogInformation("Deleting group {GroupId} by user {UserId}.", id, userId);

        var group = await GetGroupOrThrow(id, cancellationToken);

        if (group.GroupPicturePath != null)
        {
            await _storageService.DeleteFileAsync("group-pictures", group.GroupPicturePath);
            _memoryCache.Remove($"group-pic-{group.GroupPicturePath}");
        }

        _db.Groups.Remove(group);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task PatchGroup(uint id, Guid userId, JsonPatchDocument<Group> patchDoc, CancellationToken cancellationToken)
    {
        _permissionService.EnsurePermission(userId, Permission.ManageGroups);
        _logger.LogInformation("Patching group {GroupId} by user {UserId}.", id, userId);

        if (patchDoc == null)
            throw new ArgumentException("Patch document is null");

        if (patchDoc.Operations.Any(op => op.path.Equals("/id", StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("Cannot modify Id field.");

        var group = await GetGroupOrThrow(id, cancellationToken);

        patchDoc.ApplyTo(group);

        StateValidator.Validate(group);

        GroupValidator.ValidateName(group.Name);

        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateGroup(uint id, Guid userId, GroupUpdateDTO dto, CancellationToken cancellationToken)
    {
        _permissionService.EnsurePermission(userId, Permission.ManageGroups);
        _logger.LogInformation("Updating group {GroupId} by user {UserId}.", id, userId);

        GroupValidator.ValidateName(dto.Name);

        var group = await GetGroupOrThrow(id, cancellationToken);

        group.Name = dto.Name;
        group.Active = dto.Active;
        group.Type = dto.Type;

        StateValidator.Validate(group);

        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string?> UploadGroupPicture(uint groupId, Guid userId, IFormFile? image)
    {
        var group = await GetGroupOrThrow(groupId, default, "Group not found");
        _logger.LogInformation("Uploading group picture for group {GroupId} by user {UserId}.", groupId, userId);

        _permissionService.EnsurePermission(userId, Permission.ManageGroups);

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
                _memoryCache.Remove($"group-pic-{oldPath}");
            }

            return group.GroupPicturePath;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed uploading group picture for group {GroupId}.", groupId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<uint> GetBoardGroupId(CancellationToken cancellationToken)
    {
        var setting = await _db.Settings.FindAsync(new object[] { "BoardGroupId" }, cancellationToken);
        if (setting == null || !uint.TryParse(setting.Value, out uint boardGroupId))
            throw new KeyNotFoundException("BoardGroupId setting is missing or invalid.");

        return boardGroupId;
    }

    /// <inheritdoc />
    public async Task<uint> GetCandidateBoardGroupId(CancellationToken cancellationToken)
    {
        var setting = await _db.Settings.FindAsync(new object[] { "CandidateBoardGroupId" }, cancellationToken);
        if (setting == null || !uint.TryParse(setting.Value, out uint candidateBoardGroupId))
            throw new KeyNotFoundException("CandidateBoardGroupId setting is missing or invalid.");

        return candidateBoardGroupId;
    }

    /// <inheritdoc />
    public async Task<List<string>> GetGroupPermissions(uint id, CancellationToken cancellationToken)
    {
        await GetGroupOrThrow(id, cancellationToken);

        return await _db.GroupPermissions
            .Where(gp => gp.GroupId == id)
            .Select(gp => gp.PermissionKey)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task SetGroupPermissions(uint id, List<string> permissions, Guid userId, CancellationToken cancellationToken)
    {
        _permissionService.EnsurePermission(userId, Permission.ManageGroupPermissions);
        _logger.LogInformation("Setting permissions for group {GroupId} by user {UserId}.", id, userId);

        await GetGroupOrThrow(id, cancellationToken);

        var distinctPermissions = permissions.Where(p => !string.IsNullOrWhiteSpace(p)).Distinct().ToList();
        PermissionValidator.ValidateCustomPermissions(distinctPermissions);

        using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var existing = await _db.GroupPermissions.Where(gp => gp.GroupId == id).ToListAsync(cancellationToken);
            _db.GroupPermissions.RemoveRange(existing);

            foreach (var permission in distinctPermissions)
            {
                _db.GroupPermissions.Add(new GroupPermission { GroupId = id, PermissionKey = permission });
            }

            var currentYear = YearUtils.GetYearForDate(System.DateTime.UtcNow, YearUtils.CommitteeCreationDate);
            var affectedMembers = await _db.GroupMemberships
                .Where(gm => gm.GroupId == id && gm.MembershipYear == currentYear)
                .Select(gm => gm.Member.AuthSystemUserId)
                .Distinct()
                .ToListAsync(cancellationToken);

            foreach (var authSystemId in affectedMembers)
            {
                if (authSystemId.HasValue)
                {
                    _authOutboxWorker.EnqueueTask(AuthTaskType.Sync, authSystemId.Value, _db);
                }
            }

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Failed setting permissions for group {GroupId}.", id);
            throw;
        }
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
