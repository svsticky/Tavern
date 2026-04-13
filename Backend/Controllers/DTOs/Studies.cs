using System.ComponentModel.DataAnnotations;

using Backend.Models.Domain;

namespace Backend.Controllers.DTOs;

// ReSharper disable once InconsistentNaming => Allow DTO as an acronym
public class PostStudyDTO
{
    /// <inheritdoc cref="Models.Study.Title"/>
    [StringLength(100)]
    [Required(AllowEmptyStrings = false)]
    public required string Title { get; set; }

    /// <inheritdoc cref="Models.Study.NominalDurationYears"/>
    public required uint NominalDurationYears { get; set; }

    /// <inheritdoc cref="Models.Study.Type"/>
    public required StudyType Type { get; set; }
}

public class StudyUpdateDTO
{
    /// <inheritdoc cref="Models.Study.Title"/>
    [StringLength(100)]
    [Required(AllowEmptyStrings = false)]
    public required string Title { get; set; }

    /// <inheritdoc cref="Models.Study.NominalDurationYears"/>
    public required uint NominalDurationYears { get; set; }

    /// <inheritdoc cref="Models.Study.Type"/>
    public required StudyType Type { get; set; }
}
