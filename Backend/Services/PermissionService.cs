using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Backend.Utils.DateTime;

namespace Backend.Services;

/// <summary>
/// Implements permission and authorization checks for group and role memberships.
/// </summary>
public class PermissionService(
    PostgresDbContext db,
    ILogger<PermissionService> logger) : IPermissionService
{
    #region Group Checks
    /// <inheritdoc />
    public bool IsInGroupInCurrentYear(Guid memberId, uint groupId) 
        => IsInGroup(memberId, groupId, FinancialYearUtils.GetCurrentFinancialYear());

    /// <inheritdoc />
    public bool IsInGroupInCurrentYear(Member member, uint groupId) 
        => IsInGroup(member, groupId, FinancialYearUtils.GetCurrentFinancialYear());

    /// <inheritdoc />
    public bool IsInGroup(Guid memberId, uint groupId, uint year)
    {
        return db.GroupMemberships.Any(gm => 
            gm.MemberId == memberId && 
            gm.GroupId == groupId && 
            gm.MembershipYear == year);
    }

    /// <inheritdoc />
    public bool IsInGroup(Member member, uint groupId, uint year)
    {
        return member.GroupMemberships.Any(gm => 
            gm.GroupId == groupId && 
            gm.MembershipYear == year);
    }
    #endregion

    #region Role Checks
    /// <inheritdoc />
    public bool IsInRoleInCurrentYear(Guid memberId, uint roleId, uint? groupId = null)
        => IsInRole(memberId, roleId, FinancialYearUtils.GetCurrentFinancialYear(), groupId);

    /// <inheritdoc />
    public bool IsInRoleInCurrentYear(Member member, uint roleId, uint? groupId = null)
        => IsInRole(member, roleId, FinancialYearUtils.GetCurrentFinancialYear(), groupId);

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <summary>
    /// Checks whether a member belongs to the board group in the current year.
    /// </summary>
    /// <param name="memberId">The member user ID.</param>
    /// <returns><c>true</c> when the member is in the board group; otherwise <c>false</c>.</returns>
    public bool IsBoardMember(Guid memberId)
    {
        return IsInGroupInCurrentYear(memberId, uint.Parse(db.Settings.FirstOrDefault(s => s.Name == "BoardGroupId")?.Value ?? "0"));
    }

    /// <summary>
    /// Ensures the user is a board member and throws when not authorized.
    /// </summary>
    /// <param name="userId">The user ID to authorize.</param>
    public void EnsureBoardMember(Guid userId)
    {
        if (!IsBoardMember(userId)!)
        {
            logger.LogWarning("Unauthorized board-only access for user {UserId}.", userId);
            throw new UnauthorizedAccessException();
        }
    }

    /// <inheritdoc />
    public bool IsBoardOrCandidateBoardMember(Guid memberId)
    {
        return IsInGroupInCurrentYear(memberId, uint.Parse(db.Settings.FirstOrDefault(s => s.Name == "BoardGroupId")?.Value ?? "0")) || 
               IsInGroupInCurrentYear(memberId, uint.Parse(db.Settings.FirstOrDefault(s => s.Name == "CandidateBoardGroupId")?.Value ?? "0"));
    }

    /// <inheritdoc />
    public void EnsureBoardOrCandidateBoardMember(Guid userId)
    {
        if (!IsBoardOrCandidateBoardMember(userId)!)
        {
            logger.LogWarning("Unauthorized board-or-candidate access for user {UserId}.", userId);
            throw new UnauthorizedAccessException();
        }
    }
}
