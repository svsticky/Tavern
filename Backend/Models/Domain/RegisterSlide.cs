using Microsoft.EntityFrameworkCore;

namespace Backend.Models.Domain;

/// <summary>
/// Represents a slideshow image on the registration page.
/// </summary>
[PrimaryKey(nameof(Id))]
public class RegisterSlide
{
    /// <summary>
    /// The unique identifier of a register slide, assigned incrementally.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The order in which this slide should be displayed.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// The storage path of the slide image.
    /// </summary>
    public string? ImagePath { get; set; }

    /// <summary>
    /// The original filename of the uploaded slide image.
    /// </summary>
    public string? ImageFileName { get; set; }
}
