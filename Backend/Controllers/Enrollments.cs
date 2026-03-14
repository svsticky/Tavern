using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.JsonPatch;
using Backend.Database;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using Backend.Controllers.DTOs;
using Backend.Utils;

namespace Backend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class Enrollments(PostgresDbContext db) : ControllerBase
    {
        // GET: api/enrollments
        /// <summary>
        /// Lists all enrollments in the database.
        /// </summary>
        /// <returns>A list of enrollments.</returns>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Enrollment>>> GetEnrollments(CancellationToken cancellationToken)
        {
            return await db.Enrollments.ToListAsync(cancellationToken);
        }

        // GET: api/enrollments/1/00000000-0000-0000-0000-000000000000
        /// <summary>
        /// Fetches a single enrollment by its composite key.
        /// </summary>
        /// <param name="activityId">The ID of the activity.</param>
        /// <param name="memberId">The Guid of the member.</param>
        /// <returns>The full enrollment record.</returns>
        [HttpGet("{activityId}/{memberId}")]
        public async Task<ActionResult<Enrollment>> GetEnrollment(uint activityId, Guid memberId, CancellationToken cancellationToken)
        {
            Enrollment? enrollment = await db.Enrollments
                .FirstOrDefaultAsync(e => e.ActivityId == activityId && e.MemberId == memberId, cancellationToken);

            return enrollment != null ? enrollment : NotFound();
        }

        // POST: api/enrollments
        /// <summary>
        /// Creates a new enrollment.
        /// </summary>
        /// <param name="enrollment">The enrollment details to add.</param>
        /// <returns>The created enrollment.</returns>
        [HttpPost]
        public async Task<ActionResult<Enrollment>> PostEnrollment(PostEnrollmentDTO dto, CancellationToken cancellationToken)
        {
            Member? member = await db.Members.FindAsync(dto.MemberId, cancellationToken);
            
            if (member == null) return NotFound("Member not found.");

            if (PaymentUtils.HasPaidMembershipPayment(member, db) == false)
            {
                return BadRequest("Member does not have a paid membership payment.");
            }

            Activity? activity = await db.Activities.FirstOrDefaultAsync(a => a.Id == dto.ActivityId, cancellationToken);
            
            if (activity == null) return NotFound("Activity not found.");
            
            var enrollment = new Enrollment
            {
                ActivityId = dto.ActivityId,
                MemberId = dto.MemberId,
                Price = activity.Price
            };

            db.Enrollments.Add(enrollment);
            await db.SaveChangesAsync(cancellationToken);

            return CreatedAtAction(nameof(GetEnrollment), 
                new { activityId = enrollment.ActivityId, memberId = enrollment.MemberId }, 
                enrollment);
        }

        // DELETE: api/enrollments/1/00000000-0000-0000-0000-000000000000
        /// <summary>
        /// Deletes an enrollment.
        /// </summary>
        /// <param name="activityId">The ID of the activity.</param>
        /// <param name="memberId">The Guid of the member.</param>
        /// <returns>No content.</returns>
        [HttpDelete("{activityId}/{memberId}")]
        public async Task<IActionResult> DeleteEnrollment(uint activityId, Guid memberId, CancellationToken cancellationToken)
        {
            Enrollment? enrollment = await db.Enrollments
                .FirstOrDefaultAsync(e => e.ActivityId == activityId && e.MemberId == memberId, cancellationToken);
            
            if (enrollment == null) return NotFound();

            db.Enrollments.Remove(enrollment);
            await db.SaveChangesAsync(cancellationToken);

            return NoContent();
        }

        // PATCH: api/enrollments/1/00000000-0000-0000-0000-000000000000
        /// <summary>
        /// Partially updates an enrollment (e.g., changing the price).
        /// </summary>
        [HttpPatch("{activityId}/{memberId}")]
        public async Task<IActionResult> PatchEnrollment(uint activityId, Guid memberId, [FromBody] JsonPatchDocument<Enrollment> patchDoc, CancellationToken cancellationToken)
        {
            if (patchDoc == null) return BadRequest();

            Enrollment? enrollment = await db.Enrollments
                .FirstOrDefaultAsync(e => e.ActivityId == activityId && e.MemberId == memberId, cancellationToken);
            
            if (enrollment == null) return NotFound();

            patchDoc.ApplyTo(enrollment, ModelState);

            if (!ModelState.IsValid) return BadRequest(ModelState);

            await db.SaveChangesAsync(cancellationToken);

            return NoContent();
        }
    }
}