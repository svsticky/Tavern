using System.ComponentModel.DataAnnotations;

namespace Backend.Controllers.DTOs;

// ReSharper disable once InconsistentNaming => Allow DTO as an acronym
public class PostActivityDTO
{
    /// <inheritdoc cref="Models.Activity.Name"/>
    [StringLength(120)]
    public required string Name { get; set; }

    /// <inheritdoc cref="Models.Activity.DutchDescription"/>
    [StringLength(240)]
    public required string DutchDescription { get; set; }

    /// <inheritdoc cref="Models.Activity.EnglishDescription"/>
    [StringLength(240)]
    public required string EnglishDescription { get; set; }

    /// <inheritdoc cref="Models.Activity.DateTimeStart"/>
    public required DateTimeOffset DateTimeStart { get; set; }

    /// <inheritdoc cref="Models.Activity.DateTimeEnd"/>
    public required DateTimeOffset DateTimeEnd { get; set; }
}

public class ActivityUpdateDTO
{
    /// <inheritdoc cref="Models.Activity.Name"/>
    [StringLength(120)]
    public required string Name { get; set; }

    /// <inheritdoc cref="Models.Activity.DutchDescription"/>
    [StringLength(240)]
    public required string DutchDescription { get; set; }

    /// <inheritdoc cref="Models.Activity.EnglishDescription"/>
    [StringLength(240)]
    public required string EnglishDescription { get; set; }

    /// <inheritdoc cref="Models.Activity.DateTimeStart"/>
    public required DateTimeOffset DateTimeStart { get; set; }

    /// <inheritdoc cref="Models.Activity.DateTimeEnd"/>
    public required DateTimeOffset DateTimeEnd { get; set; }
}

public class ActivityResponseDTO
{
    /// <inheritdoc cref="Models.Activity.Id"/>
    public uint Id { get; set; }

    /// <inheritdoc cref="Models.Activity.Name"/>
    public required string Name { get; set; }

    /// <inheritdoc cref="Models.Activity.DutchDescription"/>
    public required string DutchDescription { get; set; }

    /// <inheritdoc cref="Models.Activity.EnglishDescription"/>
    public required string EnglishDescription { get; set; }

    /// <inheritdoc cref="Models.Activity.DateTimeStart"/>
    public DateTimeOffset DateTimeStart { get; set; }

    /// <inheritdoc cref="Models.Activity.DateTimeEnd"/>
    public DateTimeOffset DateTimeEnd { get; set; }
}