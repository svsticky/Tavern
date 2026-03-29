using System.ComponentModel.DataAnnotations;

namespace Backend.Controllers.DTOs;

public class PostAnnouncementDTO
{
    /// <inheritdoc cref="Models.Announcement.Title"/>>
    [StringLength(100)]
    [Required(AllowEmptyStrings = false)]
    public required string Title { get; set; }

    /// <inheritdoc cref="Models.Announcement.Content"/>>
    [StringLength(1000)]
    [Required(AllowEmptyStrings = false)]
    public required string Content { get; set; }
}

public class UpdateAnnouncementDTO
{
    /// <inheritdoc cref="Models.Announcement.Title"/>
    [StringLength(100)]
    [Required(AllowEmptyStrings = false)]
    public required string Title { get; set; }

    /// <inheritdoc cref="Models.Announcement.Content"/>
    [StringLength(1000)]
    [Required(AllowEmptyStrings = false)]
    public required string Content { get; set; }
}