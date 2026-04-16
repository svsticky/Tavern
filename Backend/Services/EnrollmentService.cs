using Backend.Controllers.DTOs;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models;
using Backend.Models.Domain;
using Backend.Validators;
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
        var query = _db.Enrollments.AsQueryable();

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

            EnrollmentValidator.ValidateAnswers(providedAnswers, activity.SpecificationQuestions);

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

            StateValidator.Validate(enrollment);

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

            if (!wasOnWaitingList)
            {
                PromoteFromWaitingList(activityId, cancellationToken);
            }

            await _db.SaveChangesAsync(cancellationToken);
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

            EnrollmentValidator.ValidateAnswers(providedAnswers, activity.SpecificationQuestions);

            _db.SpecificationAnswers.RemoveRange(enrollment.SpecificationAnswers);

            enrollment.SpecificationAnswers = providedAnswers.Select(a => new SpecificationAnswer
            {
                SpecificationQuestionId = a.QuestionId,
                Answer = a.Answer,
                MemberId = memberId,
                Enrollment = enrollment
            }).ToList();

            StateValidator.Validate(enrollment);

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
        ArgumentNullException.ThrowIfNull(patchDoc);

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
        StateValidator.Validate(enrollment);

        await _db.SaveChangesAsync(cancellationToken);
    }

    public void PromoteFromWaitingList(uint activityId, int numberToPromote, CancellationToken ct)
    {
        var next = _db.Enrollments
            .Include(e => e.Member)
            .Include(e => e.Activity)
            .Where(e => e.ActivityId == activityId && e.IsOnWaitingList && TargetAudienceHelper.IsMemberInTargetAudience(e.Member, e.Activity.AllowedAudience))
            .OrderBy(e => e.RegisteredOn)
            .Take(numberToPromote)
            .ToList();

        foreach (var enrollment in next)
        {
            enrollment.IsOnWaitingList = false;
        }
    }

    public void PromoteFromWaitingList(uint activityId, CancellationToken ct)
    {
        PromoteFromWaitingList(activityId, 1, ct);
    }
}