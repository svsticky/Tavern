using Backend.Database;
using Backend.Interfaces;
using Backend.Models;
using Backend.Models.Domain;
using Backend.Utils.DateTime;
using System.Globalization;

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
        => IsInGroup(memberId, groupId, YearUtils.GetYearForDate(System.DateTime.UtcNow, YearUtils.CommitteeCreationDate));

    /// <inheritdoc />
    public bool IsInGroupInCurrentYear(Member member, uint groupId)
        => IsInGroup(member, groupId, YearUtils.GetYearForDate(System.DateTime.UtcNow, YearUtils.CommitteeCreationDate));

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
        => IsInRole(memberId, roleId, YearUtils.GetYearForDate(System.DateTime.UtcNow, YearUtils.CommitteeCreationDate), groupId);

    /// <inheritdoc />
    public bool IsInRoleInCurrentYear(Member member, uint roleId, uint? groupId = null)
        => IsInRole(member, roleId, YearUtils.GetYearForDate(System.DateTime.UtcNow, YearUtils.CommitteeCreationDate), groupId);

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
    /// Checks whether a member belongs to the board group in the current board year.
    /// </summary>
    /// <param name="memberId">The member user ID.</param>
    /// <returns><c>true</c> when the member is in the board group; otherwise <c>false</c>.</returns>
    public bool IsBoardMember(Guid memberId)
    {
        return IsInGroup(memberId, uint.Parse(db.Settings.FirstOrDefault(s => s.Name == "BoardGroupId")?.Value ?? "0", CultureInfo.InvariantCulture), YearUtils.GetBoardYear(db));
    }

    /// <summary>
    /// Ensures the user is a board member and throws when not authorized.
    /// </summary>
    /// <param name="userId">The user ID to authorize.</param>
    public void EnsureBoardMember(Guid userId)
    {
        if (!IsBoardMember(userId))
        {
            logger.LogWarning("Unauthorized board-only access for user {UserId}.", userId);
            throw new UnauthorizedAccessException();
        }
    }

    /// <inheritdoc />
    public bool IsBoardOrCandidateBoardMember(Guid memberId)
    {
        var boardYear = YearUtils.GetBoardYear(db);
        return IsInGroup(memberId, uint.Parse(db.Settings.FirstOrDefault(s => s.Name == "BoardGroupId")?.Value ?? "1", CultureInfo.InvariantCulture), boardYear) ||
               IsInGroup(memberId, uint.Parse(db.Settings.FirstOrDefault(s => s.Name == "CandidateBoardGroupId")?.Value ?? "1", CultureInfo.InvariantCulture), boardYear);
    }

    /// <inheritdoc />
    public void EnsureBoardOrCandidateBoardMember(Guid userId)
    {
        if (!IsBoardOrCandidateBoardMember(userId))
        {
            logger.LogWarning("Unauthorized board-or-candidate access for user {UserId}.", userId);
            throw new UnauthorizedAccessException();
        }
    }

    /// <summary>
    /// The one permission whose effect is scoped to the group it was granted through. Every other
    /// permission has a global effect - granting it via any group/role membership makes it true everywhere.
    /// </summary>
    private static readonly HashSet<Permission> _groupScopedPermissions = new() { Permission.EditActivityForGroup };

    /// <inheritdoc />
    public bool HasPermission(Guid memberId, Permission permission, uint? groupId = null)
    {
        var year = YearUtils.GetYearForDate(System.DateTime.UtcNow, YearUtils.CommitteeCreationDate);
        var permissionKey = permission.ToString();

        var query = db.GroupMemberships.Where(gm => gm.MemberId == memberId && gm.MembershipYear == year);

        if (_groupScopedPermissions.Contains(permission) && groupId.HasValue)
            query = query.Where(gm => gm.GroupId == groupId.Value);

        return query.Any(gm =>
            db.GroupPermissions.Any(gp => gp.GroupId == gm.GroupId && gp.PermissionKey == permissionKey) ||
            (gm.RoleAliasId != null && db.RolePermissions.Any(rp => rp.RoleId == gm.RoleAlias!.RoleId && rp.PermissionKey == permissionKey)));
    }

    /// <inheritdoc />
    public bool HasPermissionOrBoard(Guid memberId, Permission permission, uint? groupId = null)
        => IsBoardOrCandidateBoardMember(memberId) || HasPermission(memberId, permission, groupId);

    /// <inheritdoc />
    public void EnsurePermission(Guid userId, Permission permission, uint? groupId = null)
    {
        if (!HasPermissionOrBoard(userId, permission, groupId))
        {
            logger.LogWarning("Unauthorized access for user {UserId}, missing permission {Permission}.", userId, permission);
            throw new UnauthorizedAccessException();
        }
    }

    /// <inheritdoc />
    public IEnumerable<uint> GetGroupIdsWithPermission(Guid memberId, Permission permission)
    {
        var year = YearUtils.GetYearForDate(System.DateTime.UtcNow, YearUtils.CommitteeCreationDate);
        var permissionKey = permission.ToString();

        return db.GroupMemberships
            .Where(gm => gm.MemberId == memberId && gm.MembershipYear == year)
            .Where(gm =>
                db.GroupPermissions.Any(gp => gp.GroupId == gm.GroupId && gp.PermissionKey == permissionKey) ||
                (gm.RoleAliasId != null && db.RolePermissions.Any(rp => rp.RoleId == gm.RoleAlias!.RoleId && rp.PermissionKey == permissionKey)))
            .Select(gm => gm.GroupId)
            .Distinct()
            .ToList();
    }
}
