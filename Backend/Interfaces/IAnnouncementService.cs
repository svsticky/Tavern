using Backend.Controllers.DTOs;
using Backend.Models;
using Microsoft.AspNetCore.JsonPatch;

namespace Backend.Interfaces;

public interface IAnnouncementService
{
    Task<IEnumerable<GetAnnouncementDTO>> GetAnnouncements(CancellationToken cancellationToken);

    Task<Announcement?> GetAnnouncement(uint id, CancellationToken cancellationToken);

    Task<Announcement> CreateAnnouncement(Guid userId, PostAnnouncementDTO dto, CancellationToken cancellationToken);

    Task DeleteAnnouncement(uint id, CancellationToken cancellationToken);

    Task PatchAnnouncement(uint id, JsonPatchDocument<Announcement> patchDoc, CancellationToken cancellationToken);

    Task UpdateAnnouncement(uint id, UpdateAnnouncementDTO dto, CancellationToken cancellationToken);
}