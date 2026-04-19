using Backend.Controllers.DTOs;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Backend.Projections;
using Backend.Validators;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
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

    private readonly string[] _restrictedPaths = new[] { "/showInKoala", "/showOnWebsite", "/paymentDeadline", "/enrollOpenDate" };

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

        if (dto.IncludePast && !isBoard)
            throw new UnauthorizedAccessException();

        var userGroupIds = await _db.GroupMemberships
            .Where(gm => gm.MemberId == userId && gm.MembershipYear == FinancialYearUtils.GetCurrentFinancialYear())
            .Select(gm => gm.GroupId)
            .ToListAsync();

        var query = _db.Activities
            .AsNoTracking()
            .Filter(dto, isBoard, userGroupIds)
            .Select(ActivityProjections.ToDto(userId, isBoard));

        return await query
            .OrderBy(a => a.DateTimeStart)
            .ToListAsync();
    }

    public async Task<ActivityResponseDTO?> GetActivity(Guid userId, uint id)
    {
        bool isBoard = _permissionService.IsBoardOrCandidateBoardMember(userId);

        var activity = await _db.Activities.FirstOrDefaultAsync(a => a.Id == id);

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

        return await _db.Activities
            .Where(a => a.Id == id)
            .Select(ActivityProjections.ToDto(userId, isBoard))
            .FirstOrDefaultAsync();
    }

    public async Task<Activity> CreateActivity(Guid userId, PostActivityDTO dto)
    {
        if (dto.DateTimeEnd < dto.DateTimeStart)
            throw new ArgumentException("Activity cannot end before it starts.");

        if (dto.ShowInKoala || dto.ShowOnWebsite || dto.PaymentDeadline != null)
            _permissionService.EnsureBoardOrCandidateBoardMember(userId);

        if (dto.ParticipantLimit < 0)
            throw new ArgumentException("Participant limit cannot be negative.");

        if (dto.Poster != null)
        {
            ExtensionValidator.ValidatePosterExtension(dto.Poster);
        }

        if (dto.IsEnrollable)
        {
            dto.EnrollOpenDate = null;
        }

        using var transaction = await _db.Database.BeginTransactionAsync();

        var questions = string.IsNullOrEmpty(dto.SpecificationQuestionsJson)
                ? new List<SpecificationQuestionDTO>()
                : JsonConvert.DeserializeObject<List<SpecificationQuestionDTO>>(dto.SpecificationQuestionsJson);

        var organizerMembers = await _db.GroupMemberships
            .Where(gm => gm.GroupId == dto.OrganizerId)
            .Select(gm => gm.Member)
            .ToListAsync();

        if (questions == null)
            throw new ArgumentException("Invalid specification questions format.");

        try
        {
            var activity = new Activity
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

            if (dto.Poster != null)
            {
                var compressed = await _fileCompressor.CompressFileAsync(dto.Poster);
                activity.PosterPath = await _storageService.SaveFileAsync(compressed.Stream, compressed.ContentType, "posters");
                activity.PosterFileName = dto.Poster.FileName;
            }

            StateValidator.Validate(activity);

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

            _db.Activities.Add(activity);
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

        if (activity.ShowInKoala 
                || activity.ShowOnWebsite 
                || patchDoc.Operations.Any(op => _restrictedPaths.Contains(op.path, StringComparer.OrdinalIgnoreCase)))
            _permissionService.EnsureBoardOrCandidateBoardMember(userId);


        using var transaction = await _db.Database.BeginTransactionAsync(ct);

        try
        {
            uint? oldLimit = activity.ParticipantLimit;
            decimal oldPrice = activity.Price;
            var oldAudience = activity.AllowedAudience;

            patchDoc.ApplyTo(activity);

            if (activity.IsEnrollable)
            {
                activity.EnrollOpenDate = null;
            }

            StateValidator.Validate(activity);

            if (activity.ParticipantLimit == null || (oldLimit.HasValue && activity.ParticipantLimit > oldLimit) 
                || activity.AllowedAudience != oldAudience)
            {
                await ProcessWaitingList(id, activity.ParticipantLimit, ct);
            }

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

        if (activity.ShowInKoala || activity.ShowOnWebsite)
        {
            _permissionService.EnsureBoardOrCandidateBoardMember(userId);
        }

        if (poster != null)
            ExtensionValidator.ValidatePosterExtension(poster);

        string? oldPath = activity.PosterPath;

        using var transaction = await _db.Database.BeginTransactionAsync();

        try
        {
            if (poster != null)
            {
                var compressedImage = await _fileCompressor.CompressFileAsync(poster);
                string path = await _storageService.SaveFileAsync(compressedImage.Stream, compressedImage.ContentType, "posters");

                activity.PosterPath = path;
                activity.PosterFileName = poster.FileName;
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

    public async Task PutActivity(Guid userId, uint id, PutActivityDTO dto)
    {
        var activity = await _db.Activities
            .Include(a => a.SpecificationQuestions)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (activity == null)
            throw new KeyNotFoundException();

        if (dto.DateTimeEnd < dto.DateTimeStart)
            throw new ArgumentException("Activity cannot end before it starts.");

        if (dto.ParticipantLimit < 0)
            throw new ArgumentException("Participant limit cannot be negative.");

        if (dto.Poster != null)
            ExtensionValidator.ValidatePosterExtension(dto.Poster);

        if (activity.ShowInKoala || activity.ShowOnWebsite || dto.ShowInKoala || dto.ShowOnWebsite || dto.PaymentDeadline != null || dto.EnrollOpenDate != null)
            _permissionService.EnsureBoardOrCandidateBoardMember(userId);

        using var transaction = await _db.Database.BeginTransactionAsync();

        var questions = string.IsNullOrEmpty(dto.SpecificationQuestionsJson)
                ? new List<UpdateSpecificationQuestionDTO>()
                : JsonConvert.DeserializeObject<List<UpdateSpecificationQuestionDTO>>(dto.SpecificationQuestionsJson);

        if (questions == null)
            throw new ArgumentException("Invalid specification questions format.");

        try
        {
            decimal oldPrice = activity.Price;
            uint? oldLimit = activity.ParticipantLimit;
            string? existingPosterPath = activity.PosterPath;
            var oldAudience = activity.AllowedAudience;

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

            await SyncSpecificationQuestions(activity, questions);

            if (oldPrice != activity.Price)
            {
                var enrollmentsToUpdate = await _db.Enrollments
                    .Where(e => e.ActivityId == id && e.Price == oldPrice)
                    .ToListAsync();

                foreach (var enrollment in enrollmentsToUpdate)
                    enrollment.Price = activity.Price;
            }

            if (dto.Poster != null)
            {
                var compressed = await _fileCompressor.CompressFileAsync(dto.Poster);
                activity.PosterPath = await _storageService.SaveFileAsync(compressed.Stream, compressed.ContentType, "posters");
                activity.PosterFileName = dto.Poster.FileName;
            }

            if (activity.ParticipantLimit == null || (oldLimit.HasValue && activity.ParticipantLimit > oldLimit) 
                || activity.AllowedAudience != oldAudience)
            {
                await ProcessWaitingList(id, activity.ParticipantLimit, default);
            }

            StateValidator.Validate(activity);

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

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

        if (activity.DateTimeEnd.UtcDateTime < DateTime.UtcNow)
            _permissionService.EnsureBoardOrCandidateBoardMember(userId);

        var file = await _storageService.GetFileAsync("posters", activity.PosterPath);

        if (file == null)
            return null;

        return (
            file.Stream,
            file.ContentType,
            download ? activity.PosterFileName ?? "poster" : null
        );
    }

    public async Task<(byte[] Content, string FileName)> GetEnrollmentsCsv(Guid userId, uint activityId, CancellationToken ct)
    {
        _permissionService.EnsureBoardOrCandidateBoardMember(userId);

        Member? member = await _db.Members.FirstOrDefaultAsync(m => m.Id == userId, ct);

        if(member == null)
            throw new UnauthorizedAccessException("User not found");

        Language language = member.PreferredLanguage;

        var activity = await _db.Activities
            .Include(a => a.SpecificationQuestions)
            .Include(a => a.Enrollments)
                .ThenInclude(e => e.Member)
            .Include(a => a.Enrollments)
                .ThenInclude(e => e.SpecificationAnswers)
            .FirstOrDefaultAsync(a => a.Id == activityId, ct);

        if (activity == null) throw new KeyNotFoundException("Activity not found");

        var csv = new StringBuilder();

        var header = language == Language.NL ? new List<string> { "Voornaam", "Achternaam", "Op Wachtlijst" } : new List<string> { "First Name", "Last Name", "On Waiting List" };
        foreach (var question in activity.SpecificationQuestions.OrderBy(q => q.Id))
        {
            header.Add(language == Language.NL ? question.QuestionDutch : question.QuestionEnglish);
        }
        csv.AppendLine(string.Join(";", header));

        foreach (var enrollment in activity.Enrollments.OrderBy(e => e.RegisteredOn)
            .OrderBy(e => e.IsOnWaitingList)
            .ThenBy(e => e.RegisteredOn))
        {
            var row = new List<string>
            {
                enrollment.Member.FirstName,
                enrollment.Member.LastName,
                enrollment.IsOnWaitingList ? "True" : "False"
            };

            foreach (var question in activity.SpecificationQuestions.OrderBy(q => q.Id))
            {
                var answer = enrollment.SpecificationAnswers
                    .FirstOrDefault(a => a.SpecificationQuestionId == question.Id)?.Answer ?? "";
                
                row.Add(answer.Replace(";", ",")); 
            }

            csv.AppendLine(string.Join(";", row));
        }

        var fileName = $"Enrollments_{activity.Name}.csv";
        return (Encoding.UTF8.GetBytes(csv.ToString()), fileName);
    }

    private async Task ProcessWaitingList(uint activityId, uint? newLimit, CancellationToken ct)
    {
        int currentParticipants = await _db.Enrollments
            .CountAsync(e => e.ActivityId == activityId && !e.IsOnWaitingList, ct);

        int availableSpots = newLimit.HasValue
            ? (int)newLimit.Value - currentParticipants
            : int.MaxValue;

        if (availableSpots > 0)
        {
            _enrollmentService.PromoteFromWaitingList(activityId, availableSpots, ct);
        }
    }

    private void MapSpecificationQuestion(SpecificationQuestion entity, UpdateSpecificationQuestionDTO dto)
    {
        entity.QuestionDutch = dto.QuestionDutch;
        entity.QuestionEnglish = dto.QuestionEnglish;
        entity.Type = dto.Type;
        entity.IsMandatory = dto.IsMandatory;
        entity.IsPublic = dto.IsPublic;
        entity.Options = dto.Options != null && dto.Options.Any()
            ? string.Join(';', dto.Options)
            : null;
    }

    private async Task SyncSpecificationQuestions(Activity activity, List<UpdateSpecificationQuestionDTO> dtoQuestions)
    {
        await _db.Entry(activity)
            .Collection(a => a.SpecificationQuestions)
            .LoadAsync();

        var existingQuestions = activity.SpecificationQuestions.ToList();

        foreach (var dto in dtoQuestions)
        {
            if (dto.Id.HasValue)
            {
                var existing = existingQuestions.FirstOrDefault(q => q.Id == dto.Id.Value);
                if (existing == null)
                    throw new Exception($"SpecificationQuestion with id {dto.Id} not found.");

                MapSpecificationQuestion(existing, dto);
            }
            else
            {
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

        var dtoIds = dtoQuestions.Where(q => q.Id.HasValue).Select(q => q.Id!.Value).ToHashSet();

        var toRemove = existingQuestions.Where(q => !dtoIds.Contains(q.Id)).ToList();

        _db.SpecificationQuestions.RemoveRange(toRemove);
    }
}