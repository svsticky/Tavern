using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Backend.Models.Domain;

/// <summary>
/// Represents a legal document or agreement link that users must agree to before registering.
/// </summary>
[PrimaryKey(nameof(Id))]
public class RegistrationDocument
{
    /// <summary>
    /// The unique identifier of a RegistrationDocument, assigned incrementally.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The Dutch name/title of the document.
    /// </summary>
    [StringLength(120)]
    [Required(AllowEmptyStrings = false)]
    public required string NameDutch { get; set; }

    /// <summary>
    /// The English name/title of the document.
    /// </summary>
    [StringLength(120)]
    [Required(AllowEmptyStrings = false)]
    public required string NameEnglish { get; set; }

    /// <summary>
    /// The destination URL for the document.
    /// </summary>
    [StringLength(2000)]
    [Required(AllowEmptyStrings = false)]
    public required string Url { get; set; }

    /// <summary>
    /// The order in which this document should be displayed.
    /// </summary>
    public int SortOrder { get; set; }
}
