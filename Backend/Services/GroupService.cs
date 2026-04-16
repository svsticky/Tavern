using Backend.Controllers.DTOs;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class GroupService : IGroupService
{
    private readonly PostgresDbContext _db;
    private readonly IPermissionService _permissionService;

    public GroupService(PostgresDbContext db, IPermissionService permissionService)
    {
        _db = db;
        _permissionService = permissionService;
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
                GroupMemberships = c.GroupMemberships.Select(cm => new GroupMembershipSummaryDTO
                {
                    Member = new MemberSummaryDTO
                    {
                        Id = cm.Member.Id,
                        FirstName = cm.Member.FirstName,
                        LastName = cm.Member.LastName,
                    },
                    GroupName = c.Name,
                    MembershipYear = cm.MembershipYear
                }).ToList()
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<GroupResponseDTO?> GetGroup(uint id, CancellationToken cancellationToken)
    {
        return await _db.Groups
            .Where(g => g.Id == id)
            .Select(g => new GroupResponseDTO
            {
                Id = g.Id,
                Name = g.Name,
                Active = g.Active,
                Type = g.Type,
                GroupMemberships = g.GroupMemberships.Select(gm => new GroupMembershipSummaryDTO
                {
                    Member = new MemberSummaryDTO
                    {
                        Id = gm.Member.Id,
                        FirstName = gm.Member.FirstName,
                        LastName = gm.Member.LastName
                    },
                    GroupName = g.Name,
                    MembershipYear = gm.MembershipYear
                }).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<Group> CreateGroup(PostGroupDTO dto, Guid userId, CancellationToken cancellationToken)
    {
        if (!_permissionService.IsBoardOrCandidateBoardMember(userId))
            throw new UnauthorizedAccessException("Only board members can create groups.");

        if (dto.Name.Contains(';') || dto.Name.Contains(':'))
            throw new ArgumentException("Group names cannot contain ';' or ':'.");

        var group = new Group
        {
            Name = dto.Name,
            Type = dto.Type
        };

        StateValidator.Validate(group);

        _db.Groups.Add(group);
        await _db.SaveChangesAsync(cancellationToken);

        return group;
    }

    public async Task DeleteGroup(uint id, Guid userId, CancellationToken cancellationToken)
    {
        if (!_permissionService.IsBoardOrCandidateBoardMember(userId))
            throw new UnauthorizedAccessException("Only board members can delete groups.");

        var group = await _db.Groups.FindAsync(id, cancellationToken);
        if (group == null)
            throw new KeyNotFoundException();

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
}