using Backend.Controllers.DTOs;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Backend.Validators;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services.Domain;

/// <summary>
/// Implements service operations for registration documents.
/// </summary>
public class RegistrationDocumentService(
    PostgresDbContext db,
    IPermissionService permissionService,
    ILogger<RegistrationDocumentService> logger
) : IRegistrationDocumentService
{
    /// <inheritdoc />
    public async Task<IEnumerable<RegistrationDocumentResponseDTO>> GetRegistrationDocuments(CancellationToken ct)
    {
        return await db.RegistrationDocuments
            .AsNoTracking()
            .OrderBy(d => d.SortOrder)
            .ThenBy(d => d.Id)
            .Select(d => new RegistrationDocumentResponseDTO
            {
                Id = d.Id,
                NameDutch = d.NameDutch,
                NameEnglish = d.NameEnglish,
                Url = d.Url,
                SortOrder = d.SortOrder
            })
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<RegistrationDocumentResponseDTO?> GetRegistrationDocument(int id, CancellationToken ct)
    {
        var doc = await db.RegistrationDocuments.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id, ct);
        if (doc == null) return null;

        return new RegistrationDocumentResponseDTO
        {
            Id = doc.Id,
            NameDutch = doc.NameDutch,
            NameEnglish = doc.NameEnglish,
            Url = doc.Url,
            SortOrder = doc.SortOrder
        };
    }

    /// <inheritdoc />
    public async Task<RegistrationDocumentResponseDTO> CreateRegistrationDocument(PostRegistrationDocumentDTO dto, Guid userId, CancellationToken ct)
    {
        permissionService.EnsureBoardOrCandidateBoardMember(userId);

        var doc = new RegistrationDocument
        {
            NameDutch = dto.NameDutch,
            NameEnglish = dto.NameEnglish,
            Url = dto.Url,
            SortOrder = dto.SortOrder
        };

        StateValidator.Validate(doc);
        db.RegistrationDocuments.Add(doc);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Created registration document {DocumentId} by user {UserId}", doc.Id, userId);

        return new RegistrationDocumentResponseDTO
        {
            Id = doc.Id,
            NameDutch = doc.NameDutch,
            NameEnglish = doc.NameEnglish,
            Url = doc.Url,
            SortOrder = doc.SortOrder
        };
    }

    /// <inheritdoc />
    public async Task UpdateRegistrationDocument(int id, RegistrationDocumentUpdateDTO dto, Guid userId, CancellationToken ct)
    {
        permissionService.EnsureBoardOrCandidateBoardMember(userId);

        var doc = await db.RegistrationDocuments.FirstOrDefaultAsync(d => d.Id == id, ct)
            ?? throw new KeyNotFoundException();

        doc.NameDutch = dto.NameDutch;
        doc.NameEnglish = dto.NameEnglish;
        doc.Url = dto.Url;
        doc.SortOrder = dto.SortOrder;

        StateValidator.Validate(doc);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Updated registration document {DocumentId} by user {UserId}", id, userId);
    }

    /// <inheritdoc />
    public async Task DeleteRegistrationDocument(int id, Guid userId, CancellationToken ct)
    {
        permissionService.EnsureBoardOrCandidateBoardMember(userId);

        var doc = await db.RegistrationDocuments.FirstOrDefaultAsync(d => d.Id == id, ct)
            ?? throw new KeyNotFoundException();

        db.RegistrationDocuments.Remove(doc);
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Deleted registration document {DocumentId} by user {UserId}", id, userId);
    }
}
