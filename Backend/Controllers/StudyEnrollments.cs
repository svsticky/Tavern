using Microsoft.AspNetCore.Mvc;
using Backend.Database;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using Backend.Controllers.DTOs;
using Microsoft.AspNetCore.Authorization;
using Backend.Utils;

namespace Backend.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class StudyEnrollmentsController(PostgresDbContext db) : ControllerBase
{
    // GET: api/studyenrollments
    /// <summary>
    /// Lists all study enrollments in the database.
    /// </summary>
    /// <returns>Said list.</returns>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<StudyEnrollment>>> GetStudyEnrollments(CancellationToken cancellationToken)
    {
        Guid userId = Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);

        if(!PermissionUtils.IsInGroupInCurrentYear(userId, (uint)PredefinedGroups.Board, db))
        {
            return Forbid("Only board members can view study enrollments.");
        }

        var result = await db.StudyEnrollments
            .Select(se => new StudyEnrollmentResponseDTO
            {
                Id = se.Id,
                MemberId = se.MemberId,
                MemberName = $"{se.Member.FirstName} {se.Member.LastName}",
                StudyId = se.StudyId,
                StudyTitle = se.Study.Title,
                EnrollmentDate = se.EnrollmentDate,
                CompletionDate = se.CompletionDate,
                Status = se.Status,
            })
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    // GET: api/studyenrollments/5
    /// <summary>
    /// Fetches a single study enrollment.
    /// </summary>
    /// <param name="id">The id of the study enrollment to fetch.</param>
    /// <returns>The full study enrollment.</returns> 
    [HttpGet("{id}")]
    public async Task<ActionResult<StudyEnrollment>> GetStudyEnrollment(uint id, CancellationToken cancellationToken)
    {
        Guid userId = Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);

        var result = await db.StudyEnrollments
            .Where(se => se.Id == id)
            .Select(se => new StudyEnrollmentResponseDTO
            {
                Id = se.Id,
                MemberId = se.MemberId,
                MemberName = $"{se.Member.FirstName} {se.Member.LastName}",
                StudyId = se.StudyId,
                StudyTitle = se.Study.Title,
                EnrollmentDate = se.EnrollmentDate,
                CompletionDate = se.CompletionDate,
                Status = se.Status
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (result is null) return NotFound();

        if(!PermissionUtils.IsInGroupInCurrentYear(userId, (uint)PredefinedGroups.Board, db) && result.MemberId != userId)
        {
            return Forbid("Only board members can view study enrollments of others.");
        }

        return Ok(result);
    }

    // POST: api/studyenrollments
    /// <summary>
    /// Creates a new study enrollment with a unique ID assigned by the database.
    /// </summary>
    /// <param name="enrollmentDto">The study enrollment to be added to the database.</param>
    /// <returns>Fully created study enrollment in body and api route of where to
    [HttpPost]
    public async Task<ActionResult<StudyEnrollment>> PostStudyEnrollment(PostStudyEnrollmentDTO enrollmentDto, CancellationToken cancellationToken)
    {
        Guid userId = Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);

        if(!PermissionUtils.IsInGroupInCurrentYear(userId, (uint)PredefinedGroups.Board, db))
        {
            return Forbid("Only board members can create study enrollments.");
        }

        Member? member = await db.Members.FindAsync(enrollmentDto.MemberId, cancellationToken);
        if (member is null)
            return BadRequest($"Member with ID {enrollmentDto.MemberId} does not exist.");

        Study? study = await db.Studies.FindAsync(enrollmentDto.StudyId, cancellationToken);
        if (study is null)
            return BadRequest($"Study with ID {enrollmentDto.StudyId} does not exist.");

        var newEnrollment = new StudyEnrollment
        {
            Member = member,
            Study = study,
            EnrollmentDate = enrollmentDto.EnrollmentDate,
            Status = enrollmentDto.Status
        };
        var newEntry = db.StudyEnrollments.Add(newEnrollment);
        await db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(
            nameof(GetStudyEnrollment),
            new { id = newEntry.Entity.Id },
            new StudyEnrollmentResponseDTO
            {
                Id = newEntry.Entity.Id,
                MemberId = newEntry.Entity.MemberId,
                StudyId = newEntry.Entity.StudyId,
                EnrollmentDate = newEntry.Entity.EnrollmentDate,
                CompletionDate = newEntry.Entity.CompletionDate,
                Status = newEntry.Entity.Status
            }
        );
    }

    // DELETE: api/studyenrollments/5
    /// <summary>
    /// Deletes a study enrollment.
    /// </summary>
    /// <param name="id">The id of the study enrollment to delete.</param>
    /// <returns>No content.</returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteStudyEnrollment(uint id, CancellationToken cancellationToken)
    {
        Guid userId = Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);

        if(!PermissionUtils.IsInGroupInCurrentYear(userId, (uint)PredefinedGroups.Board, db))
        {
            return Forbid("Only board members can delete study enrollments.");
        }

        var enrollment = await db.StudyEnrollments.FindAsync(id, cancellationToken);
        if (enrollment is null)
            return NotFound();

        db.StudyEnrollments.Remove(enrollment);
        await db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    // PATCH: api/studyenrollments
    /// <summary>
    /// Updates an existing study enrollment.
    /// </summary>
    /// <param name="enrollmentDto">The study enrollment to be updated.</param>
    /// <returns>Fully updated study enrollment in body and api route of where to fetch it in the headers.</returns>
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateStatus(uint id, [FromBody] StudyStatus newStatus, CancellationToken cancellationToken)
    {
        Guid userId = Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);

        if(!PermissionUtils.IsInGroupInCurrentYear(userId, (uint)PredefinedGroups.Board, db))
        {
            return Forbid("Only board members can change study enrollment statuses.");
        }

        var enrollment = await db.StudyEnrollments.FindAsync([id], cancellationToken);
        if (enrollment is null)
            return NotFound();

        enrollment.Status = newStatus;
        await db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}
