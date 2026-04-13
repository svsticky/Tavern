using Backend.Controllers.DTOs;
using Backend.Interfaces;
using Backend.Models.Domain;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AnnouncementsController : ControllerBase
{
    private readonly IAnnouncementService _announcementService;

    public AnnouncementsController(IAnnouncementService announcementService)
    {
        _announcementService = announcementService;
    }

    // GET: api/announcements
    [HttpGet]
    public async Task<ActionResult<IEnumerable<GetAnnouncementResponseDTO>>> GetAnnouncements(CancellationToken cancellationToken)
    {
        var announcements = await _announcementService.GetAnnouncements(cancellationToken);
        return Ok(announcements);
    }

    // GET: api/announcements/5
    [HttpGet("{id}")]
    public async Task<ActionResult<Announcement>> GetAnnouncement(uint id, CancellationToken cancellationToken)
    {
        var announcement = await _announcementService.GetAnnouncement(id, cancellationToken);

        if (announcement == null)
            return NotFound();

        return Ok(announcement);
    }

    // POST: api/announcements
    [HttpPost]
    public async Task<ActionResult<Announcement>> PostAnnouncement(PostAnnouncementDTO dto, CancellationToken cancellationToken)
    {
        var userId = Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);

        var created = await _announcementService.CreateAnnouncement(userId, dto, cancellationToken);

        return CreatedAtAction(nameof(GetAnnouncement), new { id = created.Id }, created);
    }

    // DELETE: api/announcements/5
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteAnnouncement(uint id, CancellationToken cancellationToken)
    {
        try
        {
            await _announcementService.DeleteAnnouncement(id, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    // PATCH: api/announcements/5
    [HttpPatch("{id}")]
    public async Task<ActionResult> PatchAnnouncement(uint id, [FromBody] JsonPatchDocument<Announcement> patchDoc, CancellationToken cancellationToken)
    {
        if (patchDoc == null)
            return BadRequest();

        try
        {
            await _announcementService.PatchAnnouncement(id, patchDoc, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (ArgumentException)
        {
            return BadRequest();
        }
    }

    // PUT: api/announcements/5
    [HttpPut("{id}")]
    public async Task<ActionResult> PutAnnouncement(uint id, UpdateAnnouncementDTO dto, CancellationToken cancellationToken)
    {
        try
        {
            await _announcementService.UpdateAnnouncement(id, dto, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}