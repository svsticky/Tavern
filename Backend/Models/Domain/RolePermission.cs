#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor => Will be saturated by EFCore
using Microsoft.EntityFrameworkCore;

namespace Backend.Models.Domain;

/// <summary>
/// Grants a permission to every member currently holding a Role, wherever they hold it. Global to the
/// Role - not scoped to any particular group. The permission key is either the string name of one of
/// the 12 known <see cref="Backend.Models.Permission"/> values, or an arbitrary custom string for other
/// applications sharing this Keycloak instance to interpret via the group_memberships claim - Tavern's
/// own backend only ever evaluates the 12 known ones.
/// </summary>
[PrimaryKey(nameof(Id))]
public class RolePermission
{
    /// <summary>
    /// The unique identifier of a role permission, assigned incrementally.
    /// </summary>
    public uint Id { get; set; }

    /// <summary>
    /// The id of the role this permission is granted to.
    /// </summary>
    public uint RoleId { get; set; }

    /// <summary>
    /// The role this permission is granted to.
    /// </summary>
    public Role Role { get; set; }

    /// <summary>
    /// The permission key granted to the role - a known Permission's name, or a custom string.
    /// </summary>
    public string PermissionKey { get; set; }
}
