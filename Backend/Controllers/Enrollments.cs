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
    public async Task<ActionResult<IEnumerable<Enrollment>>> GetEnrollments(bool ownEnrollments, CancellationToken cancellationToken)
    {   
        var userId = Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);

        var enrollments = await _enrollmentService.GetEnrollments(cancellationToken, ownEnrollments ? userId : null);
        return Ok(enrollments);
    }

    // GET: api/enrollments/1/{memberId}
    [HttpGet("{activityId}/{memberId}")]
    public async Task<ActionResult<Enrollment>> GetEnrollment(uint activityId, Guid memberId, CancellationToken cancellationToken)
    {
        var enrollment = await _enrollmentService.GetEnrollment(activityId, memberId, cancellationToken);

        if (enrollment == null)
            return NotFound();

        return Ok(enrollment);
    }

    // POST: api/enrollments
    [HttpPost]
    public async Task<ActionResult<Enrollment>> PostEnrollment(
        PostEnrollmentDTO dto,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);

            var created = await _enrollmentService.CreateEnrollment(dto, userId, cancellationToken);

            return CreatedAtAction(
                nameof(GetEnrollment),
                new { activityId = created.ActivityId, memberId = created.MemberId },
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
    public async Task<ActionResult> DeleteEnrollment(uint activityId, Guid memberId, CancellationToken cancellationToken)
    {
        try
        {
            await _enrollmentService.DeleteEnrollment(activityId, memberId, cancellationToken);
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
        uint activityId,
        Guid memberId,
        PostEnrollmentDTO dto,
        CancellationToken cancellationToken)
    {
        try
        {
            await _enrollmentService.UpdateEnrollment(activityId, memberId, dto, cancellationToken);
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
        uint activityId,
        Guid memberId,
        [FromBody] JsonPatchDocument<Enrollment> patchDoc,
        CancellationToken cancellationToken)
    {
        if (patchDoc == null)
            return BadRequest();

        try
        {
            await _enrollmentService.PatchEnrollment(activityId, memberId, patchDoc, cancellationToken);
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