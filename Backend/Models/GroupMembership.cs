#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor => Will be saturated by EFCore
using Microsoft.EntityFrameworkCore;

namespace Backend.Models;

[PrimaryKey(nameof(Id))]
public class GroupMembership
{
    /// <summary>
    /// The unique identifier of a group membership, assigned incrementally.
    /// </summary>
    public uint Id { get; set; }

    /// <summary>
    /// The member associated with this membership.
    /// </summary>
    public Guid MemberId { get; set; }
    
    /// <summary>
    /// The member associated with this membership.
    /// </summary>
    public Member Member { get; set; }


    /// <summary>
    /// The group associated with this membership.
    /// </summary>
    public uint GroupId { get; set; }

    /// <summary>
    /// The group associated with this membership.
    /// </summary>
    public Group Group { get; set; }

    /// <summary>
    /// The year of the membership.
    /// </summary>
    public uint MembershipYear { get; set; }

    /// <summary>
    /// The role of the member in this group membership.
    /// </summary>
    public uint? RoleAliasId { get; set; } = null;

    /// <summary>
    /// The role of the member in this group membership.
    /// </summary>
    public RoleAlias? RoleAlias { get; set; } = null;
}
