#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor => Will be saturated by EFCore
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Backend.Models;

public enum GroupType
{
    Committee,
    WorkingGroup
}

[PrimaryKey(nameof(Id))]
public class Group
{
    /// <summary>
    /// The unique identifier of a Group, assigned incrementally.
    /// </summary>
    public uint Id { get; set; }

    /// <summary>
    /// The name of the Group.
    /// </summary>
    [StringLength(100)]
    public string Name { get; set; }

    /// <summary>
    /// Status of the group. Inactive groups are hidden from administrative views to prevent clutter, 
    /// but are preserved in the database for historical records and statistics (e.g., the Almanac).
    /// </summary>
    public bool Active { get; set; } = true;

    /// <summary>
    /// The members associated with this Group.
    /// </summary>
    public List<GroupMembership> GroupMemberships { get; set; } = new List<GroupMembership>();

    /// <summary>
    /// The type of the Group (E.G. Committee, Working Group).
    /// </summary>
    public GroupType Type { get; set; }
}
