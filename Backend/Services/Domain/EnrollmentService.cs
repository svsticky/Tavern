using Backend.Controllers.DTOs;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models;
using Backend.Models.Domain;
using Backend.QueryExtensions;
using Backend.Services.MailServices;
using Backend.Validators;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services.Domain;

/// <summary>
/// Implements enrollment workflows, including waiting-list promotion.
/// </summary>
public class EnrollmentService : IEnrollmentService
{
    private readonly PostgresDbContext _db;
    private readonly IPermissionService _permissionService;
    private readonly IPaymentValidationService _paymentValidationService;
    private readonly AbstractMailService _mailService;
    private readonly ILogger<EnrollmentService> _logger;

    /// <summary>
    /// Initializes a new instance of the EnrollmentService class with the specified database context, permission service, payment validation service, mail service, and logger. The constructor sets up the necessary dependencies for managing enrollments, allowing the service to interact with the database for enrollment operations, perform permission checks to ensure that only authorized users can manage enrollments, validate payments when necessary, send emails related to enrollment actions, and log important events and errors that occur during enrollment management for monitoring and debugging purposes.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="permissionService">The permission service.</param>
    /// <param name="paymentValidationService">The payment validation service.</param>
    /// <param name="mailService">The mail service.</param>
    /// <param name="logger">The logger.</param>
    public EnrollmentService(
        PostgresDbContext db,
        IPermissionService permissionService,
        IPaymentValidationService paymentValidationService,
        AbstractMailService mailService,
        ILogger<EnrollmentService> logger)
    {
        _db = db;
        _permissionService = permissionService;
        _paymentValidationService = paymentValidationService;
        _mailService = mailService;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<EnrollmentResponseDTO>> GetEnrollments(GetEnrollmentsDTO dto, Guid userId, CancellationToken cancellationToken)
    {
        bool isBoard = _permissionService.IsBoardOrCandidateBoardMember(userId);
        bool hasViewMembers = _permissionService.HasPermissionOrBoard(userId, Permission.ViewMembers);
        bool hasViewFinances = _permissionService.HasPermissionOrBoard(userId, Permission.ViewFinances);

        if (dto.FromMemberId == null || dto.FromMemberId != userId)
            _permissionService.EnsureBoardOrCandidateBoardMember(userId);

        var enrollments = await _db.Enrollments
            .Include(e => e.Activity)
            .Include(e => e.Member)
            .Filter(dto)
            .ToListAsync(cancellationToken);

        return enrollments.Select(e => EnrollmentResponseDTO.ToDto(userId, hasViewMembers, hasViewFinances, isBoard).Compile()(e));
    }

    /// <inheritdoc />
    public async Task<EnrollmentResponseDTO?> GetEnrollment(uint activityId, Guid enrolledUser, Guid userId, CancellationToken cancellationToken)
    {
        bool isBoard = _permissionService.IsBoardOrCandidateBoardMember(userId);
        bool hasViewMembers = _permissionService.HasPermissionOrBoard(userId, Permission.ViewMembers);
        bool hasViewFinances = _permissionService.HasPermissionOrBoard(userId, Permission.ViewFinances);

        if (enrolledUser != userId)
            _permissionService.EnsureBoardOrCandidateBoardMember(userId);

        var enrollment = await _db.Enrollments
            .Include(e => e.Activity)
            .Include(e => e.Member)
            .Include(e => e.SpecificationAnswers)
                .ThenInclude(sa => sa.Question)
            .FirstOrDefaultAsync(e => e.ActivityId == activityId && e.MemberId == enrolledUser, cancellationToken);

        if (enrollment == null)
            return null;

        return EnrollmentResponseDTO.ToDto(userId, hasViewMembers, hasViewFinances, isBoard).Compile()(enrollment);
    }

    /// <inheritdoc />
    public async Task<EnrollmentResponseDTO> CreateEnrollment(PostEnrollmentDTO dto, Guid userId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating enrollment for member {MemberId} in activity {ActivityId}. Requested by {UserId}.", dto.MemberId, dto.ActivityId, userId);
        using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        if (dto.MemberId != userId)
            _permissionService.EnsureBoardOrCandidateBoardMember(userId);

        bool isBoardMember = _permissionService.IsBoardOrCandidateBoardMember(userId);

        try
        {
            // Get member and check if they are allowed to enroll in activities
            var member = await _db.Members.Include(m => m.StudyEnrollments).FirstOrDefaultAsync(m => m.Id == dto.MemberId, cancellationToken);

            if (member == null)
                throw new KeyNotFoundException("Member not found.");

            // Get activity and validate if enrollment is possible
            var activity = await GetActivityWithQuestionsAndEnrollmentsOrThrow(dto.ActivityId, cancellationToken);

            EnrollmentValidator.ValidateEnrollment(dto.SpecificationAnswers, member, activity, isBoardMember, _paymentValidationService);

            // Determine if enrollment should be on waiting list
            int currentParticipants = activity.Enrollments.Count(e => !e.IsOnWaitingList);

            bool shouldBeOnWaitingList =
                activity.ParticipantLimit.HasValue &&
                currentParticipants >= activity.ParticipantLimit.Value || !TargetAudienceHelper.IsMemberInTargetAudience(member, activity.AllowedAudience);

            if (!isBoardMember)
            {
                await EnsureActivityEnrollmentsOpen(activity, isBoardMember, cancellationToken);
            }

            var enrollment = BuildEnrollment(dto, activity, shouldBeOnWaitingList, dto.SpecificationAnswers ?? new List<PostSpecificationAnswerDTO>());

            StateValidator.Validate(enrollment);

            _db.Enrollments.Add(enrollment);
            await _db.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Failed creating enrollment for member {MemberId} in activity {ActivityId}.", dto.MemberId, dto.ActivityId);
            throw;
        }

        var savedEnrollment = await _db.Enrollments
            .Include(e => e.Member)
            .Include(e => e.Activity)
            .Include(e => e.SpecificationAnswers)
                .ThenInclude(sa => sa.Question)
            .FirstAsync(e => e.ActivityId == dto.ActivityId && e.MemberId == dto.MemberId, cancellationToken);

        return EnrollmentResponseDTO.ToDto(userId, _permissionService.HasPermissionOrBoard(userId, Permission.ViewMembers), _permissionService.HasPermissionOrBoard(userId, Permission.ViewFinances), isBoardMember).Compile()(savedEnrollment);
    }

    /// <inheritdoc />
    public async Task DeleteEnrollment(uint activityId, Guid enrolledUser, Guid userId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting enrollment for member {MemberId} in activity {ActivityId}. Requested by {UserId}.", enrolledUser, activityId, userId);
        if (enrolledUser != userId)
            _permissionService.EnsureBoardOrCandidateBoardMember(userId);

        using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var enrollment = await _db.Enrollments
                .Include(e => e.SpecificationAnswers)
                .Include(e => e.Activity)
                .ThenInclude(a => a.Enrollments)
                .FirstOrDefaultAsync(e => e.ActivityId == activityId && e.MemberId == enrolledUser, cancellationToken);

            if (enrollment == null)
                throw new KeyNotFoundException();

            // Determine if activity enrollments can be changed 
            bool isBoardMember = _permissionService.IsBoardOrCandidateBoardMember(userId);

            EnsureActivityUnenrollmentOpen(enrollment.Activity, isBoardMember);

            // If the enrollment is not on the waiting list, we need to promote the next in line after deletion
            bool wasOnWaitingList = enrollment.IsOnWaitingList;

            _db.SpecificationAnswers.RemoveRange(enrollment.SpecificationAnswers);
            _db.Enrollments.Remove(enrollment);

            Enrollment? promotedEnrollment = null;
            if (!wasOnWaitingList)
            {
                promotedEnrollment = await PromoteFromWaitingList(activityId, cancellationToken);
            }

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            if (promotedEnrollment != null)
            {
                try
                {
                    await _mailService.SendEnrollmentPromotionEmail(promotedEnrollment);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed sending enrollment promotion email to member {MemberId} for activity {ActivityId}.", promotedEnrollment.MemberId, promotedEnrollment.ActivityId);
                }
            }
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Failed deleting enrollment for member {MemberId} in activity {ActivityId}.", enrolledUser, activityId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task UpdateEnrollment(PostEnrollmentDTO dto, Guid userId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Updating enrollment for member {MemberId} in activity {ActivityId}. Requested by {UserId}.", dto.MemberId, dto.ActivityId, userId);
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
                await EnsureActivityEnrollmentsOpen(activity, isBoard, cancellationToken);

                if (userId != enrollment.MemberId)
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
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Failed updating enrollment for member {MemberId} in activity {ActivityId}.", dto.MemberId, dto.ActivityId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task PatchEnrollment(uint activityId, Guid memberId, JsonPatchDocument<Enrollment> patchDoc, Guid userId, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Patching enrollment for member {MemberId} in activity {ActivityId}. Requested by {UserId}.", memberId, activityId, userId);
        ArgumentNullException.ThrowIfNull(patchDoc);

        // Determine if activity enrollments can be changed
        bool isBoardMember = _permissionService.IsBoardOrCandidateBoardMember(userId);

        // Board members can patch any field (e.g. moving someone off the waiting list); everyone else
        // is restricted to Enrollment.AllowedFields.
        if (!isBoardMember && patchDoc.Operations.Any(op => !Enrollment.AllowedFields.Contains(op.path)))
        {
            throw new ArgumentException("Cannot change ActivityId, MemberId, Price, RegisteredOn or IsOnWaitingList.");
        }

        // Get enrollment
        var enrollment = await _db.Enrollments
            .FirstOrDefaultAsync(e => e.ActivityId == activityId && e.MemberId == memberId, cancellationToken);

        if (enrollment == null)
            throw new KeyNotFoundException();

        if (!isBoardMember)
        {
            await EnsureActivityEnrollmentsOpen(enrollment.Activity, isBoardMember, cancellationToken);

            if (userId != enrollment.MemberId)
                throw new UnauthorizedAccessException("Members can only update their own enrollments.");
        }

        // Apply patch and validate
        patchDoc.ApplyTo(enrollment);
        StateValidator.Validate(enrollment);

        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Enrollment>> PromoteFromWaitingList(uint activityId, int numberToPromote, CancellationToken ct)
    {
        var next = await _db.Enrollments
            .Include(e => e.Member)
                .ThenInclude(m => m.StudyEnrollments)
                    .ThenInclude(se => se.Study)
            .Include(e => e.Member)
                .ThenInclude(m => m.GroupMemberships)
            .Include(e => e.Activity)
            .Where(e => e.ActivityId == activityId && e.IsOnWaitingList)
            .OrderBy(e => e.RegisteredOn)
            .ToListAsync(ct);

        var toPromote = next
            .Where(e => TargetAudienceHelper.IsMemberInTargetAudience(e.Member, e.Activity.AllowedAudience))
            .Take(numberToPromote);

        foreach (var enrollment in toPromote)
        {
            enrollment.IsOnWaitingList = false;
        }

        return toPromote;
    }

    /// <inheritdoc />
    public async Task<Enrollment?> PromoteFromWaitingList(uint activityId, CancellationToken ct)
    {
        return (await PromoteFromWaitingList(activityId, 1, ct)).FirstOrDefault();
    }

    private async Task EnsureActivityEnrollmentsOpen(Activity activity, bool isBoardMember, CancellationToken cancellationToken)
    {
        if (!activity.ShowInKoala)
        {
            throw new UnauthorizedAccessException("Activity is not visible for enrollment.");
        }

        if (!activity.IsEnrollable)
        {
            if (activity.EnrollOpenDate != null && activity.EnrollOpenDate <= DateTimeOffset.UtcNow)
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
        if (enrollmentDeadline < DateTime.UtcNow && !isBoardMember)
            throw new UnauthorizedAccessException("Enrollment deadline has passed.");
    }

    private void EnsureActivityUnenrollmentOpen(Activity activity, bool isBoardMember)
    {
        if (isBoardMember) return;

        if (activity.IsEnrollable == false)
        {
            throw new UnauthorizedAccessException("Activity is not open for enrollment.");
        }

        if (activity.UnenrollmentDeadline == null)
        {
            if (activity.DateTimeStart < DateTime.UtcNow)
            {
                throw new UnauthorizedAccessException("Activity has already started.");
            }
        }
        else if (activity.UnenrollmentDeadline < DateTime.UtcNow)
        {
            throw new UnauthorizedAccessException("Unenrollment deadline has passed.");
        }
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
