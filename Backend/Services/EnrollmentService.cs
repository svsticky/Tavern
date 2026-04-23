using Backend.Controllers.DTOs;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models;
using Backend.Models.Domain;
using Backend.Projections;
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

    public async Task<IEnumerable<EnrollmentResponseDTO>> GetEnrollments(GetEnrollmentsDTO dto, Guid userId, CancellationToken cancellationToken)
    {
        bool isBoard = _permissionService.IsBoardOrCandidateBoardMember(userId);

        if (dto.FromMemberId == null || dto.FromMemberId != userId)
            _permissionService.EnsureBoardOrCandidateBoardMember(userId);

        var enrollments = await _db.Enrollments
            .Include(e => e.Activity)
            .Include(e => e.Member)
            .Include(e => e.SpecificationAnswers)
                .ThenInclude(sa => sa.Question)
            .AsNoTracking()
            .Filter(dto)
            .ToListAsync(cancellationToken);

        return enrollments.Select(e => EnrollmentProjections.ToDto(userId, isBoard).Compile()(e));
    }

    public async Task<EnrollmentResponseDTO?> GetEnrollment(EnrollmentKeyDTO dto, Guid userId, CancellationToken cancellationToken)
    {
        bool isBoard = _permissionService.IsBoardOrCandidateBoardMember(userId);

        if (dto.MemberId != userId)
            _permissionService.EnsureBoardOrCandidateBoardMember(userId);

        var enrollment = await _db.Enrollments
            .Include(e => e.Activity)
            .Include(e => e.Member)
            .Include(e => e.SpecificationAnswers)
                .ThenInclude(sa => sa.Question)
            .FirstOrDefaultAsync(e => e.ActivityId == dto.ActivityId && e.MemberId == dto.MemberId, cancellationToken);
        
        if (enrollment == null)
            return null;

        return EnrollmentProjections.ToDto(userId, isBoard).Compile()(enrollment);
    }

    public async Task<EnrollmentResponseDTO> CreateEnrollment(PostEnrollmentDTO dto, Guid userId, CancellationToken cancellationToken)
    {
        using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        
        if(dto.MemberId != userId)
            _permissionService.EnsureBoardOrCandidateBoardMember(userId);

        try
        {
            // Get member and check if they are allowed to enroll in activities
            var member = await _db.Members.Include(m => m.StudyEnrollments).FirstOrDefaultAsync(m => m.Id == dto.MemberId, cancellationToken);

            if (member == null)
                throw new KeyNotFoundException("Member not found.");

            if (!_paymentValidationService.HasPaidMembershipPayment(member.Id))
                throw new ArgumentException("Member does not have a paid membership payment.");

            if (member.Suspended)
                throw new ArgumentException("Member is suspended and cannot enroll in activities.");

            if (member.StudyEnrollments.All(se => se.CompletionDate != null && se.CompletionDate <= DateTime.UtcNow) && !member.Gratie)
                throw new ArgumentException("Member should be enrolled in a study or be Gratie to enroll in activities.");

            // TO DO: Set all this in validator

            // Get activity and validate if enrollment is possible
            var activity = await GetActivityWithQuestionsAndEnrollmentsOrThrow(dto.ActivityId, cancellationToken);

            bool isBoardMember = _permissionService.IsBoardOrCandidateBoardMember(member.Id);

            if (activity.Enrollments.Any(e => e.MemberId == dto.MemberId))
                throw new ArgumentException("Member is already enrolled (or on waiting list).");

            if (!isBoardMember)
            {
                await EnsureActivityEnrollmentsCanBeChanged(activity, isBoardMember, cancellationToken);
                
                if (!TargetAudienceHelper.IsMemberInTargetAudience(member, activity.AllowedAudience))
                    throw new UnauthorizedAccessException("Member is not in the target audience for this activity.");
            }

            // Validate provided answers
            var providedAnswers = dto.SpecificationAnswers ?? new List<PostSpecificationAnswerDTO>();

            EnrollmentValidator.ValidateAnswers(providedAnswers, activity.SpecificationQuestions, isBoardMember);

            // Determine if enrollment should be on waiting list
            int currentParticipants = activity.Enrollments.Count(e => !e.IsOnWaitingList);

            bool shouldBeOnWaitingList =
                activity.ParticipantLimit.HasValue &&
                currentParticipants >= activity.ParticipantLimit.Value;

            var enrollment = BuildEnrollment(dto, activity, shouldBeOnWaitingList, providedAnswers);

            StateValidator.Validate(enrollment);

            _db.Enrollments.Add(enrollment);
            await _db.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return EnrollmentProjections.ToDto(userId, isBoardMember).Compile()(enrollment);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task DeleteEnrollment(EnrollmentKeyDTO dto, Guid userId, CancellationToken cancellationToken)
    {
        if(dto.MemberId != userId)
            _permissionService.EnsureBoardOrCandidateBoardMember(userId);
        
        using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var enrollment = await _db.Enrollments
                .Include(e => e.SpecificationAnswers)
                .Include(e => e.Activity)
                .ThenInclude(a => a.Enrollments)
                .FirstOrDefaultAsync(e => e.ActivityId == dto.ActivityId && e.MemberId == dto.MemberId, cancellationToken);

            if (enrollment == null)
                throw new KeyNotFoundException();

            // Determine if activity enrollments can be changed 
            bool isBoardMember = _permissionService.IsBoardOrCandidateBoardMember(dto.MemberId);

            if (!isBoardMember)
            {
                await EnsureActivityEnrollmentsCanBeChanged(enrollment.Activity, isBoardMember, cancellationToken);
            }

            // If the enrollment is not on the waiting list, we need to promote the next in line after deletion
            bool wasOnWaitingList = enrollment.IsOnWaitingList;

            _db.SpecificationAnswers.RemoveRange(enrollment.SpecificationAnswers);
            _db.Enrollments.Remove(enrollment);

            if (!wasOnWaitingList)
            {
                PromoteFromWaitingList(dto.ActivityId, cancellationToken);
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

    public async Task UpdateEnrollment(PostEnrollmentDTO dto, Guid userId,CancellationToken cancellationToken)
    {
        using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // Get enrollment
            var enrollment = await _db.Enrollments
                .Include(e => e.SpecificationAnswers)
                .FirstOrDefaultAsync(e => e.ActivityId == dto.ActivityId && e.MemberId == dto.MemberId, cancellationToken);

            if (enrollment == null)
                throw new KeyNotFoundException("Enrollment not found.");
            
            // Get activity and validate
            var activity = await GetActivityWithQuestionsOrThrow(dto.ActivityId, cancellationToken);

            // Determine if activity enrollments can be changed
            bool isBoard = _permissionService.IsBoardOrCandidateBoardMember(userId);

            if (!isBoard)
            {
                await EnsureActivityEnrollmentsCanBeChanged(activity, isBoard, cancellationToken);
                
                if(userId != enrollment.MemberId)
                    throw new UnauthorizedAccessException("Members can only update their own enrollments.");
            }
            
            // Get and validate provided answers
            var providedAnswers = dto.SpecificationAnswers ?? new List<PostSpecificationAnswerDTO>();
            
            EnrollmentValidator.ValidateAnswers(providedAnswers, activity.SpecificationQuestions, isBoard);

            // Remove old answers and add new ones
            _db.SpecificationAnswers.RemoveRange(enrollment.SpecificationAnswers);

            // Add new answers to enrollment
            enrollment.SpecificationAnswers = BuildSpecificationAnswers(providedAnswers, dto.MemberId, enrollment);

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

    public async Task PatchEnrollment(EnrollmentKeyDTO dto, JsonPatchDocument<Enrollment> patchDoc, Guid userId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(patchDoc);

        if (patchDoc.Operations.Any(op =>
            op.path.Equals("/activityid", StringComparison.OrdinalIgnoreCase) ||
            op.path.Equals("/memberid", StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException("Cannot change ActivityId or MemberId.");
        }
        
        // Get enrollment
        var enrollment = await _db.Enrollments
            .FirstOrDefaultAsync(e => e.ActivityId == dto.ActivityId && e.MemberId == dto.MemberId, cancellationToken);

        if (enrollment == null)
            throw new KeyNotFoundException();

        // Determine if activity enrollments can be changed
        bool isBoardMember = _permissionService.IsBoardOrCandidateBoardMember(userId);

        if(!isBoardMember)
        {
            await EnsureActivityEnrollmentsCanBeChanged(enrollment.Activity, isBoardMember, cancellationToken);
            
            if(userId != enrollment.MemberId)
                throw new UnauthorizedAccessException("Members can only update their own enrollments.");
        }

        // Apply patch and validate
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

    private async Task<Activity> GetActivityWithQuestionsAndEnrollmentsOrThrow(uint activityId, CancellationToken cancellationToken)
    {
        var activity = await _db.Activities
            .Include(a => a.SpecificationQuestions)
            .Include(a => a.Enrollments)
            .FirstOrDefaultAsync(a => a.Id == activityId, cancellationToken);

        return activity ?? throw new KeyNotFoundException("Activity not found.");
    }

    private async Task<Activity> GetActivityWithQuestionsOrThrow(uint activityId, CancellationToken cancellationToken)
    {
        var activity = await _db.Activities
            .Include(a => a.SpecificationQuestions)
            .FirstOrDefaultAsync(a => a.Id == activityId, cancellationToken);

        return activity ?? throw new KeyNotFoundException("Activity not found.");
    }

    private static Enrollment BuildEnrollment(
        PostEnrollmentDTO dto,
        Activity activity,
        bool isOnWaitingList,
        IEnumerable<PostSpecificationAnswerDTO> providedAnswers)
    {
        return new Enrollment
        {
            ActivityId = dto.ActivityId,
            MemberId = dto.MemberId,
            Price = activity.Price,
            RegisteredOn = DateTime.UtcNow,
            IsOnWaitingList = isOnWaitingList,
            SpecificationAnswers = providedAnswers.Select(a => new SpecificationAnswer
            {
                SpecificationQuestionId = a.QuestionId,
                Answer = a.Answer,
                MemberId = dto.MemberId
            }).ToList()
        };
    }

    private static List<SpecificationAnswer> BuildSpecificationAnswers(
        IEnumerable<PostSpecificationAnswerDTO> providedAnswers,
        Guid memberId,
        Enrollment enrollment)
    {
        return providedAnswers.Select(a => new SpecificationAnswer
        {
            SpecificationQuestionId = a.QuestionId,
            Answer = a.Answer,
            MemberId = memberId,
            Enrollment = enrollment
        }).ToList();
    }
}
