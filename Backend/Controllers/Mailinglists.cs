using Backend.Controllers.DTOs;
using Backend.Interfaces;
using Backend.Models.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

/// <summary>
/// Controller for managing mailing lists. The Mailinglists controller provides a set of endpoints for authorized users to perform CRUD operations on mailing list entities. This includes retrieving all mailing lists, fetching specific mailing list details, creating new mailing lists, updating existing ones, and deleting mailing lists as needed. The controller ensures that only users with appropriate permissions can access these operations, leveraging the IMailinglistService to handle the underlying business logic and data persistence while maintaining a secure and efficient interface for managing communication channels within the application.
/// </summary>
[Route("api/[controller]")]
[ApiController]
[Authorize]
public class Mailinglists : ControllerBase
{
    private readonly IMailinglistService _service;

    public Mailinglists(IMailinglistService service)
    {
        _service = service;
    }

    private Guid GetUserId() => Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<Mailinglist>>> GetMailinglists(CancellationToken ct)
    {
        try
        {
            var result = await _service.GetMailinglists(ct);
            return Ok(result);
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (Exception ex) { return BadRequest(ex.Message); }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Mailinglist>> GetMailinglist(int id, CancellationToken ct)
    {
        try
        {
            var result = await _service.GetMailinglist(id, ct);
            return result != null ? Ok(result) : NotFound();
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (Exception ex) { return BadRequest(ex.Message); }
    }

    [HttpPost]
    public async Task<ActionResult> PostMailinglist([FromBody] PostMailinglistDTO mailinglist, CancellationToken ct)
    {
        try
        {
            var result = await _service.CreateMailinglist(mailinglist, GetUserId(), ct);
            return CreatedAtAction(nameof(GetMailinglist), new { id = result.Id, bitValue = result.BitValue }, result);
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (Exception ex) { return BadRequest(ex.Message); }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> PutMailinglist(int id, [FromBody] PostMailinglistDTO mailinglist, CancellationToken ct)
    {
        try
        {
            await _service.UpdateMailinglist(id, mailinglist, GetUserId(), ct);
            return NoContent();
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (Exception ex) { return BadRequest(ex.Message); }
    }

    [HttpPatch("{id}")]
    public async Task<ActionResult> PatchMailinglist(int id, [FromBody] JsonPatchDocument<Mailinglist> patchDoc, CancellationToken ct)
    {
        try
        {
            await _service.PatchMailinglist(id, patchDoc, GetUserId(), ct);
            return NoContent();
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (Exception ex) { return BadRequest(ex.Message); }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteMailinglist(int id, CancellationToken ct)
    {
        try
        {
            await _service.DeleteMailinglist(id, GetUserId(), ct);
            return NoContent();
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (Exception ex) { return BadRequest(ex.Message); }
    }
}