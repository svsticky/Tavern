using Backend.Controllers.DTOs;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models;
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

    public async Task<IEnumerable<GetAnnouncementResponseDTO>> GetAnnouncements(CancellationToken cancellationToken)
    {
        var announcements = await _db.Announcements
            .Include(a => a.CreatedBy)
            .ToListAsync(cancellationToken);

        return announcements.Select(a => new GetAnnouncementResponseDTO
        {
            Id = a.Id,
            Title = a.Title,
            Content = a.Content,
            CreatedById = a.CreatedById,
            CreatedAt = a.CreatedAt,
            CreatedByName = a.CreatedBy != null
                ? $"{a.CreatedBy.FirstName} {a.CreatedBy.LastName}"
                : "Unknown"
        });
    }

    public async Task<Announcement?> GetAnnouncement(uint id, CancellationToken cancellationToken)
    {
        return await _db.Announcements
            .Include(a => a.CreatedBy)
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<Announcement> CreateAnnouncement(Guid userId, PostAnnouncementDTO dto, CancellationToken cancellationToken)
    {
        var announcement = new Announcement
        {
            Title = dto.Title,
            Content = dto.Content,
            CreatedById = userId,
            CreatedAt = DateTime.UtcNow
        };

        StateValidateUtils.Validate(announcement);

        _db.Announcements.Add(announcement);
        await _db.SaveChangesAsync(cancellationToken);

        return announcement;
    }

    public async Task DeleteAnnouncement(uint id, CancellationToken cancellationToken)
    {
        var announcement = await _db.Announcements.FindAsync(new object[] { id }, cancellationToken);

        if (announcement == null)
            throw new KeyNotFoundException();

        _db.Announcements.Remove(announcement);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task PatchAnnouncement(uint id, JsonPatchDocument<Announcement> patchDoc, CancellationToken cancellationToken)
    {
        if (patchDoc == null)
            throw new ArgumentException("Patch document cannot be null");

        var announcement = await _db.Announcements.FindAsync(new object[] { id }, cancellationToken);

        if (announcement == null)
            throw new KeyNotFoundException();

        patchDoc.ApplyTo(announcement);
        StateValidateUtils.Validate(announcement);

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAnnouncement(uint id, UpdateAnnouncementDTO dto, CancellationToken cancellationToken)
    {
        var announcement = await _db.Announcements.FindAsync(new object[] { id }, cancellationToken);

        if (announcement == null)
            throw new KeyNotFoundException();

        announcement.Title = dto.Title;
        announcement.Content = dto.Content;

        StateValidateUtils.Validate(announcement);

        await _db.SaveChangesAsync(cancellationToken);
    }
}