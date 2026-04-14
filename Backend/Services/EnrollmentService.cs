using Backend.Controllers.DTOs;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models;
using Backend.Models.Domain;
using Backend.Utils;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class EnrollmentService : IEnrollmentService
{
    private readonly PostgresDbContext _db;
    private readonly IPermissionService _permissionService;
    private readonly IPaymentValidationService _paymentValidationService;

    public EnrollmentService(
        PostgresDbContext db,
        IPermissionService permissionService,
        IPaymentValidationService paymentValidationService)
    {
        _db = db;
        _permissionService = permissionService;
        _paymentValidationService = paymentValidationService;
    }

    public async Task<IEnumerable<Enrollment>> GetEnrollments(CancellationToken cancellationToken, Guid? memberId = null)
    {
        var query = _db.Enrollments.Include(e => e.Activity).AsQueryable();

        if (memberId.HasValue)
        {
            query = query.Where(e => e.MemberId == memberId.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<Enrollment?> GetEnrollment(uint activityId, Guid memberId, CancellationToken cancellationToken)
    {
        return await _db.Enrollments
            .Include(e => e.Activity)
            .FirstOrDefaultAsync(e => e.ActivityId == activityId && e.MemberId == memberId, cancellationToken);
    }

    public async Task<Enrollment> CreateEnrollment(PostEnrollmentDTO dto, Guid userId, CancellationToken cancellationToken)
    {
        using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var member = await _db.Members.FindAsync(new object[] { dto.MemberId }, cancellationToken);
            if (member == null)
                throw new KeyNotFoundException("Member not found.");

            if (!_paymentValidationService.HasPaidMembershipPayment(member.Id))
                throw new ArgumentException("Member does not have a paid membership payment.");

            if (member.Suspended)
                throw new ArgumentException("Member is suspended and cannot enroll in activities.");

            var activity = await _db.Activities
                .Include(a => a.SpecificationQuestions)
                .Include(a => a.Enrollments)
                .FirstOrDefaultAsync(a => a.Id == dto.ActivityId, cancellationToken);

            if (activity == null)
                throw new KeyNotFoundException("Activity not found.");

            bool isBoardMember = _permissionService.IsBoardOrCandidateBoardMember(member.Id);

            var enrollmentDeadline = activity.EnrollmentDeadline ?? activity.DateTimeEnd;
            if(enrollmentDeadline < DateTime.UtcNow && !isBoardMember)
                throw new ArgumentException("Enrollment deadline has passed.");

            if (activity.Enrollments.Any(e => e.MemberId == dto.MemberId))
                throw new ArgumentException("Member is already enrolled (or on waiting list).");

            if (!isBoardMember && !TargetAudienceHelper.IsMemberInTargetAudience(member, activity.AllowedAudience))
                throw new ArgumentException("Member is not in the target audience for this activity.");

            var providedAnswers = dto.SpecificationAnswers ?? new List<PostSpecificationAnswerDTO>();

            var mandatoryQuestionIds = activity.SpecificationQuestions
                .Where(q => q.IsMandatory)
                .Select(q => q.Id)
                .ToList();

            var providedQuestionIds = providedAnswers.Select(a => a.QuestionId).ToList();

            var validQuestionIds = activity.SpecificationQuestions.Select(q => q.Id).ToHashSet();

            if (providedAnswers.Any(a => !validQuestionIds.Contains(a.QuestionId)))
                throw new ArgumentException("Invalid specification question(s) provided.");

            if (mandatoryQuestionIds.Except(providedQuestionIds).Any())
                throw new ArgumentException("Missing mandatory specification answers.");

            if (providedAnswers.Any(a => !AnswerValidateUtils.IsValidAnswer(a.Answer, activity.SpecificationQuestions.First(q => q.Id == a.QuestionId).Type, activity.SpecificationQuestions.First(q => q.Id == a.QuestionId).Options)))
                throw new ArgumentException("One or more provided answers are invalid.");

            int currentParticipants = activity.Enrollments.Count(e => !e.IsOnWaitingList);

            bool shouldBeOnWaitingList =
                activity.ParticipantLimit.HasValue &&
                currentParticipants >= activity.ParticipantLimit.Value;

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

            StateValidateUtils.Validate(enrollment);

            _db.Enrollments.Add(enrollment);
            await _db.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return enrollment;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task DeleteEnrollment(uint activityId, Guid memberId, CancellationToken cancellationToken)
    {
        using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var enrollment = await _db.Enrollments
                .Include(e => e.SpecificationAnswers)
                .Include(e => e.Activity)
                .ThenInclude(a => a.Enrollments)
                .FirstOrDefaultAsync(e => e.ActivityId == activityId && e.MemberId == memberId, cancellationToken);

            if (enrollment == null)
                throw new KeyNotFoundException();

            // to do: check if it is own enrollment or if user is board member

            var enrollmentDeadline = enrollment.Activity.EnrollmentDeadline ?? enrollment.Activity.DateTimeEnd;
            
            // To do: check if enrollmentdeadline passed, and if so, only allow deletion if user is board member

            bool wasOnWaitingList = enrollment.IsOnWaitingList;

            _db.SpecificationAnswers.RemoveRange(enrollment.SpecificationAnswers);
            _db.Enrollments.Remove(enrollment);

            await _db.SaveChangesAsync(cancellationToken);

            if (!wasOnWaitingList)
            {
                var next = await _db.Enrollments
                    .Where(e => e.ActivityId == activityId && e.IsOnWaitingList)
                    .OrderBy(e => e.RegisteredOn)
                    .FirstOrDefaultAsync(cancellationToken);

                if (next != null)
                {
                    next.IsOnWaitingList = false;
                    await _db.SaveChangesAsync(cancellationToken);
                }
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task UpdateEnrollment(uint activityId, Guid memberId, PostEnrollmentDTO dto, CancellationToken cancellationToken)
    {
        if (activityId != dto.ActivityId || memberId != dto.MemberId)
            throw new ArgumentException("URL parameters do not match body.");

        using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var enrollment = await _db.Enrollments
                .Include(e => e.SpecificationAnswers)
                .FirstOrDefaultAsync(e => e.ActivityId == activityId && e.MemberId == memberId, cancellationToken);

            if (enrollment == null)
                throw new KeyNotFoundException("Enrollment not found.");

            var activity = await _db.Activities
                .Include(a => a.SpecificationQuestions)
                .FirstOrDefaultAsync(a => a.Id == activityId, cancellationToken);

            if (activity == null)
                throw new KeyNotFoundException("Activity not found.");

            var providedAnswers = dto.SpecificationAnswers ?? new List<PostSpecificationAnswerDTO>();

            var validQuestionIds = activity.SpecificationQuestions.Select(q => q.Id).ToHashSet();
            var mandatoryQuestionIds = activity.SpecificationQuestions.Where(q => q.IsMandatory).Select(q => q.Id).ToList();
            var providedQuestionIds = providedAnswers.Select(a => a.QuestionId).ToList();

            if (providedAnswers.Any(a => !validQuestionIds.Contains(a.QuestionId)))
                throw new ArgumentException("Invalid specification question(s).");

            if (mandatoryQuestionIds.Except(providedQuestionIds).Any())
                throw new ArgumentException("Missing mandatory answers.");

            if (providedAnswers.Any(a => !AnswerValidateUtils.IsValidAnswer(a.Answer, activity.SpecificationQuestions.First(q => q.Id == a.QuestionId).Type, activity.SpecificationQuestions.First(q => q.Id == a.QuestionId).Options)))
                throw new ArgumentException("One or more provided answers are invalid.");

            _db.SpecificationAnswers.RemoveRange(enrollment.SpecificationAnswers);

            enrollment.SpecificationAnswers = providedAnswers.Select(a => new SpecificationAnswer
            {
                SpecificationQuestionId = a.QuestionId,
                Answer = a.Answer,
                MemberId = memberId,
                Enrollment = enrollment
            }).ToList();

            StateValidateUtils.Validate(enrollment);

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task PatchEnrollment(uint activityId, Guid memberId, JsonPatchDocument<Enrollment> patchDoc, CancellationToken cancellationToken)
    {
        if (patchDoc == null)
            throw new ArgumentException("Patch document cannot be null");

        if (patchDoc.Operations.Any(op =>
            op.path.Equals("/activityid", StringComparison.OrdinalIgnoreCase) ||
            op.path.Equals("/memberid", StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException("Cannot change ActivityId or MemberId.");
        }

        var enrollment = await _db.Enrollments
            .FirstOrDefaultAsync(e => e.ActivityId == activityId && e.MemberId == memberId, cancellationToken);

        if (enrollment == null)
            throw new KeyNotFoundException();

        patchDoc.ApplyTo(enrollment);
        StateValidateUtils.Validate(enrollment);

        await _db.SaveChangesAsync(cancellationToken);
    }
}