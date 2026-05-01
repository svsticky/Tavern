#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor => Will be saturated by EFCore
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace Backend.Models.Domain;

/// <summary>
/// Defines the type of a Group.
/// </summary>
public enum GroupType
{
    Committee,
    WorkingGroup,
    Dispute
}

/// <summary>
/// Represents a Group within the organization. A Group can be a Committee, Working Group, or Dispute group. Each Group has a unique identifier, a name, an active status, and can have multiple members associated with it through GroupMemberships. The Group entity also includes properties for the type of group and default financial information such as GL account and cost center. This entity is used to manage and organize different groups within the system, allowing for better collaboration and communication among members.
/// </summary>
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
    [Required(AllowEmptyStrings = false)]
    public string Name { get; set; }

    /// <summary>
    /// Status of the group. Inactive groups are hidden from administrative views to prevent clutter, 
    /// but are preserved in the database for historical records and statistics (e.g., the Almanac).
    /// </summary>
    public bool Active { get; set; } = true;

    /// <summary>
    /// The members associated with this Group.
    /// </summary>
    [JsonIgnore] public virtual ICollection<GroupMembership> GroupMemberships { get; set; }

    /// <summary>
    /// The type of the Group (E.G. Committee, Working Group).
    /// </summary>
    public GroupType Type { get; set; }

    /// <summary>
    /// The default GL account for the group, used for financial transactions.
    /// </summary>
    [StringLength(20)]
    public string? DefaultGLAccount { get; set; }

    /// <summary>
    /// The default cost center for the group, used for financial transactions.
    /// </summary>
    [StringLength(20)]
    public string? DefaultCostCenter { get; set; }

    /// <summary>
    /// The path where the picture for the group is stored, if any.
    /// </summary>
    public string? GroupPicturePath { get; set; }

    /// <summary>
    /// The filename of the picture for the group, if any.
    /// </summary>
    public string? GroupPictureFileName { get; set; }
}