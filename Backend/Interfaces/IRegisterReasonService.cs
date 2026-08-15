using Backend.Controllers.DTOs;

namespace Backend.Interfaces;

/// <summary>
/// Defines the contract for managing register reasons and their icons.
/// </summary>
public interface IRegisterReasonService
{
    /// <summary>
    /// Retrieves all register reasons.
    /// </summary>
    Task<IEnumerable<RegisterReasonResponseDTO>> GetRegisterReasons(CancellationToken ct);

    /// <summary>
    /// Retrieves a register reason by ID.
    /// </summary>
    Task<RegisterReasonResponseDTO?> GetRegisterReason(int id, CancellationToken ct);

    /// <summary>
    /// Creates a new register reason.
    /// </summary>
    Task<RegisterReasonResponseDTO> CreateRegisterReason(PostRegisterReasonDTO dto, Guid userId, CancellationToken ct);

    /// <summary>
    /// Updates a register reason.
    /// </summary>
    Task UpdateRegisterReason(int id, RegisterReasonUpdateDTO dto, Guid userId, CancellationToken ct);

    /// <summary>
    /// Deletes a register reason.
    /// </summary>
    Task DeleteRegisterReason(int id, Guid userId, CancellationToken ct);

    /// <summary>
    /// Uploads or clears the icon for a register reason.
    /// </summary>
    Task<string?> UploadRegisterReasonIcon(int id, Guid userId, IFormFile? icon);

    /// <summary>
    /// Retrieves an icon file by storage path.
    /// </summary>
    Task<FileResultDto?> GetRegisterReasonIconFile(string path);
}
