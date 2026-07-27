using System.ComponentModel.DataAnnotations;
using Backend.Models.Domain;

namespace Backend.Controllers.DTOs;

/// <summary>
/// Defines the DTO for creating a registration document.
/// </summary>
public class PostRegistrationDocumentDTO
{
    /// <inheritdoc cref="RegistrationDocument.NameDutch"/>
    [StringLength(120)]
    [Required(AllowEmptyStrings = false)]
    public required string NameDutch { get; set; }

    /// <inheritdoc cref="RegistrationDocument.NameEnglish"/>
    [StringLength(120)]
    [Required(AllowEmptyStrings = false)]
    public required string NameEnglish { get; set; }

    /// <inheritdoc cref="RegistrationDocument.Url"/>
    [StringLength(2000)]
    [Required(AllowEmptyStrings = false)]
    public required string Url { get; set; }

    /// <inheritdoc cref="RegistrationDocument.SortOrder"/>
    public int SortOrder { get; set; }
}

/// <summary>
/// Defines the DTO for updating a registration document.
/// </summary>
public class RegistrationDocumentUpdateDTO
{
    /// <inheritdoc cref="RegistrationDocument.NameDutch"/>
    [StringLength(120)]
    [Required(AllowEmptyStrings = false)]
    public required string NameDutch { get; set; }

    /// <inheritdoc cref="RegistrationDocument.NameEnglish"/>
    [StringLength(120)]
    [Required(AllowEmptyStrings = false)]
    public required string NameEnglish { get; set; }

    /// <inheritdoc cref="RegistrationDocument.Url"/>
    [StringLength(2000)]
    [Required(AllowEmptyStrings = false)]
    public required string Url { get; set; }

    /// <inheritdoc cref="RegistrationDocument.SortOrder"/>
    public int SortOrder { get; set; }
}

/// <summary>
/// Represents the response DTO for a registration document.
/// </summary>
public class RegistrationDocumentResponseDTO
{
    /// <inheritdoc cref="RegistrationDocument.Id"/>
    public required int Id { get; set; }

    /// <inheritdoc cref="RegistrationDocument.NameDutch"/>
    public required string NameDutch { get; set; }

    /// <inheritdoc cref="RegistrationDocument.NameEnglish"/>
    public required string NameEnglish { get; set; }

    /// <inheritdoc cref="RegistrationDocument.Url"/>
    public required string Url { get; set; }

    /// <inheritdoc cref="RegistrationDocument.SortOrder"/>
    public required int SortOrder { get; set; }
}
