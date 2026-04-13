#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor => Will be saturated by EFCore
using Microsoft.EntityFrameworkCore;

namespace Backend.Models.Domain;

public enum StudyStatus
{
    Enrolled,
    Completed,
    DroppedOut
}

[PrimaryKey(nameof(Id))]
public class StudyEnrollment
{
    /// <summary>
    /// The unique identifier of a study enrollment, assigned incrementally.
    /// </summary>
    public uint Id { get; set; }

    /// <summary>
    /// The member associated with this enrollment.
    /// </summary>
    public Guid MemberId { get; set; }
    
    /// <summary>
    /// The member associated with this enrollment.
    /// </summary>
    public Member Member { get; set; }


    /// <summary>
    /// The study associated with this enrollment.
    /// </summary>
    public uint StudyId { get; set; }

    /// <summary>
    /// The study associated with this enrollment.
    /// </summary>
    public Study Study { get; set; }

    /// <summary>
    /// The date and time when the enrollment started.
    /// </summary>
    public DateTimeOffset EnrollmentDate { get; set; }

    /// <summary>
    /// The date and time when the enrollment ended, if applicable.
    /// </summary>
    public DateTimeOffset? CompletionDate { get; set; }

    /// <summary>
    /// The current status of the study enrollment.
    /// </summary>
    public StudyStatus Status { get; set; }
}
