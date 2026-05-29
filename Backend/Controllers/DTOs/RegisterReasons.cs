using Backend.Models.Domain;
using System.ComponentModel.DataAnnotations;

namespace Backend.Controllers.DTOs;

/// <summary>
/// Defines the DTO for creating a register reason, containing localized text and ordering.
/// </summary>
public class PostRegisterReasonDTO
{
    /// <inheritdoc cref="RegisterReason.TitleDutch"/>
    [StringLength(120)]
    [Required(AllowEmptyStrings = false)]
    public required string TitleDutch { get; set; }

    /// <inheritdoc cref="RegisterReason.TitleEnglish"/>
    [StringLength(120)]
    [Required(AllowEmptyStrings = false)]
    public required string TitleEnglish { get; set; }

    /// <inheritdoc cref="RegisterReason.DescriptionDutch"/>
    [StringLength(2000)]
    [Required(AllowEmptyStrings = false)]
    public required string DescriptionDutch { get; set; }

    /// <inheritdoc cref="RegisterReason.DescriptionEnglish"/>
    [StringLength(2000)]
    [Required(AllowEmptyStrings = false)]
    public required string DescriptionEnglish { get; set; }

    /// <inheritdoc cref="RegisterReason.SortOrder"/>
    public int? SortOrder { get; set; }
}

/// <summary>
/// Defines the DTO for updating a register reason.
/// </summary>
public class RegisterReasonUpdateDTO
{
    /// <inheritdoc cref="RegisterReason.TitleDutch"/>
    [StringLength(120)]
    [Required(AllowEmptyStrings = false)]
    public required string TitleDutch { get; set; }

    /// <inheritdoc cref="RegisterReason.TitleEnglish"/>
    [StringLength(120)]
    [Required(AllowEmptyStrings = false)]
    public required string TitleEnglish { get; set; }

    /// <inheritdoc cref="RegisterReason.DescriptionDutch"/>
    [StringLength(2000)]
    [Required(AllowEmptyStrings = false)]
    public required string DescriptionDutch { get; set; }

    /// <inheritdoc cref="RegisterReason.DescriptionEnglish"/>
    [StringLength(2000)]
    [Required(AllowEmptyStrings = false)]
    public required string DescriptionEnglish { get; set; }

    /// <inheritdoc cref="RegisterReason.SortOrder"/>
    public required int SortOrder { get; set; }
}

/// <summary>
/// Represents the response DTO for a register reason.
/// </summary>
public class RegisterReasonResponseDTO
{
    /// <inheritdoc cref="RegisterReason.Id"/>
    public required int Id { get; set; }

    /// <inheritdoc cref="RegisterReason.TitleDutch"/>
    public required string TitleDutch { get; set; }

    /// <inheritdoc cref="RegisterReason.TitleEnglish"/>
    public required string TitleEnglish { get; set; }

    /// <inheritdoc cref="RegisterReason.DescriptionDutch"/>
    public required string DescriptionDutch { get; set; }

    /// <inheritdoc cref="RegisterReason.DescriptionEnglish"/>
    public required string DescriptionEnglish { get; set; }

    /// <inheritdoc cref="RegisterReason.SortOrder"/>
    public required int SortOrder { get; set; }

    /// <inheritdoc cref="RegisterReason.IconPath"/>
    public string? IconPath { get; set; }
}
