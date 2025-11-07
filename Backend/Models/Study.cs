#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor => Will be saturated by EFCore
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Backend.Models;

public enum StudyType
{
    Bachelor,
    Master
}

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
    public string Title { get; set; }

    /// <summary>
    /// The default duration of the study in years.
    /// </summary>
    public uint DurationYears { get; set; }

    /// <summary>
    /// The type of the study (e.g., Bachelor, Master).
    /// </summary>
    public StudyType Type { get; set; }

    /// <summary>
    /// The enrollments associated with this study.
    /// </summary>
    public List<StudyEnrollment> Enrollments { get; set; }
}
