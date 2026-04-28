using System.ComponentModel.DataAnnotations;

namespace Backend.Controllers.DTOs;

/// <summary>
/// Defines Data Transfer Object (DTO) for posting announcements, containing the necessary information for creating a new announcement, including its title and content. The PostAnnouncementDTO is used to transfer data from the client to the server when creating a new announcement, ensuring that all required information is provided and validated appropriately for the creation process.
/// </summary>
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

/// <summary>
/// Defines the DTO for updating an existing announcement, containing all necessary information for modifying an announcement's properties. The UpdateAnnouncementDTO is used to transfer data from the client to the server when updating an existing announcement, allowing for changes to be made to the announcement's details while ensuring that the provided information is validated appropriately for the update process.
/// </summary>
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

/// <summary>
/// Represents the response DTO for an announcement, containing all relevant information about the announcement, including its properties and information about the creator. The GetAnnouncementResponseDTO is used to transfer comprehensive announcement data from the server to the client when retrieving announcement information, allowing for a complete representation of the announcement's details, creator information, and creation timestamp in the response payload.
/// </summary>
public class GetAnnouncementResponseDTO
{
    /// <inheritdoc cref="Models.Announcement.Id"/>
    public required uint Id { get; set; }

    /// <inheritdoc cref="Models.Announcement.Title"/>
    public required string Title { get; set; }

    /// <inheritdoc cref="Models.Announcement.Content"/>
    public required string Content { get; set; }

    /// <inheritdoc cref="Models.Announcement.CreatedByName"/>
    public required string CreatedByName { get; set; }

    /// <inheritdoc cref="Models.Announcement.CreatedById"/>
    public Guid? CreatedById { get; set; }

    /// <inheritdoc cref="Models.Announcement.CreatedAt"/>
    public required DateTimeOffset CreatedAt { get; set; }
}