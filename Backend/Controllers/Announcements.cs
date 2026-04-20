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
        try
        {
            var userId = Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);
            var announcements = await _announcementService.GetAnnouncements(userId, cancellationToken);
            return Ok(announcements);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    // GET: api/announcements/5
    [HttpGet("{id}")]
    public async Task<ActionResult<GetAnnouncementResponseDTO>> GetAnnouncement(uint id, CancellationToken cancellationToken)
    {
        try
        {
            var userId = Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);
            var announcement = await _announcementService.GetAnnouncement(id, userId, cancellationToken);
            
            if (announcement == null)
                return NotFound();
        
            return Ok(announcement);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }

    }

    // POST: api/announcements
    [HttpPost]
    public async Task<ActionResult> PostAnnouncement(PostAnnouncementDTO dto, CancellationToken cancellationToken)
    {
        try
        {
            var userId = Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);
            var created = await _announcementService.CreateAnnouncement(userId, dto, cancellationToken);
            return CreatedAtAction(nameof(GetAnnouncement), new { id = created.Id }, created);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    // DELETE: api/announcements/5
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteAnnouncement(uint id, CancellationToken cancellationToken)
    {
        try
        {
            var userId = Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);
            await _announcementService.DeleteAnnouncement(id, userId, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
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
            var userId = Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);
            await _announcementService.PatchAnnouncement(id, patchDoc, userId, cancellationToken);
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
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    // PUT: api/announcements/5
    [HttpPut("{id}")]
    public async Task<ActionResult> PutAnnouncement(uint id, UpdateAnnouncementDTO dto, CancellationToken cancellationToken)
    {
        try
        {
            var userId = Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);
            await _announcementService.UpdateAnnouncement(id, dto, userId, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (UnauthorizedAccessException)        
        {
            return Forbid();
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }
}