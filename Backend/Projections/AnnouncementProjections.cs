using Backend.Controllers.DTOs;
using Backend.Models.Domain;
using System.Linq.Expressions;

namespace Backend.Projections;

public static class AnnouncementProjections
{
    public static Expression<Func<Announcement, GetAnnouncementResponseDTO>> ToDto()
    {
        return a => new GetAnnouncementResponseDTO
        {
            Id = a.Id,
            Title = a.Title,
            Content = a.Content,
            CreatedById = a.CreatedById,
            CreatedAt = a.CreatedAt,
            CreatedByName = a.CreatedBy != null
                ? a.CreatedBy.FirstName + " " + a.CreatedBy.LastName
                : "Unknown"
        };
    }
}