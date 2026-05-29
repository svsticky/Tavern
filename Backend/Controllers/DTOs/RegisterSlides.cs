using Backend.Models.Domain;

namespace Backend.Controllers.DTOs;

/// <summary>
/// Defines the DTO for creating a register slide.
/// </summary>
public class PostRegisterSlideDTO
{
    /// <summary>
    /// The image to upload for the slide.
    /// </summary>
    public required IFormFile Image { get; set; }

    /// <inheritdoc cref="RegisterSlide.SortOrder"/>
    public int? SortOrder { get; set; }
}

/// <summary>
/// Defines the DTO for updating a register slide.
/// </summary>
public class RegisterSlideUpdateDTO
{
    /// <inheritdoc cref="RegisterSlide.SortOrder"/>
    public required int SortOrder { get; set; }
}

/// <summary>
/// Represents the response DTO for a register slide.
/// </summary>
public class RegisterSlideResponseDTO
{
    /// <inheritdoc cref="RegisterSlide.Id"/>
    public required int Id { get; set; }

    /// <inheritdoc cref="RegisterSlide.SortOrder"/>
    public required int SortOrder { get; set; }

    /// <inheritdoc cref="RegisterSlide.ImagePath"/>
    public string? ImagePath { get; set; }
}
