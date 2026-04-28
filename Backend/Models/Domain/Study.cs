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
    Bachelor,
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
    /// The enrollments associated with this study.
    /// </summary>
    [JsonIgnore] public virtual ICollection<StudyEnrollment> Enrollments { get; set; }
}
