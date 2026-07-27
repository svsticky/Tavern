using System.ComponentModel.DataAnnotations;

namespace Backend.Models.Domain;

/// <summary>
/// Represents an announcement that can be created by members. An announcement has a title, content, and information about when it was created and who created it. This entity is used to manage and display announcements within the system, allowing members to stay informed about important updates, events, or news related to the organization or community.
/// </summary>
public class Announcement
{
    /// <summary>
    /// The unique identifier of the announcement, assigned incrementally.
    /// </summary>
    public uint Id { get; set; }

    /// <summary>
    /// The Dutch title of the announcement.
    /// </summary>
    [StringLength(100)]
    [Required(AllowEmptyStrings = false)]
    public string TitleDutch { get; set; } = null!;

    /// <summary>
    /// The English title of the announcement.
    /// </summary>
    [StringLength(100)]
    [Required(AllowEmptyStrings = false)]
    public string TitleEnglish { get; set; } = null!;

    /// <summary>
    /// The Dutch content of the announcement.
    /// </summary>
    [StringLength(1000)]
    [Required(AllowEmptyStrings = false)]
    public string ContentDutch { get; set; } = null!;

    /// <summary>
    /// The English content of the announcement.
    /// </summary>
    [StringLength(1000)]
    [Required(AllowEmptyStrings = false)]
    public string ContentEnglish { get; set; } = null!;

    /// <summary>
    /// The date and time when the announcement was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// The member id who created the announcement.
    /// </summary>
    public Guid CreatedById { get; set; }

    /// <summary>
    /// The member who created the announcement.
    /// </summary>
    public Member CreatedBy { get; set; } = null!;
}