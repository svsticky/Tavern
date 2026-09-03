#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor => Will be saturated by EFCore
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace Backend.Models.Domain;

/// <summary>
/// Defines the type of a Study.
/// </summary>
public enum StudyType
{
    /// <summary>
    /// Indicates that the study is a Bachelor's program, which typically has a nominal duration of three to four years and is designed to provide students with foundational knowledge and skills in their chosen field of study. Bachelor's programs often serve as a stepping stone for further education or entry into the workforce, offering a broad range of academic disciplines and opportunities for specialization.
    /// </summary>
    Bachelor,

    /// <summary>
    /// Indicates that the study is a Master's program, which typically has a nominal duration of one to two years and is designed for students who have already completed a Bachelor's degree. Master's programs often provide more advanced and specialized knowledge in a particular field of study, allowing students to deepen their expertise and prepare for careers that require higher levels of education or research opportunities.
    /// </summary>
    Master
}

/// <summary>
/// Represents a study program. A Study has a unique identifier, a title, a nominal duration in years, and a type (e.g., Bachelor, Master). Each Study can have multiple enrollments associated with it through the StudyEnrollment entity. This entity is used to manage and organize different study programs within the system, allowing for better tracking of student enrollments and academic programs offered by the organization.
/// </summary>
[PrimaryKey(nameof(Id))]
public class Study
{
    /// <summary>
    /// The unique identifier of a study, assigned incrementally.
    /// </summary>
    public uint Id { get; set; }

    /// <summary>
    /// The title of the study.
    /// </summary>
    [StringLength(100)]
    [Required(AllowEmptyStrings = false)]
    public string Title { get; set; }

    /// <summary>
    /// The default duration of the study in years.
    /// </summary>
    public uint NominalDurationYears { get; set; }

    /// <summary>
    /// The type of the study (e.g., Bachelor, Master).
    /// </summary>
    public StudyType Type { get; set; }

    /// <summary>
    /// Status of the study. Inactive studies are hidden from the public registration form,
    /// but are preserved in the database for historical records and statistics.
    /// </summary>
    public bool Active { get; set; } = true;

    /// <summary>
    /// The enrollments associated with this study.
    /// </summary>
    [JsonIgnore] public virtual ICollection<StudyEnrollment> Enrollments { get; set; }
}
