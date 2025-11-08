#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor => Will be saturated by EFCore
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Backend.Models;

[PrimaryKey(nameof(Id))]
public class Commission
{
    /// <summary>
    /// The unique identifier of a commission, assigned incrementally.
    /// </summary>
    public uint Id { get; set; }

    /// <summary>
    /// The name of the commission.
    /// </summary>
    [StringLength(100)]
    public string Name { get; set; }

    /// <summary>
    /// Indicates whether the commission is active.
    /// </summary>
    public bool Active { get; set; } = true;

    /// <summary>
    /// The members associated with this commission.
    /// </summary>
    public List<CommissionMembership> CommissionMemberships { get; set; } = new List<CommissionMembership>();
}
