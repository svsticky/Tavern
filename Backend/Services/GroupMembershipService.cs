using Backend.Controllers.DTOs;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class GroupMembershipService : IGroupMembershipService
{
    private readonly PostgresDbContext _db;
    private readonly IPermissionService _permissionService;
    private readonly KeycloakOutboxWorker _keycloakOutboxWorker;

    public GroupMembershipService(PostgresDbContext db, IPermissionService permissionService, KeycloakOutboxWorker keycloakOutboxService)
    {
        _db = db;
        _permissionService = permissionService;
        _keycloakOutboxWorker = keycloakOutboxService;
    }

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
            if(dto.MemberId != userId)
            {
                _permissionService.EnsureBoardOrCandidateBoardMember(userId);
            }
            query = query.Where(gm => gm.MemberId == dto.MemberId);
        }

        return await query
            .Select(GroupMembershipProjections.ToDto())
            .ToListAsync(cancellationToken);
    }

    public async Task<GroupMembershipResponseDTO?> GetGroupMembership(uint id, Guid userId, CancellationToken cancellationToken)
    {
        var result = await _db.GroupMemberships
            .Where(cm => cm.Id == id)
            .Select(GroupMembershipProjections.ToDto())
            .FirstOrDefaultAsync(cancellationToken);

        if (result == null)
            return null;

        if (!_permissionService.IsBoardOrCandidateBoardMember(userId)
            && result.MemberId != userId)
        {
            throw new UnauthorizedAccessException("Only board members can view group memberships.");
        }

        return result;
    }

    public async Task<GroupMembership> CreateGroupMembership(PostGroupMembershipDTO dto, Guid userId, CancellationToken cancellationToken)
    {
        if (!_permissionService.IsBoardOrCandidateBoardMember(userId))
            throw new UnauthorizedAccessException();

        var member = await _db.Members.FindAsync(dto.MemberId, cancellationToken)
            ?? throw new ArgumentException($"Member with ID {dto.MemberId} does not exist.");

        var group = await _db.Groups.FindAsync(dto.GroupId, cancellationToken)
            ?? throw new ArgumentException($"Group with ID {dto.GroupId} does not exist.");

        if (dto.RoleAliasId.HasValue)
        {
            var roleAlias = await _db.RoleAliases.FindAsync(dto.RoleAliasId.Value, cancellationToken);
            if (roleAlias == null)
                throw new ArgumentException($"Role alias with ID {dto.RoleAliasId.Value} does not exist.");
        }

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

            _keycloakOutboxWorker.EnqueueTask(KeycloakTaskType.Sync, member.KeycloakId ?? throw new Exception("Member does not have a Keycloak ID."));

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return entry.Entity;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task DeleteGroupMembership(uint id, Guid userId, CancellationToken cancellationToken)
    {
        if (!_permissionService.IsBoardOrCandidateBoardMember(userId))
            throw new UnauthorizedAccessException("Only board members can delete group memberships.");

        var membership = await _db.GroupMemberships
            .Include(g => g.Member)
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

        if (membership == null)
            throw new KeyNotFoundException();

        using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            _db.GroupMemberships.Remove(membership);

            _keycloakOutboxWorker.EnqueueTask(KeycloakTaskType.Sync, membership.Member.KeycloakId ?? throw new Exception("Member does not have a Keycloak ID."));

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task PatchGroupMembership(uint id, Guid userId, JsonPatchDocument<GroupMembership> patchDoc, CancellationToken cancellationToken)
    {
        if (!_permissionService.IsBoardOrCandidateBoardMember(userId))
            throw new UnauthorizedAccessException("Only board members can update group memberships.");

        if (patchDoc == null)
            throw new ArgumentException("Patch document is null");

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

            _keycloakOutboxWorker.EnqueueTask(KeycloakTaskType.Sync, membership.Member.KeycloakId ?? throw new Exception("Member does not have a Keycloak ID."));

            if (oldMemberId != membership.MemberId)
            {
                var oldMember = await _db.Members.FindAsync(oldMemberId);
                if (oldMember != null)
                {
                    _keycloakOutboxWorker.EnqueueTask(KeycloakTaskType.Sync, oldMember.KeycloakId ?? throw new Exception("Old member does not have a Keycloak ID."));
                }
            }

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task UpdateGroupMembership(uint id, Guid userId, GroupMembershipUpdateDTO dto, CancellationToken cancellationToken)
    {
        if (!_permissionService.IsBoardOrCandidateBoardMember(userId))
            throw new UnauthorizedAccessException("Only board members can update group memberships.");

        var membership = await _db.GroupMemberships
            .Include(g => g.Member)
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

        if (membership == null)
            throw new KeyNotFoundException();

        var oldMemberId = membership.MemberId;

        using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            if (dto.RoleAliasId.HasValue)
            {
                var roleAlias = await _db.RoleAliases.FindAsync(dto.RoleAliasId.Value, cancellationToken);
                if (roleAlias == null)
                    throw new ArgumentException($"Role alias with ID {dto.RoleAliasId.Value} does not exist.");
            }

            membership.RoleAliasId = dto.RoleAliasId;

            StateValidator.Validate(membership);

            _keycloakOutboxWorker.EnqueueTask(KeycloakTaskType.Sync, membership.Member.KeycloakId ?? throw new Exception("Member does not have a Keycloak ID."));

            if (oldMemberId != membership.MemberId)
            {
                var oldMember = await _db.Members.FindAsync(oldMemberId);
                if (oldMember != null)
                {
                    _keycloakOutboxWorker.EnqueueTask(KeycloakTaskType.Sync, oldMember.KeycloakId ?? throw new Exception("Old member does not have a Keycloak ID."));
                }
            }

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}