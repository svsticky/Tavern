using Backend.Controllers.DTOs;

namespace Backend.Interfaces;

/// <summary>
/// Defines the contract for managing register slides.
/// </summary>
public interface IRegisterSlideService
{
    /// <summary>
    /// Retrieves all register slides.
    /// </summary>
    Task<IEnumerable<RegisterSlideResponseDTO>> GetRegisterSlides(CancellationToken ct);

    /// <summary>
    /// Retrieves a register slide by ID.
    /// </summary>
    Task<RegisterSlideResponseDTO?> GetRegisterSlide(int id, CancellationToken ct);

    /// <summary>
    /// Creates a new register slide.
    /// </summary>
    Task<RegisterSlideResponseDTO> CreateRegisterSlide(PostRegisterSlideDTO dto, Guid userId, CancellationToken ct);

    /// <summary>
    /// Updates a register slide.
    /// </summary>
    Task UpdateRegisterSlide(int id, RegisterSlideUpdateDTO dto, Guid userId, CancellationToken ct);

    /// <summary>
    /// Deletes a register slide.
    /// </summary>
    Task DeleteRegisterSlide(int id, Guid userId, CancellationToken ct);

    /// <summary>
    /// Uploads or clears the image for a register slide.
    /// </summary>
    Task<string?> UploadRegisterSlideImage(int id, Guid userId, IFormFile? image);

    /// <summary>
    /// Retrieves a slide image file by storage path.
    /// </summary>
    Task<FileResultDto?> GetRegisterSlideImageFile(string path);
}
