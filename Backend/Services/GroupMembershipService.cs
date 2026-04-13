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

    public GroupMembershipService(PostgresDbContext db, IPermissionService permissionService)
    {
        _db = db;
        _permissionService = permissionService;
    }

    public async Task<IEnumerable<GroupMembershipResponseDTO>> GetGroupMemberships(Guid userId, bool onlyOwnMemberships, CancellationToken cancellationToken)
    {
        if (!onlyOwnMemberships &&
            !_permissionService.IsInGroupInCurrentYear(userId, PredefinedGroups.Board))
        {
            throw new UnauthorizedAccessException("Only board members can view group memberships.");
        }

        return await _db.GroupMemberships
            .Where(gm => !onlyOwnMemberships || gm.MemberId == userId)
            .Select(cm => new GroupMembershipResponseDTO
            {
                Id = cm.Id,
                MemberId = cm.MemberId,
                MemberName = $"{cm.Member.FirstName} {cm.Member.LastName}",
                GroupId = cm.GroupId,
                GroupName = cm.Group.Name,
                GroupType = cm.Group.Type,
                MembershipYear = cm.MembershipYear,
                RoleAliasId = cm.RoleAlias != null ? cm.RoleAlias.Id : null,
                RoleAliasName = cm.RoleAlias != null ? cm.RoleAlias.Name : null
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<GroupMembershipResponseDTO?> GetGroupMembership(uint id, Guid userId, CancellationToken cancellationToken)
    {
        var result = await _db.GroupMemberships
            .Where(cm => cm.Id == id)
            .Select(cm => new GroupMembershipResponseDTO
            {
                Id = cm.Id,
                MemberId = cm.MemberId,
                MemberName = $"{cm.Member.FirstName} {cm.Member.LastName}",
                GroupId = cm.GroupId,
                GroupName = cm.Group.Name,
                GroupType = cm.Group.Type,
                MembershipYear = cm.MembershipYear,
                RoleAliasId = cm.RoleAlias != null ? cm.RoleAlias.Id : null,
                RoleAliasName = cm.RoleAlias != null ? cm.RoleAlias.Name : null
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (result == null)
            return null;

        if (!_permissionService.IsInGroupInCurrentYear(userId, PredefinedGroups.Board)
            && result.MemberId != userId)
        {
            throw new UnauthorizedAccessException("Only board members can view group memberships.");
        }

        return result;
    }

    public async Task<GroupMembership> CreateGroupMembership(PostGroupMembershipDTO dto, Guid userId, CancellationToken cancellationToken)
    {
        if (!_permissionService.IsInGroupInCurrentYear(userId, PredefinedGroups.Board))
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

            StateValidateUtils.Validate(membership);

            var entry = _db.GroupMemberships.Add(membership);

            _db.KeycloakOutboxTasks.Add(new KeycloakOutboxTask
            {
                KeycloakId = member.KeycloakId ?? throw new Exception("Member does not have a Keycloak ID."),
                TaskType = KeycloakTaskType.Sync
            });

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
        if (!_permissionService.IsInGroupInCurrentYear(userId, PredefinedGroups.Board))
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

            _db.KeycloakOutboxTasks.Add(new KeycloakOutboxTask
            {
                KeycloakId = membership.Member.KeycloakId ?? throw new Exception("Member does not have a Keycloak ID."),
                TaskType = KeycloakTaskType.Sync
            });

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
        if (!_permissionService.IsInGroupInCurrentYear(userId, PredefinedGroups.Board))
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

            StateValidateUtils.Validate(membership);

            await _db.SaveChangesAsync(cancellationToken);

            _db.KeycloakOutboxTasks.Add(new KeycloakOutboxTask
            {
                KeycloakId = membership.Member.KeycloakId ?? throw new Exception("Member does not have a Keycloak ID."),
                TaskType = KeycloakTaskType.Sync
            });

            if (oldMemberId != membership.MemberId)
            {
                var oldMember = await _db.Members.FindAsync(oldMemberId);
                if (oldMember != null)
                {
                    _db.KeycloakOutboxTasks.Add(new KeycloakOutboxTask
                    {
                        KeycloakId = oldMember.KeycloakId ?? throw new Exception("Old member does not have a Keycloak ID."),
                        TaskType = KeycloakTaskType.Sync
                    });
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
        if (!_permissionService.IsInGroupInCurrentYear(userId, PredefinedGroups.Board))
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

            StateValidateUtils.Validate(membership);

            _db.KeycloakOutboxTasks.Add(new KeycloakOutboxTask
            {
                KeycloakId = membership.Member.KeycloakId ?? throw new Exception("Member does not have a Keycloak ID."),
                TaskType = KeycloakTaskType.Sync
            });

            if (oldMemberId != membership.MemberId)
            {
                var oldMember = await _db.Members.FindAsync(oldMemberId);
                if (oldMember != null)
                {
                    _db.KeycloakOutboxTasks.Add(new KeycloakOutboxTask
                    {
                        KeycloakId = oldMember.KeycloakId ?? throw new Exception("Old member does not have a Keycloak ID."),
                        TaskType = KeycloakTaskType.Sync
                    });
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