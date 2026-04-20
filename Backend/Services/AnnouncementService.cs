using Backend.Controllers.DTOs;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Backend.Projections;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class AnnouncementService : IAnnouncementService
{
    private readonly PostgresDbContext _db;

    public AnnouncementService(PostgresDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<GetAnnouncementResponseDTO>> GetAnnouncements(CancellationToken ct)
    {
        return await _db.Announcements
            .AsNoTracking()
            .Select(AnnouncementProjections.ToDto())
            .OrderByDescending(a => a.CreatedAt)
            .Take(20)
            .ToListAsync(ct);
    }

    public async Task<GetAnnouncementResponseDTO?> GetAnnouncement(uint id, CancellationToken ct)
    {
        return await _db.Announcements
            .AsNoTracking()
            .Select(AnnouncementProjections.ToDto())
            .FirstOrDefaultAsync(a => a.Id == id, ct);
    }

    public async Task<Announcement> CreateAnnouncement(Guid userId, PostAnnouncementDTO dto, CancellationToken cancellationToken)
    {
        var announcement = BuildAnnouncement(userId, dto);

        StateValidator.Validate(announcement);

        _db.Announcements.Add(announcement);
        await _db.SaveChangesAsync(cancellationToken);

        return announcement;
    }

    public async Task DeleteAnnouncement(uint id, CancellationToken cancellationToken)
    {
        var announcement = await GetAnnouncementOrThrow(id, cancellationToken);

        _db.Announcements.Remove(announcement);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task PatchAnnouncement(uint id, JsonPatchDocument<Announcement> patchDoc, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(patchDoc);

        var announcement = await GetAnnouncementOrThrow(id, cancellationToken);

        patchDoc.ApplyTo(announcement);
        StateValidator.Validate(announcement);

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAnnouncement(uint id, UpdateAnnouncementDTO dto, CancellationToken cancellationToken)
    {
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
