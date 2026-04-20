using Backend.Controllers.DTOs;
using Backend.Models.Domain;
using Microsoft.AspNetCore.JsonPatch;

namespace Backend.Interfaces;

public interface IAnnouncementService
{
    Task<IEnumerable<GetAnnouncementResponseDTO>> GetAnnouncements(Guid userId, CancellationToken cancellationToken);

    Task<GetAnnouncementResponseDTO?> GetAnnouncement(uint id, Guid userId, CancellationToken cancellationToken);

    Task<Announcement> CreateAnnouncement(Guid userId, PostAnnouncementDTO dto, CancellationToken cancellationToken);

    Task DeleteAnnouncement(uint id, Guid userId, CancellationToken cancellationToken);

    Task PatchAnnouncement(uint id, JsonPatchDocument<Announcement> patchDoc, Guid userId, CancellationToken cancellationToken);

    Task UpdateAnnouncement(uint id, UpdateAnnouncementDTO dto, Guid userId, CancellationToken cancellationToken);
}