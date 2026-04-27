using Backend.Controllers.DTOs;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Backend.Projections;
using Backend.Validators;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.EntityFrameworkCore;
using System.Text;
using Backend.Utils.DateTime;

namespace Backend.Services;

public class ActivityService : IActivityService
{
    private readonly PostgresDbContext _db;
    private readonly IStorageService _storageService;
    private readonly IFileCompressService _fileCompressor;
    private readonly IPermissionService _permissionService;
    private readonly IEnrollmentService _enrollmentService;

    private readonly string[] _restrictedForEveryonePaths = new [] {"/id", "/posterFileName", "/posterPath", };
    private readonly string[] _restrictedPaths = new[] { "/vatRate", "/gLAccountId", "/costCenterId", "/costUnitId", "/paymentDeadline", "/showInKoala", "/enrollOpenDate", "/showOnWebsite", "/paymentDeadline", "/enrollOpenDate" };

    public ActivityService(
        PostgresDbContext db,
        IStorageService storageService,
        IFileCompressService fileCompressor,
        IPermissionService permissionService,
        IEnrollmentService enrollmentService)
    {
        _db = db;
        _storageService = storageService;
        _fileCompressor = fileCompressor;
        _permissionService = permissionService;
        _enrollmentService = enrollmentService;
    }

    public async Task<IEnumerable<ActivityResponseDTO>> GetActivities(Guid userId, GetActivitiesDTO dto)
    {
        bool isBoard = _permissionService.IsBoardOrCandidateBoardMember(userId);

        // Only board members can see past activities or activities that are not shown in Koala/website
        if (dto.IncludePast && !isBoard)
            throw new UnauthorizedAccessException();

        // Get userGroupIds to filter activities that are only visible for the organizers of the groups
        var userGroupIds = await _db.GroupMemberships
            .Where(gm => gm.MemberId == userId && gm.MembershipYear == FinancialYearUtils.GetCurrentFinancialYear())
            .Select(gm => gm.GroupId)
            .ToListAsync();

        // Filter activities based on the provided criteria and the user's permissions
        var activities = await _db.Activities
            .Include(a => a.Enrollments)
                .ThenInclude(e => e.Member)
            .Include(a => a.SpecificationQuestions)
            .Include(a => a.Enrollments)
                .ThenInclude(e => e.SpecificationAnswers) 
            .AsNoTracking()
            .Filter(dto, isBoard, userGroupIds)
            .ToListAsync();

        return activities.Select(a => ActivityProjections.ToDto(userId, isBoard).Compile()(a));
    }

    public async Task<ActivityResponseDTO?> GetActivity(Guid userId, uint id)
    {
        bool isBoard = _permissionService.IsBoardOrCandidateBoardMember(userId);

        var activity = await _db.Activities
            .Include(a => a.SpecificationQuestions)
            .Include(a => a.Enrollments)
                .ThenInclude(e => e.Member)
            .Include(a => a.Enrollments)
                .ThenInclude(e => e.SpecificationAnswers)
                    .ThenInclude(sa => sa.Question) 
            .FirstOrDefaultAsync(a => a.Id == id);

        if (activity == null)
            return null;

        if(!activity.IsEnrollable && activity.EnrollOpenDate != null && activity.EnrollOpenDate <= DateTimeOffset.UtcNow)
        {
            activity.IsEnrollable = true;
            activity.EnrollOpenDate = null;
            await _db.SaveChangesAsync();
        }

        if (activity.DateTimeEnd.UtcDateTime < DateTime.UtcNow && !isBoard)
            throw new UnauthorizedAccessException();

        if (!activity.ShowInKoala && isBoard && (activity.OrganizerId == null || !_permissionService.IsInGroupInCurrentYear(userId, activity.OrganizerId.Value)))
            throw new UnauthorizedAccessException();

        return ActivityProjections.ToDto(userId, isBoard).Compile()(activity);
    }

    public async Task<Activity> CreateActivity(Guid userId, PostActivityDTO dto)
    {
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
            var organizerMembers = await _db.GroupMemberships
                .Where(gm => gm.GroupId == dto.OrganizerId)
                .Select(gm => gm.Member)
                .ToListAsync();

            foreach(var member in organizerMembers)
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
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task DeleteActivity(Guid userId, uint id)
    {
        _permissionService.EnsureBoardOrCandidateBoardMember(userId);

        var activity = await _db.Activities.FindAsync(id);
        if (activity == null)
            throw new KeyNotFoundException();

        if (activity.PosterPath != null)
            await _storageService.DeleteFileAsync("posters", activity.PosterPath);

        _db.Activities.Remove(activity);
        await _db.SaveChangesAsync();
    }

    public async Task PatchActivity(Guid userId, uint id, JsonPatchDocument<Activity> patchDoc, CancellationToken ct)
    {
        if (patchDoc == null)
            throw new ArgumentException();

        var activity = await _db.Activities.FindAsync(new [] { id }, ct);
        if (activity == null)
            throw new KeyNotFoundException();

        if(patchDoc.Operations.Any(op => _restrictedForEveryonePaths.Contains(op.path, StringComparer.OrdinalIgnoreCase)))
            throw new UnauthorizedAccessException("You are not allowed to modify the id or poster properties of the activity.");

        if (activity.ShowInKoala 
                || activity.ShowOnWebsite
                || activity.EnrollOpenDate != null
                || patchDoc.Operations.Any(op => _restrictedPaths.Contains(op.path, StringComparer.OrdinalIgnoreCase)))
            _permissionService.EnsureBoardOrCandidateBoardMember(userId);

        using var transaction = await _db.Database.BeginTransactionAsync(ct);

        try
        {
            // save old data for processing waiting list and updating enrollment prices
            uint? oldLimit = activity.ParticipantLimit;
            decimal oldPrice = activity.Price;
            var oldAudience = activity.AllowedAudience;

            patchDoc.ApplyTo(activity);


            if (activity.IsEnrollable)
            {
                activity.EnrollOpenDate = null;
            }

            StateValidator.Validate(activity);

            // Update waiting list
            if (activity.ParticipantLimit == null || (oldLimit.HasValue && activity.ParticipantLimit > oldLimit) 
                || activity.AllowedAudience != oldAudience)
            {
                await ProcessWaitingList(id, activity.ParticipantLimit, ct);
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
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }

    public async Task UploadPoster(Guid userId, uint id, IFormFile? poster)
    {
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
                await _storageService.DeleteFileAsync("posters", oldPath);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task UpdateActivity(Guid userId, uint id, PutActivityDTO dto)
    {
        var activity = await _db.Activities
            .Include(a => a.SpecificationQuestions)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (activity == null)
            throw new KeyNotFoundException();

        if(activity.ShowInKoala || activity.ShowOnWebsite || activity.EnrollOpenDate != null)
            _permissionService.EnsureBoardOrCandidateBoardMember(userId);

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

            // Update waiting list if participant limit or allowed audience has changed
            if (activity.ParticipantLimit == null || (oldLimit.HasValue && activity.ParticipantLimit > oldLimit) 
                || activity.AllowedAudience != oldAudience)
            {
                await ProcessWaitingList(id, activity.ParticipantLimit, default);
            }

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            // Delete old poster if a new one was uploaded and the activity had an existing poster
            if (existingPosterPath != null && dto.Poster != null)
                await _storageService.DeleteFileAsync("posters", existingPosterPath);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    public async Task<(Stream Stream, string ContentType, string? FileName)?> GetPoster(Guid userId, uint id, bool download)
    {
        var activity = await _db.Activities.FindAsync(id);

        if (activity == null || string.IsNullOrEmpty(activity.PosterPath))
            return null;

        // Only board members can see posters of past activities
        if (activity.DateTimeEnd.UtcDateTime < DateTime.UtcNow)
            _permissionService.EnsureBoardOrCandidateBoardMember(userId);

        // Only board members or members of the organizer group can see posters of activities that are not shown in Koala, even if the activity is in the future
        if (!activity.ShowInKoala && (activity.OrganizerId == null || !_permissionService.IsInGroupInCurrentYear(userId, activity.OrganizerId.Value)))
            _permissionService.EnsureBoardOrCandidateBoardMember(userId);

        // Get poster file from storage
        var file = await _storageService.GetFileAsync("posters", activity.PosterPath);

        if (file == null)
            return null;

        // If download is true, the file will be returned with a filename to trigger download in the frontend, otherwise it will be displayed in the browser if supported
        return (
            file.Stream,
            file.ContentType,
            download ? activity.PosterFileName ?? "poster" : null
        );
    }

    public async Task<(byte[] Content, string FileName)> GetEnrollmentsCsv(Guid userId, uint activityId, CancellationToken ct)
    {
        // Only board members or members of the organizer group can download the enrollments CSV
        _permissionService.EnsureBoardOrCandidateBoardMember(userId);
            
        // Get user preferred language for CSV header
        Member? member = await _db.Members.FirstOrDefaultAsync(m => m.Id == userId, ct);
        if(member == null)
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

    private async Task ProcessWaitingList(uint activityId, uint? newLimit, CancellationToken ct)
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
            _enrollmentService.PromoteFromWaitingList(activityId, availableSpots, ct);
        }
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
