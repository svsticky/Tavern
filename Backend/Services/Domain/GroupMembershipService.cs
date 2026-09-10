using Backend.Controllers.DTOs;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Backend.Validators;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services.Domain;

/// <summary>
/// Implements group-membership management operations.
/// </summary>
public class GroupMembershipService : IGroupMembershipService
{
    private readonly PostgresDbContext _db;
    private readonly IPermissionService _permissionService;
    private readonly AuthOutboxWorker _authOutboxWorker;
    private readonly ILogger<GroupMembershipService> _logger;
    private readonly IEnumerable<IAdminStatusUpdateOutboxWorker> _adminStatusWorkers;

    /// <summary>
    /// Initializes a new instance of the GroupMembershipService class with the specified dependencies.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="permissionService">The permission service.</param>
    /// <param name="authOutboxWorker">The authentication outbox worker.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="adminStatusWorkers">Optional workers for updating admin status across external services.</param>
    public GroupMembershipService(
        PostgresDbContext db,
        IPermissionService permissionService,
        AuthOutboxWorker authOutboxWorker,
        ILogger<GroupMembershipService> logger,
        IEnumerable<IAdminStatusUpdateOutboxWorker>? adminStatusWorkers = null)
    {
        _db = db;
        _permissionService = permissionService;
        _authOutboxWorker = authOutboxWorker;
        _logger = logger;
        _adminStatusWorkers = adminStatusWorkers ?? [];
    }

    private void EnqueueAdminStatusSync(Guid memberId, string? email = null)
    {
        var memberEmail = email;
        if (string.IsNullOrEmpty(memberEmail))
        {
            memberEmail = _db.Members.Find(memberId)?.Email;
        }

        if (string.IsNullOrEmpty(memberEmail)) return;

        bool isAdmin = _permissionService.IsBoardOrCandidateBoardMember(memberId);
        foreach (var worker in _adminStatusWorkers)
        {
            worker.EnqueueAdminStatusUpdate(memberEmail, isAdmin, _db);
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<GroupMembershipResponseDTO>> GetGroupMemberships(GetGroupMembershipsDTO dto, Guid userId, CancellationToken cancellationToken)
    {
        var query = _db.GroupMemberships.AsQueryable();

        if (dto.GroupId != null)
        {
            _permissionService.EnsureBoardOrCandidateBoardMember(userId);
            query = query.Where(gm => gm.GroupId == dto.GroupId);
        }

        if (dto.MembershipYear != null)
        {
            query = query.Where(gm => gm.MembershipYear == dto.MembershipYear);
        }

        if (dto.MemberId != null)
        {
            if (dto.MemberId != userId)
            {
                _permissionService.EnsureBoardOrCandidateBoardMember(userId);
            }
            query = query.Where(gm => gm.MemberId == dto.MemberId);
        }

        return await query
            .Select(GroupMembershipResponseDTO.ToDto(userId, _permissionService.IsBoardOrCandidateBoardMember(userId)))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<GroupMembershipResponseDTO?> GetGroupMembership(uint id, Guid userId, CancellationToken cancellationToken)
    {
        var result = await _db.GroupMemberships
            .Where(cm => cm.Id == id)
            .Select(GroupMembershipResponseDTO.ToDto(userId, _permissionService.IsBoardOrCandidateBoardMember(userId)))
            .FirstOrDefaultAsync(cancellationToken);

        if (result == null)
            return null;

        if (!_permissionService.IsBoardOrCandidateBoardMember(userId)
            && result.MemberId != userId)
        {
            throw new UnauthorizedAccessException();
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<GroupMembership> CreateGroupMembership(PostGroupMembershipDTO dto, Guid userId, CancellationToken cancellationToken)
    {
        _permissionService.EnsureBoardOrCandidateBoardMember(userId);
        _logger.LogInformation("Creating group membership for member {MemberId} in group {GroupId} by user {UserId}.", dto.MemberId, dto.GroupId, userId);

        var member = await GetMemberOrThrow(dto.MemberId, cancellationToken);
        var group = await GetGroupOrThrow(dto.GroupId, cancellationToken);
        await EnsureRoleAliasExists(dto.RoleAliasId, cancellationToken);

        using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var membership = new GroupMembership
            {
                Member = member,
                Group = group,
                MembershipYear = dto.MembershipYear,
                RoleAliasId = dto.RoleAliasId
            };

            StateValidator.Validate(membership);

            var entry = _db.GroupMemberships.Add(membership);

            _authOutboxWorker.EnqueueTask(AuthTaskType.Sync, member.Id, _db);
            EnqueueAdminStatusSync(member.Id, member.Email);

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return entry.Entity;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Failed creating group membership for member {MemberId} in group {GroupId}.", dto.MemberId, dto.GroupId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task DeleteGroupMembership(uint id, Guid userId, CancellationToken cancellationToken)
    {
        _permissionService.EnsureBoardOrCandidateBoardMember(userId);
        _logger.LogInformation("Deleting group membership {MembershipId} by user {UserId}.", id, userId);

        var membership = await _db.GroupMemberships
            .Include(g => g.Member)
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

        if (membership == null)
            throw new KeyNotFoundException();

        using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            _db.GroupMemberships.Remove(membership);

            _authOutboxWorker.EnqueueTask(AuthTaskType.Sync, membership.MemberId, _db);
            EnqueueAdminStatusSync(membership.MemberId, membership.Member?.Email);

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Failed deleting group membership {MembershipId}.", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task PatchGroupMembership(uint id, Guid userId, JsonPatchDocument<GroupMembership> patchDoc, CancellationToken cancellationToken)
    {
        _permissionService.EnsureBoardOrCandidateBoardMember(userId);
        _logger.LogInformation("Patching group membership {MembershipId} by user {UserId}.", id, userId);

        if (patchDoc == null)
            throw new ArgumentException("Patch document is null");

        if (patchDoc.Operations.Any(op => op.path.Equals("/id", StringComparison.OrdinalIgnoreCase)
            || op.path.Equals("/member", StringComparison.OrdinalIgnoreCase)
            || op.path.Equals("/memberId", StringComparison.OrdinalIgnoreCase)
            || op.path.Equals("/group", StringComparison.OrdinalIgnoreCase)
            || op.path.Equals("/groupId", StringComparison.OrdinalIgnoreCase)
            || op.path.Equals("/groupAlias", StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("Cannot modify Id, MemberId or GroupId fields.");

        var membership = await _db.GroupMemberships
            .Include(g => g.Member)
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

        if (membership == null)
            throw new KeyNotFoundException();

        var oldMemberId = membership.MemberId;

        using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            patchDoc.ApplyTo(membership);

            StateValidator.Validate(membership);

            await _db.SaveChangesAsync(cancellationToken);

            _authOutboxWorker.EnqueueTask(AuthTaskType.Sync, membership.MemberId, _db);
            EnqueueAdminStatusSync(membership.MemberId);

            if (oldMemberId != membership.MemberId)
            {
                var oldMember = await _db.Members.FindAsync(oldMemberId);
                if (oldMember != null)
                {
                    _authOutboxWorker.EnqueueTask(AuthTaskType.Sync, oldMember.Id, _db);
                    EnqueueAdminStatusSync(oldMember.Id, oldMember.Email);
                }
            }

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Failed patching group membership {MembershipId}.", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task UpdateGroupMembership(uint id, Guid userId, GroupMembershipUpdateDTO dto, CancellationToken cancellationToken)
    {
        _permissionService.EnsureBoardOrCandidateBoardMember(userId);
        _logger.LogInformation("Updating group membership {MembershipId} by user {UserId}.", id, userId);

        var membership = await _db.GroupMemberships
            .Include(g => g.Member)
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

        if (membership == null)
            throw new KeyNotFoundException();

        var oldMemberId = membership.MemberId;

        using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            await EnsureRoleAliasExists(dto.RoleAliasId, cancellationToken);

            membership.RoleAliasId = dto.RoleAliasId;

            StateValidator.Validate(membership);

            _authOutboxWorker.EnqueueTask(AuthTaskType.Sync, membership.MemberId, _db);
            EnqueueAdminStatusSync(membership.MemberId);

            if (oldMemberId != membership.MemberId)
            {
                var oldMember = await _db.Members.FindAsync(oldMemberId);
                if (oldMember != null)
                {
                    _authOutboxWorker.EnqueueTask(AuthTaskType.Sync, oldMember.Id, _db);
                    EnqueueAdminStatusSync(oldMember.Id, oldMember.Email);
                }
            }

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Failed updating group membership {MembershipId}.", id);
            throw;
        }
    }

    private async Task<Member> GetMemberOrThrow(Guid memberId, CancellationToken cancellationToken)
    {
        var member = await _db.Members.FindAsync(new object[] { memberId }, cancellationToken);
        return member ?? throw new ArgumentException($"Member with ID {memberId} does not exist.");
    }

    private async Task<Group> GetGroupOrThrow(uint groupId, CancellationToken cancellationToken)
    {
        var group = await _db.Groups.FindAsync(new object[] { groupId }, cancellationToken);
        return group ?? throw new ArgumentException($"Group with ID {groupId} does not exist.");
    }

    private async Task EnsureRoleAliasExists(uint? roleAliasId, CancellationToken cancellationToken)
    {
        if (!roleAliasId.HasValue)
            return;

        var roleAlias = await _db.RoleAliases.FindAsync(roleAliasId.Value, cancellationToken);
        if (roleAlias == null)
            throw new ArgumentException($"Role alias with ID {roleAliasId.Value} does not exist.");
    }
}
