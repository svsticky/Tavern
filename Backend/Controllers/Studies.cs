using Backend.Controllers.DTOs;
using Backend.Interfaces;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class StudiesController : ControllerBase
{
    private readonly IStudyService _service;

    public StudiesController(IStudyService service)
    {
        _service = service;
    }

    private Guid GetUserId()
    {
        return Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Study>>> GetStudies(CancellationToken ct)
    {
        return Ok(await _service.GetStudies(ct));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Study>> GetStudy(uint id, CancellationToken ct)
    {
        var study = await _service.GetStudy(id, ct);
        return study != null ? Ok(study) : NotFound();
    }

    [HttpPost]
    public async Task<IActionResult> PostStudy(PostStudyDTO dto, CancellationToken ct)
    {
        try
        {
            var result = await _service.CreateStudy(dto, GetUserId(), ct);
            return CreatedAtAction(nameof(GetStudy), new { id = result.Id }, result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteStudy(uint id, CancellationToken ct)
    {
        try
        {
            await _service.DeleteStudy(id, GetUserId(), ct);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPatch("{id}")]
    public async Task<IActionResult> PatchStudy(uint id, JsonPatchDocument<Study> patchDoc, CancellationToken ct)
    {
        try
        {
            await _service.PatchStudy(id, patchDoc, GetUserId(), ct);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> PutStudy(uint id, StudyUpdateDTO dto, CancellationToken ct)
    {
        try
        {
            await _service.UpdateStudy(id, dto, GetUserId(), ct);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }
}