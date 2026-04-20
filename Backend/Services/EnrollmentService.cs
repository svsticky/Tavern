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
        var query = _db.Enrollments.Include(e => e.Activity).AsQueryable();

        if (memberId.HasValue)
        {
            query = query.Where(e => e.MemberId == memberId.Value);
        }

        // To do: if member id == null, ensure board or candidate board
        // to do: project to dto with only necessary info instead of including everything and returning the full enrollment objects (which can be quite heavy with all the specification answers)
        // It should contain activity with basic info

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<Enrollment?> GetEnrollment(uint activityId, Guid memberId, CancellationToken cancellationToken)
    {
        // To do: project to dto with only necessary info instead of including everything and returning the full enrollment objects (which can be quite heavy with all the specification answers). It should contain activity with basic info
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

            if (!isBoardMember)
            {
                await EnsureActivityEnrollmentsCanBeChanged(activity, isBoardMember, cancellationToken);
                
                if (!TargetAudienceHelper.IsMemberInTargetAudience(member, activity.AllowedAudience))
                    throw new UnauthorizedAccessException("Member is not in the target audience for this activity.");
            }

            if (activity.Enrollments.Any(e => e.MemberId == dto.MemberId))
                throw new ArgumentException("Member is already enrolled (or on waiting list).");

            var providedAnswers = dto.SpecificationAnswers ?? new List<PostSpecificationAnswerDTO>();

            var mandatoryQuestionIds = activity.SpecificationQuestions
                .Where(q => q.IsMandatory)
                .Select(q => q.Id)
                .ToList();

            bool isBoard = _permissionService.IsBoardOrCandidateBoardMember(userId);

            EnrollmentValidator.ValidateAnswers(providedAnswers, activity.SpecificationQuestions, isBoard);

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
            bool isBoardMember = _permissionService.IsBoardOrCandidateBoardMember(memberId);

            // to do: if not board and unenroll deadline is over: don't accept it (and remove the check for enrollmentdeadline)

            if (!isBoardMember)
            {
                await EnsureActivityEnrollmentsCanBeChanged(enrollment.Activity, isBoardMember, cancellationToken);
                
                if(memberId != enrollment.MemberId)
                    throw new UnauthorizedAccessException("Members can only delete their own enrollments.");
            }

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

            bool isBoard = _permissionService.IsBoardOrCandidateBoardMember(memberId);

            if (!isBoard)
            {
                await EnsureActivityEnrollmentsCanBeChanged(activity, isBoard, cancellationToken);
                
                if(memberId != enrollment.MemberId)
                    throw new UnauthorizedAccessException("Members can only update their own enrollments.");
            }
            
            var providedAnswers = dto.SpecificationAnswers ?? new List<PostSpecificationAnswerDTO>();
            
            EnrollmentValidator.ValidateAnswers(providedAnswers, activity.SpecificationQuestions, isBoard);

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

        bool isBoardMember = _permissionService.IsBoardOrCandidateBoardMember(memberId);

        if(!isBoardMember)
        {
            await EnsureActivityEnrollmentsCanBeChanged(enrollment.Activity, isBoardMember, cancellationToken);
            
            if(memberId != enrollment.MemberId)
                throw new UnauthorizedAccessException("Members can only update their own enrollments.");
        }

        patchDoc.ApplyTo(enrollment);
        StateValidator.Validate(enrollment);

        await _db.SaveChangesAsync(cancellationToken);
    }

    public void PromoteFromWaitingList(uint activityId, int numberToPromote, CancellationToken ct)
    {
        var next = _db.Enrollments
            .Include(e => e.Member)
            .Include(e => e.Activity)
            .Where(e => e.ActivityId == activityId && e.IsOnWaitingList)
            .OrderBy(e => e.RegisteredOn)
            .AsEnumerable()
            .Where(e => TargetAudienceHelper.IsMemberInTargetAudience(e.Member, e.Activity.AllowedAudience))
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

    private async Task EnsureActivityEnrollmentsCanBeChanged(Activity activity, bool isBoardMember, CancellationToken cancellationToken)
    {
        if (!activity.ShowInKoala)
        {
            throw new UnauthorizedAccessException("Activity is not visible for enrollment.");
        }

        if (!activity.IsEnrollable)
        {
            if(activity.EnrollOpenDate != null && activity.EnrollOpenDate <= DateTimeOffset.UtcNow)
            {
                activity.IsEnrollable = true;
                activity.EnrollOpenDate = null;
                await _db.SaveChangesAsync(cancellationToken);
            }
            else
            {
                throw new UnauthorizedAccessException("Activity is not open for enrollment.");
            }
        }

        var enrollmentDeadline = activity.EnrollmentDeadline ?? activity.DateTimeEnd;
        if(enrollmentDeadline < DateTime.UtcNow && !isBoardMember)
            throw new UnauthorizedAccessException("Enrollment deadline has passed.");
    }
}