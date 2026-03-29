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
            return await db.Enrollments.Include(e => e.Activity).ToListAsync(cancellationToken);
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
                .Include(e => e.Activity)
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
            using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                Member? member = await db.Members.FindAsync(dto.MemberId, cancellationToken);
                
                if (member == null) return NotFound("Member not found.");

                if (PaymentUtils.HasPaidMembershipPayment(member, db) == false)
                {
                    return BadRequest("Member does not have a paid membership payment.");
                }

                if (member.Suspended) 
                {
                    return BadRequest("Member is suspended and cannot enroll in activities.");
                }

                Activity? activity = await db.Activities
                    .Include(a => a.SpecificationQuestions)
                    .Include(a => a.Enrollments)
                    .FirstOrDefaultAsync(a => a.Id == dto.ActivityId, cancellationToken);
                
                if (activity == null) return NotFound("Activity not found.");

                if (activity.Enrollments.Any(e => e.MemberId == dto.MemberId))
                {
                    return BadRequest("Member is already enrolled (or on the waiting list) for this activity.");
                }

                bool isBoardMember = PermissionUtils.IsInGroupInCurrentYear(member.Id, (uint)PredefinedGroups.Board, db);

                if(!isBoardMember && !TargetAudienceHelper.IsMemberInTargetAudience(member, activity.AllowedAudience))
                {
                    return BadRequest("Member is not in the target audience for this activity.");
                }

                var providedAnswers = dto.SpecificationAnswers ?? new List<PostSpecificationAnswerDTO>();
                var mandatoryQuestionIds = activity.SpecificationQuestions
                    .Where(q => q.IsMandatory)
                    .Select(q => q.Id)
                    .ToList();
                var providedQuestionIds = providedAnswers.Select(a => a.QuestionId).ToList();
                var validQuestionIds = activity.SpecificationQuestions.Select(q => q.Id).ToHashSet();

                if (providedAnswers.Any(a => !validQuestionIds.Contains(a.QuestionId)))
                {
                    return BadRequest("One or more answers refer to questions not belonging to this activity.");
                }

                if (mandatoryQuestionIds.Except(providedQuestionIds).Any())
                {
                    return BadRequest("One or more mandatory questions were not answered.");
                }
                
                int currentParticipants = activity.Enrollments.Count(e => !e.IsOnWaitingList);
                bool shouldBeOnWaitingList = activity.ParticipantLimit.HasValue && currentParticipants >= activity.ParticipantLimit.Value;
                var enrollment = new Enrollment
                {
                    ActivityId = dto.ActivityId,
                    MemberId = dto.MemberId,
                    Price = activity.Price,
                    RegisteredOn = DateTime.UtcNow,
                    IsOnWaitingList = shouldBeOnWaitingList,
                    SpecificationAnswers = providedAnswers.Select(a => new SpecificationAnswer
                    {
                        SpecificationQuestionId = a.QuestionId,
                        Answer = a.Answer,
                        MemberId = dto.MemberId
                    }).ToList()
                };

                db.Enrollments.Add(enrollment);
                await db.SaveChangesAsync(cancellationToken);

                transaction.Commit();

                return CreatedAtAction(nameof(GetEnrollment), 
                    new { activityId = enrollment.ActivityId, memberId = enrollment.MemberId }, 
                    enrollment);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
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
            using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var enrollment = await db.Enrollments
                    .Include(e => e.SpecificationAnswers)
                    .Include(e => e.Activity)
                    .FirstOrDefaultAsync(e => e.ActivityId == activityId && e.MemberId == memberId, cancellationToken);
            
                if (enrollment == null) return NotFound();

                bool wasOnWaitingList = enrollment.IsOnWaitingList;
                bool placeWillBeFreed = enrollment.Activity.ParticipantLimit.HasValue 
                    && enrollment.Activity.Enrollments.Count(e => !e.IsOnWaitingList) == enrollment.Activity.ParticipantLimit.Value;

                db.SpecificationAnswers.RemoveRange(enrollment.SpecificationAnswers);
                db.Enrollments.Remove(enrollment);
                await db.SaveChangesAsync(cancellationToken);

                if (!wasOnWaitingList)
                {
                    var firstOnWaitingList = await db.Enrollments
                        .Where(e => e.ActivityId == activityId && e.IsOnWaitingList)
                        .OrderBy(e => e.RegisteredOn)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (firstOnWaitingList != null)
                    {
                        firstOnWaitingList.IsOnWaitingList = false;
                        await db.SaveChangesAsync(cancellationToken);
                    }
                }

                await transaction.CommitAsync(cancellationToken);
                return NoContent();
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // PUT: api/enrollments/1/00000000-0000-0000-0000-000000000000
        /// <summary>
        /// Updates an enrollment and its specification answers.
        /// </summary>
        [HttpPut("{activityId}/{memberId}")]
        public async Task<IActionResult> PutEnrollment(uint activityId, Guid memberId, PostEnrollmentDTO dto, CancellationToken cancellationToken)
        {
            if (activityId != dto.ActivityId || memberId != dto.MemberId)
            {
                return BadRequest("URL parameters do not match the request body.");
            }

            using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var enrollment = await db.Enrollments
                    .Include(e => e.SpecificationAnswers)
                    .FirstOrDefaultAsync(e => e.ActivityId == activityId && e.MemberId == memberId, cancellationToken);

                if (enrollment == null) return NotFound("Enrollment not found.");

                var activity = await db.Activities
                    .Include(a => a.SpecificationQuestions)
                    .FirstOrDefaultAsync(a => a.Id == activityId, cancellationToken);

                if (activity == null) return NotFound("Activity not found.");

                var providedAnswers = dto.SpecificationAnswers ?? new List<PostSpecificationAnswerDTO>();
                var validQuestionIds = activity.SpecificationQuestions.Select(q => q.Id).ToHashSet();
                var mandatoryQuestionIds = activity.SpecificationQuestions.Where(q => q.IsMandatory).Select(q => q.Id).ToList();
                var providedQuestionIds = providedAnswers.Select(a => a.QuestionId).ToList();

                if (providedAnswers.Any(a => !validQuestionIds.Contains(a.QuestionId)))
                {
                    return BadRequest("One or more answers refer to questions not belonging to this activity.");
                }

                if (mandatoryQuestionIds.Except(providedQuestionIds).Any())
                {
                    return BadRequest("One or more mandatory questions were not answered.");
                }

                db.SpecificationAnswers.RemoveRange(enrollment.SpecificationAnswers);

                enrollment.SpecificationAnswers = providedAnswers.Select(a => new SpecificationAnswer
                {
                    SpecificationQuestionId = a.QuestionId,
                    Answer = a.Answer,
                    MemberId = memberId,
                    Enrollment = enrollment,
                }).ToList();

                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return NoContent();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(cancellationToken);
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // PATCH: api/enrollments/1/00000000-0000-0000-0000-000000000000
        /// <summary>
        /// Partially updates an enrollment (e.g., changing the price).
        /// </summary>
        [HttpPatch("{activityId}/{memberId}")]
        public async Task<IActionResult> PatchEnrollment(uint activityId, Guid memberId, [FromBody] JsonPatchDocument<Enrollment> patchDoc, CancellationToken cancellationToken)
        {
            if (patchDoc == null) return BadRequest();

            // if trying to change the activity or member, reject the request as these are part of the composite key and cannot be changed
            if (patchDoc.Operations.Any(op => string.Equals(op.path.ToLower(), "/activityid", StringComparison.OrdinalIgnoreCase) 
                || string.Equals(op.path.ToLower(), "/memberid", StringComparison.OrdinalIgnoreCase)))
            {
                return BadRequest("Cannot change ActivityId or MemberId of an enrollment.");
            }

            Enrollment? enrollment = await db.Enrollments
                .FirstOrDefaultAsync(e => e.ActivityId == activityId && e.MemberId == memberId, cancellationToken);
            
            if (enrollment == null) return NotFound();

            patchDoc.ApplyTo(enrollment, ModelState);

            TryValidateModel(enrollment);

            if (!ModelState.IsValid) 
                return BadRequest(ModelState);

            await db.SaveChangesAsync(cancellationToken);

            return NoContent();
        }
    }
}