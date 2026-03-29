using Backend.Controllers.DTOs;
using Backend.Database;
using Backend.Models;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Backend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class Announcements(PostgresDbContext db) : ControllerBase
{
    // GET: api/announcements
    /// <summary>
    /// Lists all announcements in the database.
    /// </summary>
    /// <returns>Said list.</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Announcement>>> GetAnnouncements(CancellationToken cancellationToken)
    {
        return await db.Announcements.ToListAsync(cancellationToken);
    }

    // GET: api/announcements/5
    /// <summary>
    /// Fetches a single announcement.
    /// </summary>
    /// <param name="id">The id of the announcement to fetch.</param>
    /// <returns>The full announcement.</returns>
    [HttpGet("{id}")]
    public async Task<ActionResult<Announcement>> GetAnnouncement(uint id, CancellationToken cancellationToken)
    {
        Announcement? announcement = await db.Announcements.FindAsync(id, cancellationToken);

        return announcement != null ? announcement : NotFound();
    }

    // POST: api/announcements
    /// <summary>
    /// Creates a new announcement with a unique ID assigned by the database.
    /// </summary>
    /// <param name="announcementDto">The announcement to be added to the database.</param>
    /// <returns>Fully created announcement in body and api route of where to fetch it in the headers.</returns>
    [HttpPost]
    public async Task<ActionResult<Announcement>> PostAnnouncement(PostAnnouncementDTO announcementDto, CancellationToken cancellationToken)
    {
        var createdById = Guid.Parse(User.Claims.First(c => c.Type == "member_id").Value!);
        var newEntry = db.Announcements.Add(new Announcement
        {
            Title = announcementDto.Title,
            Content = announcementDto.Content,
            CreatedById = createdById,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetAnnouncement), new { id = newEntry.Entity.Id }, newEntry.Entity);
    }

    // DELETE: api/announcements/5
    /// <summary>
    /// Deletes an announcement.
    /// </summary>
    /// <param name="id">The id of the announcement to delete.</param>
    /// <returns>Nothing, really.</returns>
    /// <remarks>
    /// Deleting an announcement will also delete all enrollments and role enrollments associated with said
    /// announcement.
    /// </remarks>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAnnouncement(uint id, CancellationToken cancellationToken)
    {
        Announcement? announcement = await db.Announcements.FindAsync(id, cancellationToken);
        if (announcement == null) return NotFound();

        db.Announcements.Remove(announcement);
        await db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    // PATCH: api/announcements/5
    /// <summary>
    /// Partially updates an announcement's details.
    /// </summary>
    /// <param name="id">The id of the announcement to update.</param>
    /// <param name="patchDoc">The patch document containing the changes.</param>
    /// <returns>No Content.</returns>
    [HttpPatch("{id}")]
    public async Task<IActionResult> PatchAnnouncement(uint id, [FromBody] JsonPatchDocument<Announcement> patchDoc, CancellationToken cancellationToken)
    {
        if (patchDoc == null)
            return BadRequest();

        Announcement? announcement = await db.Announcements.FindAsync(new object[] { id }, cancellationToken);
        if (announcement == null)
            return NotFound();

        patchDoc.ApplyTo(announcement, ModelState);

        TryValidateModel(announcement);

        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        await db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    // PUT: api/announcements/5
    /// <summary>
    /// Updates an announcement.
    /// </summary>
    /// <param name="id">The id of the announcement to update.</param>
    /// <param name="announcementDto">The updated announcement data.</param>
    /// <returns>No Content.</returns>
    [HttpPut("{id}")]
    public async Task<IActionResult> PutAnnouncement(uint id, UpdateAnnouncementDTO announcementDto, CancellationToken cancellationToken)
    {
        Announcement? announcement = await db.Announcements.FindAsync(id, cancellationToken);
        if (announcement == null) return NotFound();

        announcement.Title = announcementDto.Title;
        announcement.Content = announcementDto.Content;
        await db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}