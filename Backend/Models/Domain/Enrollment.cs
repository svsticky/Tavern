#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor => Will be saturated by EFCore

using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Models.Domain;

/// <summary>
/// Represents an enrollment of a member in an activity. An enrollment is associated with a specific activity and member, and it contains information about the price paid for the enrollment, any specification answers provided by the member, the date and time when the enrollment was placed, and whether the enrollment is on a waiting list. This entity is used to manage and track enrollments for activities within the system, allowing members to participate in activities based on their preferences and eligibility.
/// </summary>
[PrimaryKey(nameof(ActivityId), nameof(MemberId))]
public class Enrollment
{
    /// <summary>
    /// A list of allowed field paths that can be modified by standard update operations (such as
    /// JSON Patch or partial updates via the API). Identity fields (activity/member) and
    /// admin-managed bookkeeping fields (price, registration time, waiting-list status) are
    /// off-limits to everyone and must go through their own dedicated flows.
    /// </summary>
    public static readonly IReadOnlySet<string> AllowedFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "/specificationanswers"
    };

    /// <summary>
    /// Reference to the unique identifier of the activity which is enrolled for.
    /// </summary>
    public required uint ActivityId { get; set; }

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
    public virtual ICollection<SpecificationAnswer> SpecificationAnswers { get; set; }

    /// <summary>
    /// The date and time at which the enrollment was placed.
    /// </summary>
    public DateTime RegisteredOn { get; set; }

    /// <summary>
    /// If the enrollment is placed on a waiting list due to the associated activity being fully booked, this field indicates the position of the enrollment on the waiting list. 
    /// </summary>
    public bool IsOnWaitingList { get; set; }
}
