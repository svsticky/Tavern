using Backend.Controllers.DTOs;
using Backend.Models.Domain;
using Microsoft.AspNetCore.JsonPatch;

namespace Backend.Interfaces;

/// <summary>
/// Defines the contract for creating, reading, updating, and deleting announcements.
/// </summary>
public interface IAnnouncementService
{
    /// <summary>
    /// Retrieves announcements visible to the requesting user.
    /// </summary>
    /// <param name="userId">The ID of the requesting user.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The announcements matching the request context.</returns>
    Task<IEnumerable<GetAnnouncementResponseDTO>> GetAnnouncements(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a single announcement by ID.
    /// </summary>
    /// <param name="id">The announcement ID.</param>
    /// <param name="userId">The ID of the requesting user.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The announcement when found; otherwise <c>null</c>.</returns>
    Task<GetAnnouncementResponseDTO?> GetAnnouncement(uint id, Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a new announcement.
    /// </summary>
    /// <param name="userId">The ID of the user creating the announcement.</param>
    /// <param name="dto">The announcement payload.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The created announcement entity.</returns>
    Task<Announcement> CreateAnnouncement(Guid userId, PostAnnouncementDTO dto, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes an existing announcement.
    /// </summary>
    /// <param name="id">The announcement ID.</param>
    /// <param name="userId">The ID of the user deleting the announcement.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task DeleteAnnouncement(uint id, Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Applies a JSON Patch document to an announcement.
    /// </summary>
    /// <param name="id">The announcement ID.</param>
    /// <param name="patchDoc">The patch document to apply.</param>
    /// <param name="userId">The ID of the user updating the announcement.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task PatchAnnouncement(uint id, JsonPatchDocument<Announcement> patchDoc, Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Replaces an announcement with the provided values.
    /// </summary>
    /// <param name="id">The announcement ID.</param>
    /// <param name="dto">The replacement announcement payload.</param>
    /// <param name="userId">The ID of the user updating the announcement.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task UpdateAnnouncement(uint id, UpdateAnnouncementDTO dto, Guid userId, CancellationToken cancellationToken);
}
