using Backend.Controllers.DTOs;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models;
using Backend.Models.Domain;
using Backend.QueryExtensions;
using Backend.Services.MailServices;
using Backend.Utils.DateTime;
using Backend.Validators;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Text;

namespace Backend.Services.Domain;

/// <summary>
/// Implements activity management, poster handling, and enrollment exports.
/// </summary>
public class ActivityService : IActivityService
{
    private readonly PostgresDbContext _db;
    private readonly IStorageService _storageService;
    private readonly IFileCompressService _fileCompressor;
    private readonly IPermissionService _permissionService;
    private readonly IEnrollmentService _enrollmentService;
    private readonly AbstractMailService _mailService;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<ActivityService> _logger;

    private readonly string[] _restrictedForEveryonePaths = new[] { "/id", "/posterFileName", "/posterPath", };

    /// <summary>
    /// Initializes a new instance of the ActivityService class with the specified database context, storage service, file compressor, permission service, enrollment service, mail service, and logger. The constructor sets up the necessary dependencies for managing activities, including database access for activity data, storage service for handling activity posters, file compressor for optimizing poster files, permission service for enforcing access control on activity operations, enrollment service for managing enrollments related to activities, mail service for sending notifications about activity-related events, and logging for monitoring activity management operations and troubleshooting any issues that may arise.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="storageService">The storage service.</param>
    /// <param name="fileCompressor">The file compressor.</param>
    /// <param name="permissionService">The permission service.</param>
    /// <param name="enrollmentService">The enrollment service.</param>
    /// <param name="mailService">The mail service.</param>
    /// <param name="memoryCache">The memory cache service.</param>
    /// <param name="logger">The logger.</param>
    public ActivityService(
        PostgresDbContext db,
        IStorageService storageService,
        IFileCompressService fileCompressor,
        IPermissionService permissionService,
        IEnrollmentService enrollmentService,
        AbstractMailService mailService,
        IMemoryCache memoryCache,
        ILogger<ActivityService> logger)
    {
        _db = db;
        _storageService = storageService;
        _fileCompressor = fileCompressor;
        _permissionService = permissionService;
        _enrollmentService = enrollmentService;
        _mailService = mailService;
        _memoryCache = memoryCache;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ActivityResponseDTO>> GetActivities(Guid? userId, GetActivitiesDTO dto)
    {
        bool isBoard = userId.HasValue && _permissionService.IsBoardOrCandidateBoardMember(userId.Value);
        bool hasViewFinances = userId.HasValue && _permissionService.HasPermissionOrBoard(userId.Value, Permission.ViewFinances);
        bool hasViewMembers = userId.HasValue && _permissionService.HasPermissionOrBoard(userId.Value, Permission.ViewMembers);

        // Only board members or ViewPastActivities holders can see past activities; activities not
        // shown in Koala/website are filtered separately below based on organizer/permission checks.
        if (dto.IncludePast && (!userId.HasValue || userId.Value != dto.UserId))
            _permissionService.EnsurePermission(userId ?? throw new UnauthorizedAccessException("Authentication required."), Permission.ViewPastActivities);

        var currentCommitteeYear = YearUtils.GetYearForDate(System.DateTime.UtcNow, YearUtils.CommitteeCreationDate);
        var userGroupIds = userId.HasValue
            ? await _db.GroupMemberships
                .Where(gm => gm.MemberId == userId.Value && gm.MembershipYear == currentCommitteeYear)
                .Select(gm => gm.GroupId)
                .ToListAsync()
            : new List<uint>();

        // Filter activities based on the provided criteria and the user's permissions
        var activities = await _db.Activities
            .Include(a => a.Enrollments)
            .Include(a => a.SpecificationQuestions)
            .AsNoTracking()
            .Filter(dto, isBoard, userGroupIds, userId.HasValue)
            .ApplyPaging(dto)
            .ToListAsync();

        return activities.Select(a => ActivityResponseDTO.ToDto(userId ?? Guid.Empty, hasViewFinances, hasViewMembers, isBoard).Compile()(a));
    }

    /// <inheritdoc />
    public async Task<ActivityResponseDTO?> GetActivity(Guid userId, uint id)
    {
        bool isBoard = _permissionService.IsBoardOrCandidateBoardMember(userId);
        bool hasViewFinances = _permissionService.HasPermissionOrBoard(userId, Permission.ViewFinances);
        bool hasViewMembers = _permissionService.HasPermissionOrBoard(userId, Permission.ViewMembers);

        var activity = await _db.Activities
            .Include(a => a.SpecificationQuestions)
            .Include(a => a.Enrollments)
                .ThenInclude(e => e.Member)
            .Include(a => a.Enrollments)
                .ThenInclude(e => e.SpecificationAnswers)
                    .ThenInclude(sa => sa.Question)
                        .ThenInclude(q => q.Activity)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (activity == null)
            return null;

        if (!activity.IsEnrollable && activity.EnrollOpenDate != null && activity.EnrollOpenDate <= DateTimeOffset.UtcNow)
        {
            activity.IsEnrollable = true;
            activity.EnrollOpenDate = null;
            await _db.SaveChangesAsync();
        }

        if (activity.DateTimeEnd.UtcDateTime < DateTime.UtcNow && !isBoard)
            throw new UnauthorizedAccessException();

        if (!activity.ShowInKoala && !isBoard && (activity.OrganizerId == null || !_permissionService.IsInGroupInCurrentYear(userId, activity.OrganizerId.Value)))
            throw new UnauthorizedAccessException();

        return ActivityResponseDTO.ToDto(userId, hasViewFinances, hasViewMembers, isBoard).Compile()(activity);
    }

    /// <inheritdoc />
    public async Task<Activity> CreateActivity(Guid userId, PostActivityDTO dto)
    {
        _logger.LogInformation("Creating activity {ActivityName} by user {UserId}.", dto.Name, userId);
        ActivityValidator.ValidateRequest(dto, userId, _permissionService);
        ActivityValidator.NormalizeCreateRequest(dto);

        using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            // Parse questions and create activity
            var questions = ActivityValidator.ParseCreateQuestions(dto.SpecificationQuestionsJson);
            var activity = BuildActivity(dto, questions);
            _db.Activities.Add(activity);

            StateValidator.Validate(activity);

            // Save poster
            await SavePosterIfProvided(activity, dto.Poster);

            // Enroll organizers
            var currentCommitteeYear = YearUtils.GetYearForDate(
                activity.DateTimeStart.UtcDateTime,
                YearUtils.CommitteeCreationDate);

            var organizerMembers = await _db.GroupMemberships
                .Where(gm =>
                    gm.GroupId == dto.OrganizerId &&
                    gm.MembershipYear == currentCommitteeYear)
                .Select(gm => gm.Member)
                .ToListAsync();

            foreach (var member in organizerMembers)
            {
                _db.Enrollments.Add(new Enrollment
                {
                    ActivityId = activity.Id,
                    Activity = activity,
                    Member = member,
                    Price = activity.Price,
                    IsOnWaitingList = false,
                    RegisteredOn = DateTime.UtcNow
                });
            }

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return activity;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed creating activity {ActivityName}.", dto.Name);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task DeleteActivity(Guid userId, uint id)
    {
        _permissionService.EnsureBoardOrCandidateBoardMember(userId);
        _logger.LogInformation("Deleting activity {ActivityId} by user {UserId}.", id, userId);

        var activity = await _db.Activities.FindAsync(id);
        if (activity == null)
            throw new KeyNotFoundException();

        if (activity.PosterPath != null)
        {
            await _storageService.DeleteFileAsync("posters", activity.PosterPath);
            _memoryCache.Remove($"poster-{activity.PosterPath}");
        }

        _db.Activities.Remove(activity);
        await _db.SaveChangesAsync();
    }

    /// <inheritdoc />
    public async Task PatchActivity(Guid userId, uint id, JsonPatchDocument<Activity> patchDoc, CancellationToken ct)
    {
        _logger.LogInformation("Patching activity {ActivityId} by user {UserId}.", id, userId);
        if (patchDoc == null)
            throw new ArgumentException();

        var activity = await _db.Activities.FindAsync(new object[] { id }, ct);
        if (activity == null)
            throw new KeyNotFoundException();

        if (patchDoc.Operations.Any(op => _restrictedForEveryonePaths.Contains(op.path, StringComparer.OrdinalIgnoreCase)))
            throw new UnauthorizedAccessException("You are not allowed to modify the id or poster properties of the activity.");

        bool isBoard = _permissionService.IsBoardOrCandidateBoardMember(userId);
        bool hasEditAll = _permissionService.HasPermission(userId, Permission.EditAllActivities);

        if (!isBoard && !hasEditAll)
        {
            bool hasEditForGroup = activity.OrganizerId.HasValue && _permissionService.HasPermission(userId, Permission.EditActivityForGroup, activity.OrganizerId.Value);
            bool hasManageFinances = _permissionService.HasPermission(userId, Permission.ManageFinances);

            bool isOnline = activity.ShowInKoala || activity.ShowOnWebsite || activity.EnrollOpenDate != null;
            bool isPast = activity.DateTimeEnd.UtcDateTime < DateTime.UtcNow;
            bool canEditForGroup = !isPast && !isOnline && hasEditForGroup;

            bool allOpsAuthorized = patchDoc.Operations.All(op =>
                (canEditForGroup && Activity.AllowedFields.Contains(op.path)) ||
                (hasManageFinances && Activity.FinanceAllowedFields.Contains(op.path)));

            if (!allOpsAuthorized)
                throw new UnauthorizedAccessException("You are not authorized to edit this activity.");
        }

        // Intercept and handle SpecificationQuestionsJson operations from the patch document
        var specQuestionsOp = patchDoc.Operations.FirstOrDefault(op =>
            op.path.Equals("/SpecificationQuestionsJson", StringComparison.OrdinalIgnoreCase));

        List<UpdateSpecificationQuestionDTO>? questionsToSync = null;
        if (specQuestionsOp != null)
        {
            var jsonString = specQuestionsOp.value?.ToString();
            questionsToSync = ActivityValidator.ParseUpdateQuestions(jsonString);
            patchDoc.Operations.Remove(specQuestionsOp);
        }

        using var transaction = await _db.Database.BeginTransactionAsync(ct);

        try
        {
            // save old data for processing waiting list and updating enrollment prices
            uint? oldLimit = activity.ParticipantLimit;
            decimal oldPrice = activity.Price;
            var oldAudience = activity.AllowedAudience;

            patchDoc.ApplyTo(activity, err =>
            {
                throw new ArgumentException(err.ErrorMessage);
            });


            if (activity.IsEnrollable)
            {
                activity.EnrollOpenDate = null;
            }

            ActivityValidator.ValidateTimeRange(activity.DateTimeStart, activity.DateTimeEnd);
            ActivityValidator.ValidateDeadlines(activity.DateTimeEnd, activity.EnrollmentDeadline, activity.UnenrollmentDeadline);
            StateValidator.Validate(activity);

            if (questionsToSync != null)
            {
                await SyncSpecificationQuestions(activity, questionsToSync);
            }

            IEnumerable<Enrollment> promotedEnrollments;

            // Update waiting list
            if (activity.ParticipantLimit == null || (oldLimit.HasValue && activity.ParticipantLimit > oldLimit)
                || activity.AllowedAudience != oldAudience)
            {
                promotedEnrollments = await ProcessWaitingList(id, activity.ParticipantLimit, ct);
            }
            else
            {
                promotedEnrollments = Enumerable.Empty<Enrollment>();
            }

            // Update enrollment prices if the price has changed
            if (oldPrice != activity.Price)
            {
                var enrollments = await _db.Enrollments
                    .Where(e => e.ActivityId == id && e.Price == oldPrice)
                    .ToListAsync(ct);

                foreach (var e in enrollments)
                    e.Price = activity.Price;
            }

            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);

            foreach (var enrollment in promotedEnrollments)
            {
                try
                {
                    await _mailService.SendEnrollmentPromotionEmail(enrollment);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed sending enrollment promotion email for enrollment {ActivityId} for {MemberId}.", enrollment.ActivityId, enrollment.MemberId);
                }
            }
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(ct);
            _logger.LogError(ex, "Failed patching activity {ActivityId}.", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task UploadPoster(Guid userId, uint id, IFormFile? poster)
    {
        _logger.LogInformation("Uploading poster for activity {ActivityId} by user {UserId}.", id, userId);
        var activity = await _db.Activities.FindAsync(id);
        if (activity == null)
            throw new KeyNotFoundException();

        // Uploading is only allowed if the activity is not online yet
        if (activity.ShowInKoala || activity.ShowOnWebsite || activity.EnrollOpenDate != null)
        {
            _permissionService.EnsureBoardOrCandidateBoardMember(userId);
        }

        // Validate poster if provided
        if (poster != null)
            ExtensionValidator.ValidatePosterExtension(poster);

        // Save old path for deletion after successful upload of the new poster
        string? oldPath = activity.PosterPath;

        using var transaction = await _db.Database.BeginTransactionAsync();

        try
        {
            if (poster != null)
            {
                await SavePoster(activity, poster);
            }
            else
            {
                activity.PosterPath = null;
                activity.PosterFileName = null;
            }

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            if (!string.IsNullOrEmpty(oldPath))
            {
                await _storageService.DeleteFileAsync("posters", oldPath);
                _memoryCache.Remove($"poster-{oldPath}");
            }
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed uploading poster for activity {ActivityId}.", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task UpdateActivity(Guid userId, uint id, PutActivityDTO dto)
    {
        _logger.LogInformation("Updating activity {ActivityId} by user {UserId}.", id, userId);
        var activity = await _db.Activities
            .Include(a => a.SpecificationQuestions)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (activity == null)
            throw new KeyNotFoundException();

        bool isBoard = _permissionService.IsBoardOrCandidateBoardMember(userId);
        bool hasEditAll = _permissionService.HasPermission(userId, Permission.EditAllActivities);

        if (!isBoard && !hasEditAll)
        {
            bool hasEditForGroup = activity.OrganizerId.HasValue && _permissionService.HasPermission(userId, Permission.EditActivityForGroup, activity.OrganizerId.Value);
            bool hasManageFinances = _permissionService.HasPermission(userId, Permission.ManageFinances);

            bool isOnline = activity.ShowInKoala || activity.ShowOnWebsite || activity.EnrollOpenDate != null;
            bool isPast = activity.DateTimeEnd.UtcDateTime < DateTime.UtcNow;
            bool canEditForGroup = !isPast && !isOnline && hasEditForGroup;

            if (!canEditForGroup && !hasManageFinances)
                throw new UnauthorizedAccessException("You are not authorized to edit this activity.");

            // Non-board, non-EditAllActivities organizers can't change these fields via PUT either
            // (finance fields excepted for ManageFinances holders). Silently keep them as they are
            // instead of rejecting the request when they differ - comparing and rejecting would let
            // someone guess a hidden value (e.g. VatRate) and learn whether they guessed right from
            // whether the request succeeds or fails.
            PreserveFieldsOutsideAllowedFields(activity, dto, hasManageFinances, canEditForGroup);
        }

        ActivityValidator.ValidateRequest(dto, userId, _permissionService);

        using var transaction = await _db.Database.BeginTransactionAsync();

        // Parse the specification questions from the JSON string in the DTO
        var questions = ActivityValidator.ParseUpdateQuestions(dto.SpecificationQuestionsJson);

        try
        {
            // Save old values for processing waiting list and updating enrollment prices after applying the update
            decimal oldPrice = activity.Price;
            uint? oldLimit = activity.ParticipantLimit;
            string? existingPosterPath = activity.PosterPath;
            var oldAudience = activity.AllowedAudience;

            ApplyUpdateDto(activity, dto);
            StateValidator.Validate(activity);

            // Sync specification questions
            await SyncSpecificationQuestions(activity, questions);

            // Update enrollment prices if the price has changed
            if (oldPrice != activity.Price)
            {
                var enrollmentsToUpdate = await _db.Enrollments
                    .Where(e => e.ActivityId == id && e.Price == oldPrice)
                    .ToListAsync();

                foreach (var enrollment in enrollmentsToUpdate)
                    enrollment.Price = activity.Price;
            }

            // Save poster if a new one is provided
            await SavePosterIfProvided(activity, dto.Poster);

            IEnumerable<Enrollment> promotedEnrollments;

            // Update waiting list if participant limit or allowed audience has changed
            if (activity.ParticipantLimit == null || (oldLimit.HasValue && activity.ParticipantLimit > oldLimit)
                || activity.AllowedAudience != oldAudience)
            {
                promotedEnrollments = await ProcessWaitingList(id, activity.ParticipantLimit, default);
            }
            else
            {
                promotedEnrollments = Enumerable.Empty<Enrollment>();
            }

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            foreach (var enrollment in promotedEnrollments)
            {
                try
                {
                    await _mailService.SendEnrollmentPromotionEmail(enrollment);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed sending enrollment promotion email for enrollment {ActivityId} for {MemberId}.", enrollment.ActivityId, enrollment.MemberId);
                }
            }

            // Delete old poster if a new one was uploaded and the activity had an existing poster
            if (existingPosterPath != null && dto.Poster != null)
            {
                await _storageService.DeleteFileAsync("posters", existingPosterPath);
                _memoryCache.Remove($"poster-{existingPosterPath}");
            }
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed updating activity {ActivityId}.", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<(Stream Stream, string ContentType, string? FileName)?> GetPoster(Guid? userId, uint id, bool download)
    {
        var activity = await _db.Activities.FindAsync(id);

        if (activity == null || string.IsNullOrEmpty(activity.PosterPath))
            return null;

        bool isPast = activity.DateTimeEnd.UtcDateTime < DateTime.UtcNow;

        // Only board members or candidate board members can access posters of past activities
        if (isPast)
            _permissionService.EnsureBoardOrCandidateBoardMember(userId ?? throw new UnauthorizedAccessException("Authentication required."));

        // If not authenticated, you can only see posters of activities that are shown on the website
        if (!userId.HasValue && !activity.ShowOnWebsite)
            throw new UnauthorizedAccessException("Authentication required.");

        // If authenticated, you can only see posters of activities that are shown in Koala or if you are a board member or a member of the organizer group
        if (userId.HasValue)
        {
            // Only board members or members of the organizer group can see posters of activities that are not shown in Koala
            if (!activity.ShowInKoala && (activity.OrganizerId == null || !_permissionService.IsInGroupInCurrentYear(userId.Value, activity.OrganizerId.Value)))
                _permissionService.EnsureBoardOrCandidateBoardMember(userId.Value);
        }

        // Get poster from cache if available, otherwise fetch from storage and cache it for future requests
        string cacheKey = $"poster-{activity.PosterPath}";
        if (_memoryCache.TryGetValue(cacheKey, out (byte[] bytes, string contentType) cached))
        {
            return (
                new MemoryStream(cached.bytes),
                cached.contentType,
                download ? activity.PosterFileName ?? "poster" : null
            );
        }

        // Get poster file from storage
        var file = await _storageService.GetFileAsync("posters", activity.PosterPath);

        if (file == null)
            return null;

        using var memoryStream = new MemoryStream();
        await file.Stream.CopyToAsync(memoryStream);
        byte[] bytes = memoryStream.ToArray();

        _memoryCache.Set(cacheKey, (bytes, file.ContentType), TimeSpan.FromHours(1));

        // If download is true, the file will be returned with a filename to trigger download in the frontend, otherwise it will be displayed in the browser if supported
        return (
            new MemoryStream(bytes),
            file.ContentType,
            download ? activity.PosterFileName ?? "poster" : null
        );
    }

    /// <inheritdoc />
    public async Task<(byte[] Content, string FileName)> GetEnrollmentsCsv(Guid userId, uint activityId, CancellationToken ct)
    {
        // Only board members can download the enrollments CSV
        _permissionService.EnsureBoardOrCandidateBoardMember(userId);

        // Get user preferred language for CSV header
        Member? member = await _db.Members.FirstOrDefaultAsync(m => m.Id == userId, ct);
        if (member == null)
            throw new UnauthorizedAccessException("User not found");
        Language language = member.PreferredLanguage;

        // Load activity with enrollments and specification questions
        var activity = await _db.Activities
            .Include(a => a.SpecificationQuestions)
            .Include(a => a.Enrollments)
                .ThenInclude(e => e.Member)
            .Include(a => a.Enrollments)
                .ThenInclude(e => e.SpecificationAnswers)
            .FirstOrDefaultAsync(a => a.Id == activityId, ct);

        // If activity is not found, throw an exception
        if (activity == null) throw new KeyNotFoundException("Activity not found");

        // Build CSV content
        var csv = BuildEnrollmentsCsv(language, activity);

        var fileName = $"Enrollments_{activity.Name}.csv";
        return (Encoding.UTF8.GetBytes(csv.ToString()), fileName);
    }

    private Activity BuildActivity(PostActivityDTO dto, List<SpecificationQuestionDTO> questions)
    {
        return new Activity
        {
            Name = dto.Name,
            Price = dto.Price,
            DutchDescription = dto.DutchDescription,
            EnglishDescription = dto.EnglishDescription,
            DateTimeStart = dto.DateTimeStart,
            DateTimeEnd = dto.DateTimeEnd,
            UnenrollmentDeadline = dto.UnenrollmentDeadline,
            EnrollmentDeadline = dto.EnrollmentDeadline,
            EnrollOpenDate = dto.EnrollOpenDate,
            Location = dto.Location,
            ParticipantLimit = dto.ParticipantLimit,
            OrganizerId = dto.OrganizerId,
            ShowInKoala = dto.ShowInKoala,
            ShowOnWebsite = dto.ShowOnWebsite,
            IsEnrollable = dto.IsEnrollable,
            AreParticipantsVisible = dto.AreParticipantsVisible,
            IsAdultOnly = dto.IsAdultOnly,
            IsWeeklyDrinks = dto.IsWeeklyDrinks,
            AllowedAudience = dto.AllowedAudience,
            VatRate = dto.VatRate,
            GLAccountId = dto.GLAccountId,
            CostCenterId = dto.CostCenterId,
            CostUnitId = dto.CostUnitId,
            SpecificationQuestions = questions.Select(q => new SpecificationQuestion
            {
                QuestionDutch = q.QuestionDutch,
                QuestionEnglish = q.QuestionEnglish,
                Type = q.Type,
                IsMandatory = q.IsMandatory,
                IsPublic = q.IsPublic,
                Options = q.Options != null ? string.Join(";", q.Options) : null
            }).ToList(),
            PaymentDeadline = dto.PaymentDeadline ?? dto.DateTimeStart.Date.AddDays(14)
        };
    }

    private async Task<IEnumerable<Enrollment>> ProcessWaitingList(uint activityId, uint? newLimit, CancellationToken ct)
    {
        // Get the number of current participants (excluding those on the waiting list)
        int currentParticipants = await _db.Enrollments
            .CountAsync(e => e.ActivityId == activityId && !e.IsOnWaitingList, ct);

        // Calculate how many people can be promoted from the waiting list based on the new participant limit
        int availableSpots = newLimit.HasValue
            ? (int)newLimit.Value - currentParticipants
            : int.MaxValue;

        if (availableSpots > 0)
        {
            // Promote people from the waiting list based on the available spots
            return await _enrollmentService.PromoteFromWaitingList(activityId, availableSpots, ct);
        }

        return Enumerable.Empty<Enrollment>();
    }

    private async Task SyncSpecificationQuestions(Activity activity, List<UpdateSpecificationQuestionDTO> dtoQuestions)
    {
        // Load existing questions from the database
        await _db.Entry(activity)
            .Collection(a => a.SpecificationQuestions)
            .LoadAsync();

        var existingQuestions = activity.SpecificationQuestions.ToList();

        foreach (var dto in dtoQuestions)
        {
            if (dto.Id.HasValue)
            {
                // If the DTO has an ID, find the existing question and update it
                var existing = existingQuestions.FirstOrDefault(q => q.Id == dto.Id.Value);
                if (existing == null)
                    throw new Exception($"SpecificationQuestion with id {dto.Id} not found.");

                ActivityValidator.MapSpecificationQuestion(existing, dto);
            }
            else
            {
                // If the DTO does not have an ID, it's a new question, so create it and add it to the activity
                var newQuestion = new SpecificationQuestion
                {
                    Activity = activity,
                    QuestionDutch = dto.QuestionDutch,
                    QuestionEnglish = dto.QuestionEnglish,
                    Type = dto.Type,
                    IsMandatory = dto.IsMandatory,
                    IsPublic = dto.IsPublic,
                    Options = dto.Options != null && dto.Options.Any()
                        ? string.Join(';', dto.Options)
                        : null
                };

                activity.SpecificationQuestions.Add(newQuestion);
            }
        }

        // Remove questions that are not in the DTO
        var dtoIds = dtoQuestions.Where(q => q.Id.HasValue).Select(q => q.Id!.Value).ToHashSet();
        var toRemove = existingQuestions.Where(q => !dtoIds.Contains(q.Id)).ToList();
        _db.SpecificationQuestions.RemoveRange(toRemove);
    }

    private async Task SavePosterIfProvided(Activity activity, IFormFile? poster)
    {
        if (poster == null)
            return;

        await SavePoster(activity, poster);
    }

    private async Task SavePoster(Activity activity, IFormFile poster)
    {
        var compressed = await _fileCompressor.CompressFileAsync(poster);
        activity.PosterPath = await _storageService.SaveFileAsync(compressed.Stream, compressed.ContentType, "posters");
        activity.PosterFileName = poster.FileName;
    }

    /// <summary>
    /// Since PUT replaces the whole activity rather than applying discrete patch operations, field-level
    /// restrictions can't be enforced by inspecting operation paths like PatchActivity does. Rejecting the
    /// request whenever one of these fields differs from its current value would let a non-board organizer
    /// guess a hidden value (e.g. VatRate) and learn whether the guess was correct from whether the request
    /// succeeds - so instead, these fields are silently reset to their current value on the DTO before it's
    /// applied, regardless of what was submitted for them.
    ///
    /// The finance fields are preserved unless the caller has ManageFinances; the remaining
    /// organizer-editable fields are preserved unless the caller has EditActivityForGroup for this activity's
    /// organizer group. The online/structural fields (ShowInKoala/ShowOnWebsite/EnrollOpenDate) are always
    /// preserved here - they require board or EditAllActivities, which bypasses this method entirely.
    ///
    /// PaymentDeadline isn't included: <see cref="ApplyUpdateDto"/> never copies it onto the activity in
    /// the first place, so PUT already can't change it either way.
    /// </summary>
    private static void PreserveFieldsOutsideAllowedFields(Activity activity, PutActivityDTO dto, bool hasManageFinances, bool canEditForGroup)
    {
        if (!hasManageFinances)
        {
            dto.VatRate = activity.VatRate;
            dto.GLAccountId = activity.GLAccountId;
            dto.CostCenterId = activity.CostCenterId;
            dto.CostUnitId = activity.CostUnitId;
        }

        dto.ShowInKoala = activity.ShowInKoala;
        dto.ShowOnWebsite = activity.ShowOnWebsite;
        dto.EnrollOpenDate = activity.EnrollOpenDate;

        if (!canEditForGroup)
        {
            dto.Name = activity.Name;
            dto.Price = activity.Price;
            dto.DutchDescription = activity.DutchDescription;
            dto.EnglishDescription = activity.EnglishDescription;
            dto.DateTimeStart = activity.DateTimeStart;
            dto.DateTimeEnd = activity.DateTimeEnd;
            dto.UnenrollmentDeadline = activity.UnenrollmentDeadline;
            dto.EnrollmentDeadline = activity.EnrollmentDeadline;
            dto.Location = activity.Location;
            dto.ParticipantLimit = activity.ParticipantLimit;
            dto.OrganizerId = activity.OrganizerId;
            dto.IsEnrollable = activity.IsEnrollable;
            dto.AreParticipantsVisible = activity.AreParticipantsVisible;
            dto.IsAdultOnly = activity.IsAdultOnly;
            dto.IsWeeklyDrinks = activity.IsWeeklyDrinks;
            dto.AllowedAudience = activity.AllowedAudience;
        }
    }

    private static void ApplyUpdateDto(Activity activity, PutActivityDTO dto)
    {
        activity.Name = dto.Name;
        activity.Price = dto.Price;
        activity.DutchDescription = dto.DutchDescription;
        activity.EnglishDescription = dto.EnglishDescription;
        activity.DateTimeStart = dto.DateTimeStart;
        activity.DateTimeEnd = dto.DateTimeEnd;
        activity.UnenrollmentDeadline = dto.UnenrollmentDeadline;
        activity.EnrollmentDeadline = dto.EnrollmentDeadline;
        activity.EnrollOpenDate = dto.IsEnrollable ? null : dto.EnrollOpenDate;
        activity.Location = dto.Location;
        activity.ParticipantLimit = dto.ParticipantLimit;
        activity.OrganizerId = dto.OrganizerId;
        activity.ShowInKoala = dto.ShowInKoala;
        activity.ShowOnWebsite = dto.ShowOnWebsite;
        activity.IsEnrollable = dto.IsEnrollable;
        activity.AreParticipantsVisible = dto.AreParticipantsVisible;
        activity.IsAdultOnly = dto.IsAdultOnly;
        activity.IsWeeklyDrinks = dto.IsWeeklyDrinks;
        activity.AllowedAudience = dto.AllowedAudience;
        activity.VatRate = dto.VatRate;
        activity.GLAccountId = dto.GLAccountId;
        activity.CostCenterId = dto.CostCenterId;
        activity.CostUnitId = dto.CostUnitId;
    }

    private static StringBuilder BuildEnrollmentsCsv(Language language, Activity activity)
    {
        var csv = new StringBuilder();

        // Create CSV header
        var header = language == Language.NL ? new List<string> { "Voornaam", "Achternaam", "Op Wachtlijst" } : new List<string> { "First Name", "Last Name", "On Waiting List" };

        foreach (var question in activity.SpecificationQuestions.OrderBy(q => q.Id))
        {
            header.Add(language == Language.NL ? question.QuestionDutch : question.QuestionEnglish);
        }

        csv.AppendLine(string.Join(";", header));

        // Create CSV rows for each enrollment
        foreach (var enrollment in activity.Enrollments.OrderBy(e => e.RegisteredOn)
            .OrderBy(e => e.IsOnWaitingList)
            .ThenBy(e => e.RegisteredOn))
        {
            // Create basic information
            var row = new List<string>
            {
                enrollment.Member.FirstName,
                enrollment.Member.LastName,
                enrollment.IsOnWaitingList ? "True" : "False"
            };

            // Add answers to specification questions in the order of the questions
            foreach (var question in activity.SpecificationQuestions.OrderBy(q => q.Id))
            {
                var answer = enrollment.SpecificationAnswers
                    .FirstOrDefault(a => a.SpecificationQuestionId == question.Id)?.Answer ?? "";

                row.Add(answer.Replace(";", ","));
            }

            csv.AppendLine(string.Join(";", row));
        }

        return csv;
    }
}
