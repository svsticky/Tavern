#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor => Will be saturated by EFCore
using Microsoft.EntityFrameworkCore;

namespace Backend.Models.Domain;

/// <summary>
/// Defines the status of a study enrollment, indicating whether the enrollment is currently active (Enrolled), has been completed (Completed), or has been dropped out (DroppedOut). The StudyStatus enum is used to track the current state of a study enrollment, allowing for better management and organization of student enrollments within the system. This status information can be crucial for various functionalities such as reporting, analytics, and determining eligibility for certain activities or programs based on the student's enrollment status in their study program.
/// </summary>
public enum StudyStatus
{
    /// <summary>
    /// Indicates that the study enrollment is currently active, meaning that the member is currently enrolled in the study program and has not yet completed or dropped out. This status is used to track ongoing enrollments and can be important for determining eligibility for certain activities or programs that require active enrollment in a study program.
    /// </summary>
    Enrolled,

    /// <summary>
    /// Indicates that the study enrollment has been completed, meaning that the member has successfully finished the study program. This status is used to track completed enrollments and can be important for reporting and analytics purposes, as well as for determining eligibility for certain activities or programs that may require completion of a study program.
    /// </summary>
    Completed,

    /// <summary>
    /// Indicates that the study enrollment has been dropped out, meaning that the member has discontinued their enrollment in the study program before completion. This status is used to track dropped-out enrollments and can be important for reporting and analytics purposes, as well as for determining eligibility for certain activities or programs that may require active enrollment or completion of a study program.
    /// </summary>
    DroppedOut
}

/// <summary>
/// Represents a study enrollment of a member in a study program. A StudyEnrollment has a unique identifier, references to the associated Member and Study, the date and time when the enrollment started, an optional completion date, and the current status of the enrollment (e.g., Enrolled, Completed, DroppedOut). This entity is used to manage and track enrollments for study programs within the system, allowing members to participate in academic programs based on their preferences and eligibility, and enabling better organization and reporting of student enrollments and academic progress.
/// </summary>
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
