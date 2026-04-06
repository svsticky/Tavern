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

public class GetAnnouncementDTO
{
    public required uint Id { get; set; }
    public required string Title { get; set; }
    public required string Content { get; set; }
    public required string CreatedByName { get; set; }
    public required Guid CreatedById { get; set; }
    public required DateTimeOffset CreatedAt { get; set; }
}