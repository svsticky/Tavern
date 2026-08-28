using Backend.Models;
using Backend.Models.Domain;

namespace Backend.Interfaces;

/// <summary>
/// Defines permission and authorization checks used across Tavern services.
/// </summary>
public interface IPermissionService
{
    /// <summary>
    /// Checks whether a member belongs to a group in the current year.
    /// </summary>
    /// <param name="memberId">The member user ID.</param>
    /// <param name="groupId">The group ID.</param>
    /// <returns><c>true</c> when the member is in the group; otherwise <c>false</c>.</returns>
    bool IsInGroupInCurrentYear(Guid memberId, uint groupId);

    /// <summary>
    /// Checks whether a member belongs to a group in the current year.
    /// </summary>
    /// <param name="member">The member.</param>
    /// <param name="groupId">The group ID.</param>
    /// <returns><c>true</c> when the member is in the group; otherwise <c>false</c>.</returns>
    bool IsInGroupInCurrentYear(Member member, uint groupId);

    /// <summary>
    /// Checks whether a member belongs to a group in a specific year.
    /// </summary>
    /// <param name="memberId">The member user ID.</param>
    /// <param name="groupId">The group ID.</param>
    /// <param name="year">The year to evaluate.</param>
    /// <returns><c>true</c> when the member is in the group; otherwise <c>false</c>.</returns>
    bool IsInGroup(Guid memberId, uint groupId, uint year);

    /// <summary>
    /// Checks whether a member belongs to a group in a specific year.
    /// </summary>
    /// <param name="member">The member.</param>
    /// <param name="groupId">The group ID.</param>
    /// <param name="year">The year to evaluate.</param>
    /// <returns><c>true</c> when the member is in the group; otherwise <c>false</c>.</returns>
    bool IsInGroup(Member member, uint groupId, uint year);

    /// <summary>
    /// Checks whether a member has a role in the current year.
    /// </summary>
    /// <param name="memberId">The member user ID.</param>
    /// <param name="roleId">The role ID.</param>
    /// <param name="groupId">An optional group constraint.</param>
    /// <returns><c>true</c> when the member has the role; otherwise <c>false</c>.</returns>
    bool IsInRoleInCurrentYear(Guid memberId, uint roleId, uint? groupId = null);

    /// <summary>
    /// Checks whether a member has a role in the current year.
    /// </summary>
    /// <param name="member">The member.</param>
    /// <param name="roleId">The role ID.</param>
    /// <param name="groupId">An optional group constraint.</param>
    /// <returns><c>true</c> when the member has the role; otherwise <c>false</c>.</returns>
    bool IsInRoleInCurrentYear(Member member, uint roleId, uint? groupId = null);

    /// <summary>
    /// Checks whether a member has a role in a specific year.
    /// </summary>
    /// <param name="memberId">The member user ID.</param>
    /// <param name="roleId">The role ID.</param>
    /// <param name="year">The year to evaluate.</param>
    /// <param name="groupId">An optional group constraint.</param>
    /// <returns><c>true</c> when the member has the role; otherwise <c>false</c>.</returns>
    bool IsInRole(Guid memberId, uint roleId, uint year, uint? groupId = null);

    /// <summary>
    /// Checks whether a member has a role in a specific year.
    /// </summary>
    /// <param name="member">The member.</param>
    /// <param name="roleId">The role ID.</param>
    /// <param name="year">The year to evaluate.</param>
    /// <param name="groupId">An optional group constraint.</param>
    /// <returns><c>true</c> when the member has the role; otherwise <c>false</c>.</returns>
    bool IsInRole(Member member, uint roleId, uint year, uint? groupId = null);

    /// <summary>
    /// Checks whether a member is in the board group in the current board year.
    /// </summary>
    bool IsBoardMember(Guid memberId);

    /// <summary>
    /// Ensures the user is a board member and throws when not authorized.
    /// </summary>
    void EnsureBoardMember(Guid userId);

    /// <summary>
    /// Checks whether a member is in the board or candidate board.
    /// </summary>
    /// <param name="memberId">The member user ID.</param>
    /// <returns><c>true</c> when the member is in board or candidate board; otherwise <c>false</c>.</returns>
    bool IsBoardOrCandidateBoardMember(Guid memberId);

    /// <summary>
    /// Ensures the user is in the board or candidate board and throws when not authorized.
    /// </summary>
    /// <param name="userId">The user ID to authorize.</param>
    public void EnsureBoardOrCandidateBoardMember(Guid userId);

    /// <summary>
    /// Checks whether a member has a given permission, either granted directly to a Group they belong to in
    /// the current committee year, or granted to the Role they hold within such a membership. Does not
    /// consider board/candidate-board status - see <see cref="HasPermissionOrBoard"/> for that.
    /// </summary>
    /// <param name="memberId">The member user ID.</param>
    /// <param name="permission">The permission to check.</param>
    /// <param name="groupId">
    /// For the group-scoped <see cref="Permission.EditActivityForGroup"/>, restricts the check to this group.
    /// Ignored for every other (global-effect) permission.
    /// </param>
    bool HasPermission(Guid memberId, Permission permission, uint? groupId = null);

    /// <summary>
    /// Checks whether a member has a given permission, or is a (candidate) board member (who always has
    /// every permission).
    /// </summary>
    /// <param name="memberId">The member user ID.</param>
    /// <param name="permission">The permission to check.</param>
    /// <param name="groupId">See <see cref="HasPermission"/>.</param>
    bool HasPermissionOrBoard(Guid memberId, Permission permission, uint? groupId = null);

    /// <summary>
    /// Ensures the user has a given permission (or is a (candidate) board member) and throws when not authorized.
    /// </summary>
    /// <param name="userId">The user ID to authorize.</param>
    /// <param name="permission">The permission to require.</param>
    /// <param name="groupId">See <see cref="HasPermission"/>.</param>
    void EnsurePermission(Guid userId, Permission permission, uint? groupId = null);

    /// <summary>
    /// Gets the IDs of every group in which the member currently holds the given permission (via a direct
    /// group grant or via their role in that group), in the current committee year.
    /// </summary>
    /// <param name="memberId">The member user ID.</param>
    /// <param name="permission">The permission to check.</param>
    IEnumerable<uint> GetGroupIdsWithPermission(Guid memberId, Permission permission);
}
