using Backend.Controllers.DTOs;
using Backend.Interfaces;
using Backend.Models.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class StudyEnrollmentsController : ControllerBase
{
    private readonly IStudyEnrollmentService _service;

    public StudyEnrollmentsController(IStudyEnrollmentService service)
    {
        _service = service;
    }

    private Guid GetUserId()
    {
        return Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<StudyEnrollmentResponseDTO>>> GetStudyEnrollments([FromQuery] GetStudyEnrollmentsDTO dto, CancellationToken ct)
    {
        try
        {
            var result = await _service.GetStudyEnrollments(dto, GetUserId(), ct);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<StudyEnrollmentResponseDTO>> GetStudyEnrollment(uint id, CancellationToken ct)
    {
        try
        {
            var result = await _service.GetStudyEnrollment(id, GetUserId(), ct);
            return result != null ? Ok(result) : NotFound();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
    }

    [HttpPost]
    public async Task<ActionResult<StudyEnrollmentResponseDTO>> PostStudyEnrollment(PostStudyEnrollmentDTO dto, CancellationToken ct)
    {
        try
        {
            var result = await _service.CreateStudyEnrollment(dto, GetUserId(), ct);
            return CreatedAtAction(nameof(GetStudyEnrollment), new { id = result.Id }, result);
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
    public async Task<ActionResult> DeleteStudyEnrollment(uint id, CancellationToken ct)
    {
        try
        {
            await _service.DeleteStudyEnrollment(id, GetUserId(), ct);
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
    public async Task<ActionResult> PatchStudy(uint id, [FromBody] JsonPatchDocument<StudyEnrollment> patchDoc, CancellationToken ct)
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
            return NotFound(ex.Message);
        }
    }
}