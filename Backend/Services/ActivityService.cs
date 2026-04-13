using Backend.Controllers.DTOs;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Backend.Utils;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Backend.Services;

public class ActivityService : IActivityService
{
    private readonly PostgresDbContext _db;
    private readonly IStorageService _storageService;
    private readonly IFileCompressor _fileCompressor;
    private readonly IPermissionService _permissionService;

    private readonly string[] _restrictedPaths = new[] { "/showInKoala", "/showOnWebsite", "/paymentDeadline" };

    public ActivityService(
        PostgresDbContext db,
        IStorageService storageService,
        IFileCompressor fileCompressor,
        IPermissionService permissionService)
    {
        _db = db;
        _storageService = storageService;
        _fileCompressor = fileCompressor;
        _permissionService = permissionService;
    }

    public async Task<IEnumerable<ActivityResponseDTO>> GetActivities(Guid userId, GetActivitiesDTO dto)
    {
        bool isBoard = _permissionService.IsBoardMember(userId);

        if (dto.IncludePast && !isBoard)
            throw new UnauthorizedAccessException();

        IQueryable<ActivityResponseDTO> query = _db.Activities
            .Include(a => a.SpecificationQuestions)
            .Select(a => new ActivityResponseDTO
            {
                Id = a.Id,
                Name = a.Name,
                Price = a.Price,
                PosterPath = a.PosterPath,
                PosterFileName = a.PosterFileName,
                DutchDescription = a.DutchDescription,
                EnglishDescription = a.EnglishDescription,
                DateTimeStart = a.DateTimeStart,
                DateTimeEnd = a.DateTimeEnd,
                UnenrollmentDeadline = a.UnenrollmentDeadline,
                EnrollmentDeadline = a.EnrollmentDeadline,
                Location = a.Location,
                ParticipantLimit = a.ParticipantLimit,
                OrganizerId = a.OrganizerId,
                ShowInKoala = a.ShowInKoala,
                ShowOnWebsite = a.ShowOnWebsite,
                IsEnrollable = a.IsEnrollable,
                AreParticipantsVisible = a.AreParticipantsVisible,
                IsAdultOnly = a.IsAdultOnly,
                AllowedAudience = a.AllowedAudience,
                VatRate = a.VatRate,
                GLAccountId = a.GLAccountId,
                CostCenterId = a.CostCenterId,
                CostUnitId = a.CostUnitId,
                Enrollments = a.Enrollments.Select(e => new EnrollmentSummaryDTO
                {
                    IsOnWaitingList = e.IsOnWaitingList,
                    Member = new MemberSummaryDTO
                    {
                        Id = e.MemberId == userId ? e.MemberId : null,
                        FirstName = a.AreParticipantsVisible || _permissionService.IsBoardMember(userId) ? e.Member.FirstName : null,
                        LastName = a.AreParticipantsVisible || _permissionService.IsBoardMember(userId) ? e.Member.LastName : null,
                        ProfilePicturePath = a.AreParticipantsVisible || _permissionService.IsBoardMember(userId) ? e.Member.ProfilePicturePath : null
                    },
                    SpecificationAnswers = e.SpecificationAnswers.Where(sa => isBoard || sa.MemberId == userId || sa.Question.IsPublic).Select(sa => new SpecificationAnswerResponseDTO
                    {
                        QuestionId = sa.SpecificationQuestionId,
                        AnswerId = sa.Id,
                        Answer = sa.Answer
                    }).ToList(),
                    Price = isBoard ? e.Price : null
                }).ToList(),
                SpecificationQuestions = a.SpecificationQuestions.Select(q => new GetSpecificationQuestionResponseDTO
                {
                    Id = q.Id,
                    QuestionDutch = q.QuestionDutch,
                    QuestionEnglish = q.QuestionEnglish,
                    Type = q.Type,
                    IsMandatory = q.IsMandatory,
                    IsPublic = q.IsPublic,
                    Options = q.Options != null
                        ? q.Options.Split(new[] { ';' }, StringSplitOptions.None).ToList()
                        : null
                }).ToList(),
                PaymentDeadline = isBoard ? a.PaymentDeadline : default,
                IsOpenForPayment = a.IsOpenForPayment
            });

        if (!dto.IncludePast)
        {
            DateTime now = DateTime.UtcNow;
            query = query.Where(a => a.DateTimeEnd > now && a.ShowInKoala);
        }

        if(!dto.IncludeFuture)
        {
            DateTime now = DateTime.UtcNow;
            query = query.Where(a => a.DateTimeStart < now && a.ShowInKoala);
        }

        if (dto.Year.HasValue)
        {
            query = query.Where(a => a.DateTimeStart.Year == dto.Year.Value);
        }

        if(dto.OpenForPayment.HasValue)
        {
            DateTime now = DateTime.UtcNow;
            query = query.Where(a => a.IsOpenForPayment == dto.OpenForPayment.Value);
        }

        return await query.OrderBy(a => a.DateTimeStart).ToListAsync();
    }

    public async Task<ActivityResponseDTO?> GetActivity(Guid userId, uint id)
    {
        bool isBoard = _permissionService.IsBoardMember(userId);

        var activity = await _db.Activities.Select(a => new ActivityResponseDTO
        {
            Id = a.Id,
            Name = a.Name,
            Price = a.Price,
            PosterPath = a.PosterPath,
            PosterFileName = a.PosterFileName,
            DutchDescription = a.DutchDescription,
            EnglishDescription = a.EnglishDescription,
            DateTimeStart = a.DateTimeStart,
            DateTimeEnd = a.DateTimeEnd,
            UnenrollmentDeadline = a.UnenrollmentDeadline,
            EnrollmentDeadline = a.EnrollmentDeadline,
            Location = a.Location,
            ParticipantLimit = a.ParticipantLimit,
            OrganizerId = a.OrganizerId,
            ShowInKoala = a.ShowInKoala,
            ShowOnWebsite = a.ShowOnWebsite,
            IsEnrollable = a.IsEnrollable,
            AreParticipantsVisible = a.AreParticipantsVisible,
            IsAdultOnly = a.IsAdultOnly,
            AllowedAudience = a.AllowedAudience,
            VatRate = a.VatRate,
            GLAccountId = a.GLAccountId,
            CostCenterId = a.CostCenterId,
            CostUnitId = a.CostUnitId,
            Enrollments = a.Enrollments.Select(e => new EnrollmentSummaryDTO
            {
                IsOnWaitingList = e.IsOnWaitingList,
                Member = new MemberSummaryDTO
                {
                    Id = e.MemberId == userId ? e.MemberId : null,
                    FirstName = a.AreParticipantsVisible || isBoard ? e.Member.FirstName : null,
                    LastName = a.AreParticipantsVisible || isBoard ? e.Member.LastName : null,
                    ProfilePicturePath = a.AreParticipantsVisible || isBoard ? e.Member.ProfilePicturePath : null
                },
                SpecificationAnswers = e.SpecificationAnswers.Where(sa => isBoard || sa.MemberId == userId || sa.Question.IsPublic).Select(sa => new SpecificationAnswerResponseDTO
                {
                    QuestionId = sa.SpecificationQuestionId,
                    AnswerId = sa.Id,
                    Answer = sa.Answer
                }).ToList(),
                Price = isBoard ? e.Price : null
            }).ToList(),
            SpecificationQuestions = a.SpecificationQuestions.Select(q => new GetSpecificationQuestionResponseDTO
            {
                Id = q.Id,
                QuestionDutch = q.QuestionDutch,
                QuestionEnglish = q.QuestionEnglish,
                Type = q.Type,
                IsMandatory = q.IsMandatory,
                IsPublic = q.IsPublic,
                Options = q.Options != null
                    ? q.Options.Split(new[] { ';' }, StringSplitOptions.None).ToList()
                    : null
            }).ToList(),
            PaymentDeadline = isBoard ? a.PaymentDeadline : default,
            IsOpenForPayment = a.IsOpenForPayment
        }).FirstOrDefaultAsync(a => a.Id == id);

        if (activity == null)
            return null;

        if (activity.DateTimeEnd.UtcDateTime < DateTime.UtcNow && isBoard)
            throw new UnauthorizedAccessException();

        return activity;
    }

    public async Task<Activity> CreateActivity(Guid userId, PostActivityDTO dto)
    {
        if (dto.DateTimeEnd < dto.DateTimeStart)
            throw new ArgumentException("Activity cannot end before it starts.");

        if ((dto.ShowInKoala || dto.ShowOnWebsite || dto.PaymentDeadline != null) &&
            !_permissionService.IsBoardMember(userId))
            throw new UnauthorizedAccessException();

        if (dto.ParticipantLimit < 0)
            throw new ArgumentException("Participant limit cannot be negative.");

        if (dto.Poster != null && !ExtensionUtils.IsValidPosterExtension(dto.Poster))
            throw new ArgumentException("Invalid poster file type.");

        using var transaction = await _db.Database.BeginTransactionAsync();

        var questions = string.IsNullOrEmpty(dto.SpecificationQuestionsJson)
                ? new List<SpecificationQuestionDTO>()
                : JsonConvert.DeserializeObject<List<SpecificationQuestionDTO>>(dto.SpecificationQuestionsJson);

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
                Location = dto.Location,
                ParticipantLimit = dto.ParticipantLimit,
                OrganizerId = dto.OrganizerId,
                ShowInKoala = dto.ShowInKoala,
                ShowOnWebsite = dto.ShowOnWebsite,
                IsEnrollable = dto.IsEnrollable,
                AreParticipantsVisible = dto.AreParticipantsVisible,
                IsAdultOnly = dto.IsAdultOnly,
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

            StateValidateUtils.Validate(activity);

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
        if (!_permissionService.IsBoardMember(userId))
            throw new UnauthorizedAccessException();

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

        if ((activity.ShowInKoala 
                || activity.ShowOnWebsite 
                || patchDoc.Operations.Any(op => _restrictedPaths.Contains(op.path, StringComparer.OrdinalIgnoreCase))) &&
                !_permissionService.IsBoardMember(userId))
            throw new UnauthorizedAccessException();


        using var transaction = await _db.Database.BeginTransactionAsync(ct);

        try
        {
            uint? oldLimit = activity.ParticipantLimit;
            decimal oldPrice = activity.Price;

            patchDoc.ApplyTo(activity);

            StateValidateUtils.Validate(activity);

            if (activity.ParticipantLimit == null || (oldLimit.HasValue && activity.ParticipantLimit > oldLimit))
                await ProcessWaitingList(id, activity.ParticipantLimit, ct);

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

        if ((activity.ShowInKoala || activity.ShowOnWebsite) &&
            !_permissionService.IsBoardMember(userId))
            throw new UnauthorizedAccessException();

        if (poster != null && !ExtensionUtils.IsValidPosterExtension(poster))
            throw new ArgumentException("Invalid poster file.");

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

        if (dto.Poster != null && !ExtensionUtils.IsValidPosterExtension(dto.Poster))
            throw new ArgumentException("Invalid poster file type.");

        if ((activity.ShowInKoala || activity.ShowOnWebsite || dto.ShowInKoala || dto.ShowOnWebsite || dto.PaymentDeadline != null) &&
            !_permissionService.IsBoardMember(userId))
            throw new UnauthorizedAccessException();

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

            activity.Name = dto.Name;
            activity.Price = dto.Price;
            activity.DutchDescription = dto.DutchDescription;
            activity.EnglishDescription = dto.EnglishDescription;
            activity.DateTimeStart = dto.DateTimeStart;
            activity.DateTimeEnd = dto.DateTimeEnd;
            activity.UnenrollmentDeadline = dto.UnenrollmentDeadline;
            activity.EnrollmentDeadline = dto.EnrollmentDeadline;
            activity.Location = dto.Location;
            activity.ParticipantLimit = dto.ParticipantLimit;
            activity.OrganizerId = dto.OrganizerId;
            activity.ShowInKoala = dto.ShowInKoala;
            activity.ShowOnWebsite = dto.ShowOnWebsite;
            activity.IsEnrollable = dto.IsEnrollable;
            activity.AreParticipantsVisible = dto.AreParticipantsVisible;
            activity.IsAdultOnly = dto.IsAdultOnly;
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

            if (activity.ParticipantLimit == null || (oldLimit.HasValue && activity.ParticipantLimit > oldLimit))
                await ProcessWaitingList(id, activity.ParticipantLimit, default);

            StateValidateUtils.Validate(activity);

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

        if (activity.DateTimeEnd.UtcDateTime < DateTime.UtcNow &&
            !_permissionService.IsBoardMember(userId))
            throw new UnauthorizedAccessException();

        var file = await _storageService.GetFileAsync("posters", activity.PosterPath);

        if (file == null)
            return null;

        return (
            file.Stream,
            file.ContentType,
            download ? activity.PosterFileName ?? "poster" : null
        );
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
            var waitingList = await _db.Enrollments
                .Where(e => e.ActivityId == activityId && e.IsOnWaitingList)
                .OrderBy(e => e.RegisteredOn)
                .Take(availableSpots)
                .ToListAsync(ct);

            foreach (var e in waitingList)
                e.IsOnWaitingList = false;
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