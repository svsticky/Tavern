using Backend.Controllers.DTOs;
using Backend.Models.Domain;
using System.Linq.Expressions;

namespace Backend.Projections;

/// <summary>
/// The AnnouncementProjections class provides a method to project an Announcement entity into a GetAnnouncementResponseDTO. This projection is used to transform the data from the Announcement model into a format that is suitable for API responses, including conditional logic to determine whether to include the creator's information based on the user's role. The ToDto method takes a user ID and a boolean indicating whether the requester is a board member, allowing it to conditionally include certain information based on the user's role. This class helps to centralize the logic for mapping Announcement entities to their corresponding DTOs, ensuring consistency and maintainability in the codebase when handling announcement-related data transformations for API responses.
/// </summary>
public static class AnnouncementProjections
{
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
            Title = a.Title,
            Content = a.Content,
            CreatedById = isBoard || a.CreatedById == userId ? a.CreatedById : null,
            CreatedAt = a.CreatedAt,
            CreatedByName = a.CreatedBy != null
                ? a.CreatedBy.FirstName + " " + a.CreatedBy.LastName
                : "Unknown"
        };
    }
}