#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor => Will be saturated by EFCore
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Backend.Models;

/// <summary>
/// A role for a member in a committee. E.g.: "Chair" or "Treasurer".
/// </summary>
[PrimaryKey(nameof(Id))]
public class Role
{
    /// <summary>
    /// The unique identifier of a role, assigned incrementally.
    /// </summary>
    public uint Id { get; set; }

    /// <summary>
    /// The name of the role.
    /// </summary>
    [StringLength(100)]
    [Required(AllowEmptyStrings = false)]
    public string Name { get; set; }
}
