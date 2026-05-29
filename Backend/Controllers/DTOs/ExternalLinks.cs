using Backend.Models.Domain;
using System.ComponentModel.DataAnnotations;

namespace Backend.Controllers.DTOs;

/// <summary>
/// Defines the DTO for creating an external link.
/// </summary>
public class PostExternalLinkDTO
{
    /// <inheritdoc cref="ExternalLink.TitleDutch"/>
    [StringLength(120)]
    [Required(AllowEmptyStrings = false)]
    public required string TitleDutch { get; set; }

    /// <inheritdoc cref="ExternalLink.TitleEnglish"/>
    [StringLength(120)]
    [Required(AllowEmptyStrings = false)]
    public required string TitleEnglish { get; set; }

    /// <inheritdoc cref="ExternalLink.DescriptionDutch"/>
    [StringLength(2000)]
    [Required(AllowEmptyStrings = false)]
    public required string DescriptionDutch { get; set; }

    /// <inheritdoc cref="ExternalLink.DescriptionEnglish"/>
    [StringLength(2000)]
    [Required(AllowEmptyStrings = false)]
    public required string DescriptionEnglish { get; set; }

    /// <inheritdoc cref="ExternalLink.Url"/>
    [StringLength(2000)]
    [Required(AllowEmptyStrings = false)]
    public required string Url { get; set; }

    /// <inheritdoc cref="ExternalLink.SortOrder"/>
    public int? SortOrder { get; set; }
}

/// <summary>
/// Defines the DTO for updating an external link.
/// </summary>
public class ExternalLinkUpdateDTO
{
    /// <inheritdoc cref="ExternalLink.TitleDutch"/>
    [StringLength(120)]
    [Required(AllowEmptyStrings = false)]
    public required string TitleDutch { get; set; }

    /// <inheritdoc cref="ExternalLink.TitleEnglish"/>
    [StringLength(120)]
    [Required(AllowEmptyStrings = false)]
    public required string TitleEnglish { get; set; }

    /// <inheritdoc cref="ExternalLink.DescriptionDutch"/>
    [StringLength(2000)]
    [Required(AllowEmptyStrings = false)]
    public required string DescriptionDutch { get; set; }

    /// <inheritdoc cref="ExternalLink.DescriptionEnglish"/>
    [StringLength(2000)]
    [Required(AllowEmptyStrings = false)]
    public required string DescriptionEnglish { get; set; }

    /// <inheritdoc cref="ExternalLink.Url"/>
    [StringLength(2000)]
    [Required(AllowEmptyStrings = false)]
    public required string Url { get; set; }

    /// <inheritdoc cref="ExternalLink.SortOrder"/>
    public required int SortOrder { get; set; }
}

/// <summary>
/// Represents the response DTO for an external link.
/// </summary>
public class ExternalLinkResponseDTO
{
    /// <inheritdoc cref="ExternalLink.Id"/>
    public required int Id { get; set; }

    /// <inheritdoc cref="ExternalLink.TitleDutch"/>
    public required string TitleDutch { get; set; }

    /// <inheritdoc cref="ExternalLink.TitleEnglish"/>
    public required string TitleEnglish { get; set; }

    /// <inheritdoc cref="ExternalLink.DescriptionDutch"/>
    public required string DescriptionDutch { get; set; }

    /// <inheritdoc cref="ExternalLink.DescriptionEnglish"/>
    public required string DescriptionEnglish { get; set; }

    /// <inheritdoc cref="ExternalLink.Url"/>
    public required string Url { get; set; }

    /// <inheritdoc cref="ExternalLink.SortOrder"/>
    public required int SortOrder { get; set; }

    /// <inheritdoc cref="ExternalLink.IconPath"/>
    public string? IconPath { get; set; }
}
