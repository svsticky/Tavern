using Backend.Models.Domain;
using System.Linq.Expressions;
using System.ComponentModel.DataAnnotations;

namespace Backend.Controllers.DTOs;

/// <summary>
/// Defines Data Transfer Object (DTO) for posting announcements, containing the necessary information for creating a new announcement, including its title and content. The PostAnnouncementDTO is used to transfer data from the client to the server when creating a new announcement, ensuring that all required information is provided and validated appropriately for the creation process.
/// </summary>
public class PostAnnouncementDTO
{
    /// <inheritdoc cref="Models.Domain.Announcement.TitleDutch"/>>
    [StringLength(100)]
    [Required(AllowEmptyStrings = false)]
    public required string TitleDutch { get; set; }

    /// <inheritdoc cref="Models.Domain.Announcement.TitleEnglish"/>>
    [StringLength(100)]
    [Required(AllowEmptyStrings = false)]
    public required string TitleEnglish { get; set; }

    /// <inheritdoc cref="Models.Domain.Announcement.ContentDutch"/>>
    [StringLength(10000)]
    [Required(AllowEmptyStrings = false)]
    public required string ContentDutch { get; set; }

    /// <inheritdoc cref="Models.Domain.Announcement.ContentEnglish"/>>
    [StringLength(10000)]
    [Required(AllowEmptyStrings = false)]
    public required string ContentEnglish { get; set; }
}

/// <summary>
/// Defines the DTO for updating an existing announcement, containing all necessary information for modifying an announcement's properties. The UpdateAnnouncementDTO is used to transfer data from the client to the server when updating an existing announcement, allowing for changes to be made to the announcement's details while ensuring that the provided information is validated appropriately for the update process.
/// </summary>
public class UpdateAnnouncementDTO
{
    /// <inheritdoc cref="Models.Domain.Announcement.TitleDutch"/>
    [StringLength(100)]
    [Required(AllowEmptyStrings = false)]
    public required string TitleDutch { get; set; }

    /// <inheritdoc cref="Models.Domain.Announcement.TitleEnglish"/>
    [StringLength(100)]
    [Required(AllowEmptyStrings = false)]
    public required string TitleEnglish { get; set; }

    /// <inheritdoc cref="Models.Domain.Announcement.ContentDutch"/>
    [StringLength(10000)]
    [Required(AllowEmptyStrings = false)]
    public required string ContentDutch { get; set; }

    /// <inheritdoc cref="Models.Domain.Announcement.ContentEnglish"/>
    [StringLength(10000)]
    [Required(AllowEmptyStrings = false)]
    public required string ContentEnglish { get; set; }
}

/// <summary>
/// Represents the response DTO for an announcement, containing all relevant information about the announcement, including its properties and information about the creator. The GetAnnouncementResponseDTO is used to transfer comprehensive announcement data from the server to the client when retrieving announcement information, allowing for a complete representation of the announcement's details, creator information, and creation timestamp in the response payload.
/// </summary>
public class GetAnnouncementResponseDTO
{
    /// <inheritdoc cref="Models.Domain.Announcement.Id"/>
    public required uint Id { get; set; }

    /// <inheritdoc cref="Models.Domain.Announcement.TitleDutch"/>
    public required string TitleDutch { get; set; }

    /// <inheritdoc cref="Models.Domain.Announcement.TitleEnglish"/>
    public required string TitleEnglish { get; set; }

    /// <inheritdoc cref="Models.Domain.Announcement.ContentDutch"/>
    public required string ContentDutch { get; set; }

    /// <inheritdoc cref="Models.Domain.Announcement.ContentEnglish"/>
    public required string ContentEnglish { get; set; }

    /// <summary>
    /// The name of the user who created the announcement, providing information about the creator of the announcement for display purposes in the client application. This field allows for better identification and attribution of announcements to their respective creators, enhancing the user experience by providing context about the source of the announcement in the system.
    /// </summary>
    public required string CreatedByName { get; set; }

    /// <inheritdoc cref="Models.Domain.Announcement.CreatedById"/>
    public Guid? CreatedById { get; set; }

    /// <inheritdoc cref="Models.Domain.Announcement.CreatedAt"/>
    public required DateTimeOffset CreatedAt { get; set; }

    /// <summary>
        /// Projects an Announcement entity into a GetAnnouncementResponseDTO, including conditional logic to determine whether to include the creator's information based on the user's role. The method takes a user ID and a boolean indicating whether the requester is a board member, allowing it to conditionally include certain information based on the user's role. This projection is used to transform the data from the Announcement model into a format that is suitable for API responses, ensuring that the relevant information is included while maintaining appropriate access control based on the user's role within the system.
        /// </summary>
        /// <param name="userId">The ID of the user for whom to project the announcement.</param>
        /// <param name="isBoard">A boolean indicating whether the requester is a board member.</param>
        /// <returns>An expression that projects an Announcement entity into a GetAnnouncementResponseDTO.</returns>
        public static Expression<Func<Announcement, GetAnnouncementResponseDTO>> ToDto(Guid userId, bool isBoard)
        {
            return a => new GetAnnouncementResponseDTO
            {
                Id = a.Id,
                TitleDutch = a.TitleDutch,
                TitleEnglish = a.TitleEnglish,
                ContentDutch = a.ContentDutch,
                ContentEnglish = a.ContentEnglish,
                CreatedById = isBoard || a.CreatedById == userId ? a.CreatedById : null,
                CreatedAt = a.CreatedAt,
                CreatedByName = a.CreatedBy != null
                    ? a.CreatedBy.FirstName + " " + a.CreatedBy.LastName
                    : "Unknown"
            };
        }
}