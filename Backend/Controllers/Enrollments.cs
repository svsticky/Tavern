using Backend.Controllers.DTOs;
using Backend.Interfaces;
using Backend.Models.Domain;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class EnrollmentsController : ControllerBase
{
    private readonly IEnrollmentService _enrollmentService;

    public EnrollmentsController(IEnrollmentService enrollmentService)
    {
        _enrollmentService = enrollmentService;
    }

    // GET: api/enrollments
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EnrollmentResponseDTO>>> GetEnrollments(GetEnrollmentsDTO dto, CancellationToken cancellationToken)
    {
        try
        {
            var userId = Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);

            var enrollments = await _enrollmentService.GetEnrollments(dto, userId, cancellationToken);
            return Ok(enrollments);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    // GET: api/enrollments/1/{memberId}
    [HttpGet("{activityId}/{memberId}")]
    public async Task<ActionResult<EnrollmentResponseDTO>> GetEnrollment(EnrollmentKeyDTO dto, CancellationToken cancellationToken)
    {
        try
        {
            var userId = Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);

            var enrollment = await _enrollmentService.GetEnrollment(dto, userId, cancellationToken);

            if (enrollment == null)
                return NotFound();
        
            return Ok(enrollment);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    // POST: api/enrollments
    [HttpPost]
    public async Task<ActionResult<EnrollmentResponseDTO>> PostEnrollment(
        PostEnrollmentDTO dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);

            var created = await _enrollmentService.CreateEnrollment(dto, userId, cancellationToken);

            return CreatedAtAction(
                nameof(GetEnrollment),
                new { activityId = created.Activity?.Id, memberId = created.Member?.Id },
                created
            );
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    // DELETE: api/enrollments/1/{memberId}
    [HttpDelete("{activityId}/{memberId}")]
    public async Task<ActionResult> DeleteEnrollment(EnrollmentKeyDTO dto, CancellationToken cancellationToken)
    {
        try
        {
            var userId = Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);

            await _enrollmentService.DeleteEnrollment(dto, userId, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    // PUT: api/enrollments/1/{memberId}
    [HttpPut("{activityId}/{memberId}")]
    public async Task<ActionResult> PutEnrollment(
        uint activityId, Guid memberId,
        [FromBody] PostEnrollmentDTO dto,
        CancellationToken cancellationToken)
    {
        try
        {
            if(activityId != dto.ActivityId || memberId != dto.MemberId)
                return BadRequest("ActivityId and MemberId in the URL must match those in the body.");

            var userId = Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);
            await _enrollmentService.UpdateEnrollment(dto, userId, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }

    // PATCH: api/enrollments/1/{memberId}
    [HttpPatch("{activityId}/{memberId}")]
    public async Task<ActionResult> PatchEnrollment(
        [FromRoute] EnrollmentKeyDTO dto,
        [FromBody] JsonPatchDocument<Enrollment> patchDoc,
        CancellationToken cancellationToken)
    {
        if (patchDoc == null)
            return BadRequest();

        try
        {
            var userId = Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);
            await _enrollmentService.PatchEnrollment(dto, patchDoc, userId, cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
}