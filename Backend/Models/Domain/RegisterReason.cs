using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Backend.Models.Domain;

/// <summary>
/// Represents a reason for registration at the association.
/// </summary>
[PrimaryKey(nameof(Id))]
public class RegisterReason
{
    /// <summary>
    /// The unique identifier of a RegisterReason, assigned incrementally.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The Dutch title for the registration reason.
    /// </summary>
    [StringLength(120)]
    [Required(AllowEmptyStrings = false)]
    public required string TitleDutch { get; set; }

    /// <summary>
    /// The English title for the registration reason.
    /// </summary>
    [StringLength(120)]
    [Required(AllowEmptyStrings = false)]
    public required string TitleEnglish { get; set; }

    /// <summary>
    /// The Dutch description for the registration reason.
    /// </summary>
    [StringLength(2000)]
    [Required(AllowEmptyStrings = false)]
    public required string DescriptionDutch { get; set; }

    /// <summary>
    /// The English description for the registration reason.
    /// </summary>
    [StringLength(2000)]
    [Required(AllowEmptyStrings = false)]
    public required string DescriptionEnglish { get; set; }

    /// <summary>
    /// The order in which this reason should be displayed.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// The storage path of the icon associated with this reason.
    /// </summary>
    public string? IconPath { get; set; }

    /// <summary>
    /// The original filename of the uploaded icon.
    /// </summary>
    public string? IconFileName { get; set; }
}