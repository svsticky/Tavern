using Backend.Controllers.DTOs;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Backend.Projections;
using Backend.Validators;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

/// <summary>
/// Implements announcement management operations.
/// </summary>
public class AnnouncementService : IAnnouncementService
{
    private readonly PostgresDbContext _db;
    private readonly IPermissionService _permissionService;
    private readonly ILogger<AnnouncementService> _logger;

    public AnnouncementService(PostgresDbContext db, IPermissionService permissionService, ILogger<AnnouncementService> logger)
    {
        _db = db;
        _permissionService = permissionService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<GetAnnouncementResponseDTO>> GetAnnouncements(Guid userId, CancellationToken ct)
    {
        return await _db.Announcements
            .AsNoTracking()
            .Select(AnnouncementProjections.ToDto(userId, _permissionService.IsBoardOrCandidateBoardMember(userId)))
            .OrderByDescending(a => a.CreatedAt)
            .Take(20)
            .ToListAsync(ct);
    }

    /// <inheritdoc />
    public async Task<GetAnnouncementResponseDTO?> GetAnnouncement(uint id, Guid userId, CancellationToken ct)
    {
        return await _db.Announcements
            .AsNoTracking()
            .Select(AnnouncementProjections.ToDto(userId, _permissionService.IsBoardOrCandidateBoardMember(userId)))
            .FirstOrDefaultAsync(a => a.Id == id, ct);
    }

    /// <inheritdoc />
    public async Task<Announcement> CreateAnnouncement(Guid userId, PostAnnouncementDTO dto, CancellationToken cancellationToken)
    {
        _permissionService.EnsureBoardOrCandidateBoardMember(userId);
        _logger.LogInformation("Creating announcement by user {UserId}.", userId);

        var announcement = BuildAnnouncement(userId, dto);

        StateValidator.Validate(announcement);

        _db.Announcements.Add(announcement);
        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Created announcement {AnnouncementId}.", announcement.Id);

        return announcement;
    }

    /// <inheritdoc />
    public async Task DeleteAnnouncement(uint id, Guid userId, CancellationToken cancellationToken)
    {
        _permissionService.EnsureBoardOrCandidateBoardMember(userId);
        _logger.LogInformation("Deleting announcement {AnnouncementId} by user {UserId}.", id, userId);

        var announcement = await GetAnnouncementOrThrow(id, cancellationToken);

        _db.Announcements.Remove(announcement);
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task PatchAnnouncement(uint id, JsonPatchDocument<Announcement> patchDoc, Guid userId, CancellationToken cancellationToken)
    {
        _permissionService.EnsureBoardOrCandidateBoardMember(userId);
        _logger.LogInformation("Patching announcement {AnnouncementId} by user {UserId}.", id, userId);

        ArgumentNullException.ThrowIfNull(patchDoc);

        if(patchDoc.Operations.Any(op => op.path.Equals("/id", StringComparison.OrdinalIgnoreCase) 
            || op.path.Equals("/createdById", StringComparison.OrdinalIgnoreCase) 
            || op.path.Equals("/createdAt", StringComparison.OrdinalIgnoreCase)
            || op.path.Equals("/createdBy", StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("Cannot modify Id, CreatedById or CreatedAt fields.");

        var announcement = await GetAnnouncementOrThrow(id, cancellationToken);

        patchDoc.ApplyTo(announcement);
        StateValidator.Validate(announcement);

        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAnnouncement(uint id, UpdateAnnouncementDTO dto, Guid userId, CancellationToken cancellationToken)
    {
        _permissionService.EnsureBoardOrCandidateBoardMember(userId);
        _logger.LogInformation("Updating announcement {AnnouncementId} by user {UserId}.", id, userId);

        var announcement = await GetAnnouncementOrThrow(id, cancellationToken);
        ApplyUpdate(announcement, dto);

        StateValidator.Validate(announcement);

        await _db.SaveChangesAsync(cancellationToken);
    }

    private static Announcement BuildAnnouncement(Guid userId, PostAnnouncementDTO dto)
    {
        return new Announcement
        {
            Title = dto.Title,
            Content = dto.Content,
            CreatedById = userId,
            CreatedAt = DateTime.UtcNow
        };
    }

    private static void ApplyUpdate(Announcement announcement, UpdateAnnouncementDTO dto)
    {
        announcement.Title = dto.Title;
        announcement.Content = dto.Content;
    }

    private async Task<Announcement> GetAnnouncementOrThrow(uint id, CancellationToken ct)
    {
        var announcement = await _db.Announcements.FindAsync(new object[] { id }, ct);
        return announcement ?? throw new KeyNotFoundException();
    }
}
