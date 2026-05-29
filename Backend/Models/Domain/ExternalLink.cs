using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Backend.Models.Domain;

/// <summary>
/// Represents an external link displayed in the navigation links page.
/// </summary>
[PrimaryKey(nameof(Id))]
public class ExternalLink
{
    /// <summary>
    /// The unique identifier of an ExternalLink, assigned incrementally.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The Dutch title for the external link.
    /// </summary>
    [StringLength(120)]
    [Required(AllowEmptyStrings = false)]
    public required string TitleDutch { get; set; }

    /// <summary>
    /// The English title for the external link.
    /// </summary>
    [StringLength(120)]
    [Required(AllowEmptyStrings = false)]
    public required string TitleEnglish { get; set; }

    /// <summary>
    /// The Dutch description for the external link.
    /// </summary>
    [StringLength(2000)]
    [Required(AllowEmptyStrings = false)]
    public required string DescriptionDutch { get; set; }

    /// <summary>
    /// The English description for the external link.
    /// </summary>
    [StringLength(2000)]
    [Required(AllowEmptyStrings = false)]
    public required string DescriptionEnglish { get; set; }

    /// <summary>
    /// The destination URL of the link.
    /// </summary>
    [StringLength(2000)]
    [Required(AllowEmptyStrings = false)]
    public required string Url { get; set; }

    /// <summary>
    /// The order in which this link should be displayed.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// The storage path of the icon associated with this link.
    /// </summary>
    public string? IconPath { get; set; }

    /// <summary>
    /// The original filename of the uploaded icon.
    /// </summary>
    public string? IconFileName { get; set; }
}
