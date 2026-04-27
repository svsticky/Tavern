using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Backend.Utils.DateTime;
using Microsoft.Extensions.Logging;

namespace Backend.Services;

public class PermissionService(
    PostgresDbContext db,
    ILogger<PermissionService> logger) : IPermissionService
{
    #region Group Checks
    public bool IsInGroupInCurrentYear(Guid memberId, uint groupId) 
        => IsInGroup(memberId, groupId, FinancialYearUtils.GetCurrentFinancialYear());

    public bool IsInGroupInCurrentYear(Member member, uint groupId) 
        => IsInGroup(member, groupId, FinancialYearUtils.GetCurrentFinancialYear());

    public bool IsInGroup(Guid memberId, uint groupId, uint year)
    {
        return db.GroupMemberships.Any(gm => 
            gm.MemberId == memberId && 
            gm.GroupId == groupId && 
            gm.MembershipYear == year);
    }

    public bool IsInGroup(Member member, uint groupId, uint year)
    {
        return member.GroupMemberships.Any(gm => 
            gm.GroupId == groupId && 
            gm.MembershipYear == year);
    }
    #endregion

    #region Role Checks
    public bool IsInRoleInCurrentYear(Guid memberId, uint roleId, uint? groupId = null)
        => IsInRole(memberId, roleId, FinancialYearUtils.GetCurrentFinancialYear(), groupId);

    public bool IsInRoleInCurrentYear(Member member, uint roleId, uint? groupId = null)
        => IsInRole(member, roleId, FinancialYearUtils.GetCurrentFinancialYear(), groupId);

    public bool IsInRole(Guid memberId, uint roleId, uint year, uint? groupId = null)
    {
        var query = db.GroupMemberships.Where(gm => 
            gm.MemberId == memberId && 
            gm.MembershipYear == year && 
            gm.RoleAlias != null && 
            gm.RoleAlias.RoleId == roleId);

        if (groupId.HasValue)
            query = query.Where(gm => gm.GroupId == groupId.Value);

        return query.Any();
    }

    public bool IsInRole(Member member, uint roleId, uint year, uint? groupId = null)
    {
        var query = member.GroupMemberships.AsQueryable().Where(gm => 
            gm.MembershipYear == year && 
            gm.RoleAlias != null && 
            gm.RoleAlias.RoleId == roleId);

        if (groupId.HasValue)
            query = query.Where(gm => gm.GroupId == groupId.Value);

        return query.Any();
    }
    #endregion

    public bool IsBoardMember(Guid memberId)
    {
        return IsInGroupInCurrentYear(memberId, uint.Parse(db.Settings.FirstOrDefault(s => s.Name == "BoardGroupId")?.Value ?? "0"));
    }

    public void EnsureBoardMember(Guid userId)
    {
        if (!IsBoardMember(userId)!)
        {
            logger.LogWarning("Unauthorized board-only access for user {UserId}.", userId);
            throw new UnauthorizedAccessException();
        }
    }

    public bool IsBoardOrCandidateBoardMember(Guid memberId)
    {
        return IsInGroupInCurrentYear(memberId, uint.Parse(db.Settings.FirstOrDefault(s => s.Name == "BoardGroupId")?.Value ?? "0")) || 
               IsInGroupInCurrentYear(memberId, uint.Parse(db.Settings.FirstOrDefault(s => s.Name == "CandidateBoardGroupId")?.Value ?? "0"));
    }

    public void EnsureBoardOrCandidateBoardMember(Guid userId)
    {
        if (!IsBoardOrCandidateBoardMember(userId)!)
        {
            logger.LogWarning("Unauthorized board-or-candidate access for user {UserId}.", userId);
            throw new UnauthorizedAccessException();
        }
    }
}
