#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor => Will be saturated by EFCore
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Backend.Models;

/// <summary>
/// A role alias for a member in a committee. E.g.: "Fotofeut" or "Man van Geld".
/// </summary>
[PrimaryKey(nameof(Id))]
public class RoleAlias
{
    /// <summary>
    /// The unique identifier of a role alias, assigned incrementally.
    /// </summary>
    public uint Id { get; set; }

    /// <summary>
    /// The id of the role that this alias belongs to.
    /// </summary>
    public uint RoleId { get; set; }

    /// <summary>
    /// The role that this alias belongs to.
    /// </summary>
    public Role Role { get; set; }

    /// <summary>
    /// The name of the role alias.
    /// </summary>
    [StringLength(100)]
    public string Name { get; set; }
}
