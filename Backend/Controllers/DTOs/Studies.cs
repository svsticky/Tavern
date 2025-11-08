using System.ComponentModel.DataAnnotations;

using Backend.Models;

namespace Backend.Controllers.DTOs;

// ReSharper disable once InconsistentNaming => Allow DTO as an acronym
public class PostStudyDTO
{
    /// <inheritdoc cref="Models.Study.Title"/>
    [StringLength(100)]
    public required string Title { get; set; }

    /// <inheritdoc cref="Models.Study.DurationYears"/>
    public required uint DurationYears { get; set; }

    /// <inheritdoc cref="Models.Study.Type"/>
    public required StudyType Type { get; set; }
}

public class StudyUpdateDTO
{
    /// <inheritdoc cref="Models.Study.Title"/>
    [StringLength(100)]
    public required string Title { get; set; }

    /// <inheritdoc cref="Models.Study.DurationYears"/>
    public required uint DurationYears { get; set; }

    /// <inheritdoc cref="Models.Study.Type"/>
    public required StudyType Type { get; set; }
}
