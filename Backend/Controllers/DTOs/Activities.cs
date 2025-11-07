using System.ComponentModel.DataAnnotations;

namespace Backend.Controllers.DTOs;

// ReSharper disable once InconsistentNaming => Allow DTO as an acronym
public class PostActivityDTO
{
    /// <inheritdoc cref="Models.Activity.Name"/>
    [StringLength(120)]
    public required string Name { get; set; }

    /// <inheritdoc cref="Models.Activity.Description"/>
    [StringLength(240)]
    public required string Description { get; set; } // TODO: this is of course not good for localisation

    /// <inheritdoc cref="Models.Activity.DateTimeStart"/>
    public required DateTimeOffset DateTimeStart { get; set; }
}
