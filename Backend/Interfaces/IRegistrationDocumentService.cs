using Backend.Controllers.DTOs;

namespace Backend.Interfaces;

/// <summary>
/// Defines service contracts for registration documents operations.
/// </summary>
public interface IRegistrationDocumentService
{
    /// <summary>
    /// Retrieves all registration documents in display order.
    /// </summary>
    Task<IEnumerable<RegistrationDocumentResponseDTO>> GetRegistrationDocuments(CancellationToken ct);

    /// <summary>
    /// Retrieves a specific registration document by its ID.
    /// </summary>
    Task<RegistrationDocumentResponseDTO?> GetRegistrationDocument(int id, CancellationToken ct);

    /// <summary>
    /// Creates a new registration document.
    /// </summary>
    Task<RegistrationDocumentResponseDTO> CreateRegistrationDocument(PostRegistrationDocumentDTO dto, Guid userId, CancellationToken ct);

    /// <summary>
    /// Updates an existing registration document.
    /// </summary>
    Task UpdateRegistrationDocument(int id, RegistrationDocumentUpdateDTO dto, Guid userId, CancellationToken ct);

    /// <summary>
    /// Deletes a registration document by ID.
    /// </summary>
    Task DeleteRegistrationDocument(int id, Guid userId, CancellationToken ct);
}
