#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor => Will be saturated by EFCore
using Microsoft.EntityFrameworkCore;

namespace Backend.Models;

[PrimaryKey(nameof(Id))]
public class CommissionMembership
{
    /// <summary>
    /// The unique identifier of a commission membership, assigned incrementally.
    /// </summary>
    public uint Id { get; set; }

    /// <summary>
    /// The member associated with this membership.
    /// </summary>
    public uint MemberId { get; set; }
    
    /// <summary>
    /// The member associated with this membership.
    /// </summary>
    public Member Member { get; set; }


    /// <summary>
    /// The commission associated with this membership.
    /// </summary>
    public uint CommissionId { get; set; }

    /// <summary>
    /// The commission associated with this membership.
    /// </summary>
    public Commission Commission { get; set; }

    /// <summary>
    /// The year of the membership.
    /// </summary>
    public uint MembershipYear { get; set; }
}
