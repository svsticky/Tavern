#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor => Will be saturated by EFCore
using Microsoft.EntityFrameworkCore;

namespace Backend.Models.Domain;

/// <summary>
/// Grants a permission to every member of a Group, regardless of their role within it. The
/// permission key is either the string name of one of the 12 known <see cref="Backend.Models.Permission"/>
/// values, or an arbitrary custom string for other applications sharing this Keycloak instance to
/// interpret via the group_memberships claim - Tavern's own backend only ever evaluates the 12 known ones.
/// </summary>
[PrimaryKey(nameof(Id))]
public class GroupPermission
{
    /// <summary>
    /// The unique identifier of a group permission, assigned incrementally.
    /// </summary>
    public uint Id { get; set; }

    /// <summary>
    /// The id of the group this permission is granted to.
    /// </summary>
    public uint GroupId { get; set; }

    /// <summary>
    /// The group this permission is granted to.
    /// </summary>
    public Group Group { get; set; }

    /// <summary>
    /// The permission key granted to the group - a known Permission's name, or a custom string.
    /// </summary>
    public string PermissionKey { get; set; }
}
