#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor => Will be saturated by EFCore

using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Backend.Models;

[PrimaryKey(nameof(ActivityId), nameof(MemberId))]
public class Enrollment
{
    /// <summary>
    /// Reference to the unique identifier of the activity which is enrolled for.
    /// </summary>
    public uint ActivityId { get; set; }

    /// <summary>
    /// The activity which is enrolled for.
    /// </summary>
    public Activity Activity { get; set; }
    
    /// <summary>
    /// The ID of the user, as determined by the used OAuth application, which enrolls for the activity.
    /// </summary>
    public Guid MemberId { get; set; }

    /// <summary>
    /// The member who enrolled for the activity.
    /// </summary>
    public Member Member { get; set; }

    /// <summary>
    /// The price paid for the enrollment.
    /// </summary>
    [Column(TypeName = "numeric(18,2)")]
    public decimal Price { get; set; }

    /// <summary>
    /// The answers for the specification questions associated with this enrollment.
    /// </summary>
    [JsonIgnore] public virtual ICollection<SpecificationAnswer> SpecificationAnswers { get; set; }
}
