using Backend.Controllers.DTOs;
using Backend.Models.Domain;

namespace Backend.Interfaces;

/// <summary>
/// Defines the contract for managing external links and their icons.
/// </summary>
public interface IExternalLinkRepository
{
    /// <summary>
    /// Retrieves all external links.
    /// </summary>
    Task<IEnumerable<ExternalLinkResponseDTO>> GetExternalLinks(CancellationToken ct);

    /// <summary>
    /// Retrieves an external link by ID.
    /// </summary>
    Task<ExternalLinkResponseDTO?> GetExternalLink(int id, CancellationToken ct);

    /// <summary>
    /// Creates a new external link.
    /// </summary>
    Task<ExternalLinkResponseDTO> CreateExternalLink(PostExternalLinkDTO dto, Guid userId, CancellationToken ct);

    /// <summary>
    /// Updates an external link.
    /// </summary>
    Task UpdateExternalLink(int id, ExternalLinkUpdateDTO dto, Guid userId, CancellationToken ct);

    /// <summary>
    /// Deletes an external link.
    /// </summary>
    Task DeleteExternalLink(int id, Guid userId, CancellationToken ct);

    /// <summary>
    /// Uploads or clears the icon for an external link.
    /// </summary>
    Task<string?> UploadExternalLinkIcon(int id, Guid userId, IFormFile? icon);

    /// <summary>
    /// Retrieves an icon file by storage path.
    /// </summary>
    Task<FileResultDto?> GetExternalLinkIconFile(string path);
}
