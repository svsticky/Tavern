using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Backend.Controllers.DTOs;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models;
using Backend.Models.Domain;
using Backend.Services.Domain;
using Backend.Services.MailServices;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Serialization;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Backend.Tests.Services.Domain;

public class ActivityTestPostgresDbContext : PostgresDbContext
{
    public ActivityTestPostgresDbContext(DbContextOptions<PostgresDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<MembershipPayment>()
            .HasIndex(p => p.MemberId)
            .IsUnique()
            .HasFilter("MemberId IS NOT NULL");

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var properties = entityType.ClrType.GetProperties()
                .Where(p => p.PropertyType == typeof(DateTimeOffset) || p.PropertyType == typeof(DateTimeOffset?));
            foreach (var property in properties)
            {
                modelBuilder.Entity(entityType.ClrType)
                    .Property(property.Name)
                    .HasConversion(new Microsoft.EntityFrameworkCore.Storage.ValueConversion.DateTimeOffsetToBinaryConverter());
            }
        }
    }
}

public class ExceptionThrowingPostgresDbContext : PostgresDbContext
{
    public ExceptionThrowingPostgresDbContext(DbContextOptions<PostgresDbContext> options) : base(options)
    {
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("Simulation exception");
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        throw new InvalidOperationException("Simulation exception");
    }
}

public class ActivityServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<PostgresDbContext> _dbOptions;
    private readonly PostgresDbContext _db;
    private readonly IStorageService _storageService;
    private readonly IFileCompressService _fileCompressor;
    private readonly IPermissionService _permissionService;
    private readonly IEnrollmentService _enrollmentService;
    private readonly AbstractMailService _mailService;
    private readonly IMemoryCache _memoryCache;
    private readonly ActivityService _service;
    private readonly Guid _userId = Guid.NewGuid();

    public ActivityServiceTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        _dbOptions = new DbContextOptionsBuilder<PostgresDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new ActivityTestPostgresDbContext(_dbOptions);
        _db.Database.EnsureCreated();

        _storageService = Substitute.For<IStorageService>();
        _fileCompressor = Substitute.For<IFileCompressService>();
        _permissionService = Substitute.For<IPermissionService>();
        _enrollmentService = Substitute.For<IEnrollmentService>();
        _mailService = Substitute.For<AbstractMailService>(
            _db,
            Substitute.For<IPaymentValidationService>(),
            _permissionService,
            NullLogger<AbstractMailService>.Instance
        );
        _memoryCache = Substitute.For<IMemoryCache>();

        _service = new ActivityService(
            _db,
            _storageService,
            _fileCompressor,
            _permissionService,
            _enrollmentService,
            _mailService,
            _memoryCache,
            NullLogger<ActivityService>.Instance
        );
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private Member CreateMember(string studentNumber)
    {
        return new Member
        {
            Id = Guid.NewGuid(),
            StudentNumber = studentNumber,
            FirstName = "Test",
            LastName = "User",
            Email = $"test-{Guid.NewGuid()}@example.com",
            PhoneNumber = "0612345678",
            Street = "Street",
            HouseNumber = "1",
            PostalCode = "1234AB",
            City = "City",
            DateOfBirth = new DateTime(2000, 1, 1),
            Suspended = false,
            PreferredLanguage = Language.EN,
            Gratie = true
        };
    }

    private Activity CreateActivity(string name)
    {
        return new Activity
        {
            Name = name,
            DutchDescription = "Beschrijving",
            EnglishDescription = "Description",
            DateTimeStart = DateTime.UtcNow.AddDays(1),
            DateTimeEnd = DateTime.UtcNow.AddDays(2),
            EnrollOpenDate = null,
            EnrollmentDeadline = null,
            IsEnrollable = true,
            ShowInKoala = true,
            Price = 10,
            IsAdultOnly = false,
            AllowedAudience = TargetAudience.All,
            ParticipantLimit = null,
            PaymentDeadline = DateTimeOffset.UtcNow.AddDays(5),
            Location = "Enschede"
        };
    }

    [Fact]
    public async Task GetActivities_NonBoardIncludesPast_ThrowsUnauthorizedAccessException()
    {
        _permissionService.IsBoardOrCandidateBoardMember(_userId).Returns(false);
        _permissionService.When(p => p.EnsurePermission(_userId, Permission.ViewPastActivities, Arg.Any<uint?>())).Do(_ => throw new UnauthorizedAccessException());

        var dto = new GetActivitiesDTO { IncludePast = true };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.GetActivities(_userId, dto));
    }

    [Fact]
    public async Task GetActivities_Board_ReturnsActivities()
    {
        var activity = CreateActivity("A1");
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        _permissionService.IsBoardOrCandidateBoardMember(_userId).Returns(true);

        var dto = new GetActivitiesDTO { IncludePast = false };
        var result = await _service.GetActivities(_userId, dto);

        Assert.Single(result);
        Assert.Equal("A1", result.First().Name);
    }

    [Fact]
    public async Task GetActivity_NotFound_ReturnsNull()
    {
        var result = await _service.GetActivity(_userId, 999u);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetActivity_EnrollOpenDateInPast_SetsIsEnrollableTrue()
    {
        var activity = CreateActivity("A1");
        activity.IsEnrollable = false;
        activity.EnrollOpenDate = DateTimeOffset.UtcNow.AddMinutes(-5);
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        _permissionService.IsBoardOrCandidateBoardMember(_userId).Returns(true);

        var result = await _service.GetActivity(_userId, activity.Id);

        Assert.NotNull(result);
        Assert.True(result.IsEnrollable);

        _db.ChangeTracker.Clear();
        var saved = await _db.Activities.FindAsync(activity.Id);
        Assert.True(saved?.IsEnrollable);
        Assert.Null(saved?.EnrollOpenDate);
    }

    [Fact]
    public async Task GetActivity_PastActivityNotBoard_ThrowsUnauthorizedAccessException()
    {
        var activity = CreateActivity("A1");
        activity.DateTimeEnd = DateTime.UtcNow.AddDays(-1);
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        _permissionService.IsBoardOrCandidateBoardMember(_userId).Returns(false);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.GetActivity(_userId, activity.Id));
    }

    [Fact]
    public async Task GetActivity_SecretActivityNotBoard_ThrowsUnauthorizedAccessException()
    {
        var group = new Group { Id = 10, Name = "Organizer", Type = GroupType.Committee };
        _db.Groups.Add(group);
        await _db.SaveChangesAsync();

        var activity = CreateActivity("A1");
        activity.ShowInKoala = false;
        activity.OrganizerId = 10;
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        _permissionService.IsBoardOrCandidateBoardMember(_userId).Returns(false);
        _permissionService.IsInGroupInCurrentYear(_userId, 10).Returns(false);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.GetActivity(_userId, activity.Id));
    }

    [Fact]
    public async Task CreateActivity_SavesActivityAndEnrollsOrganizers()
    {
        var member = CreateMember("1234567");
        _db.Members.Add(member);

        var group = new Group { Id = 10, Name = "Organizer Group", Type = GroupType.Committee };
        _db.Groups.Add(group);

        var membership = new GroupMembership
        {
            MemberId = member.Id,
            GroupId = group.Id,
            MembershipYear = Backend.Utils.DateTime.YearUtils.GetYearForDate(System.DateTime.UtcNow, Backend.Utils.DateTime.YearUtils.CommitteeCreationDate),
            RoleAlias = null
        };
        _db.GroupMemberships.Add(membership);

        _db.Settings.Add(new Setting { Name = "BoardGroupId", Value = "1" });
        _db.Settings.Add(new Setting { Name = "CandidateBoardGroupId", Value = "2" });
        await _db.SaveChangesAsync();

        var dto = new PostActivityDTO
        {
            Name = "New Activity",
            Price = 5,
            DutchDescription = "NL",
            EnglishDescription = "EN",
            DateTimeStart = DateTime.UtcNow.AddDays(1),
            DateTimeEnd = DateTime.UtcNow.AddDays(2),
            Location = "Enschede",
            OrganizerId = 10,
            ShowInKoala = true,
            ShowOnWebsite = true,
            IsEnrollable = true,
            AreParticipantsVisible = true,
            IsAdultOnly = false,
            IsWeeklyDrinks = false,
            AllowedAudience = TargetAudience.All,
            VatRate = 21,
            GLAccountId = "GL123",
            CostCenterId = "CC123",
            CostUnitId = "CU123",
            SpecificationQuestionsJson = "[]"
        };

        _permissionService.IsBoardOrCandidateBoardMember(_userId).Returns(true);

        var activity = await _service.CreateActivity(_userId, dto);

        Assert.NotNull(activity);
        _db.ChangeTracker.Clear();
        var saved = await _db.Activities.Include(a => a.Enrollments).FirstOrDefaultAsync(a => a.Id == activity.Id);
        Assert.NotNull(saved);
        Assert.Single(saved.Enrollments);
        Assert.Equal(member.Id, saved.Enrollments.First().MemberId);
    }

    [Fact]
    public async Task CreateActivity_EnrollmentDeadlineAfterEnd_ThrowsArgumentException()
    {
        var dto = new PostActivityDTO
        {
            Name = "New Activity",
            Price = 5,
            DutchDescription = "NL",
            EnglishDescription = "EN",
            DateTimeStart = DateTime.UtcNow.AddDays(1),
            DateTimeEnd = DateTime.UtcNow.AddDays(2),
            EnrollmentDeadline = DateTime.UtcNow.AddDays(3),
            Location = "Enschede",
            ShowInKoala = false,
            ShowOnWebsite = false,
            IsEnrollable = false,
            AreParticipantsVisible = true,
            IsAdultOnly = false,
            IsWeeklyDrinks = false,
            AllowedAudience = TargetAudience.All,
            SpecificationQuestionsJson = "[]"
        };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateActivity(_userId, dto));
    }

    [Fact]
    public async Task CreateActivity_UnenrollmentDeadlineAfterEnd_ThrowsArgumentException()
    {
        var dto = new PostActivityDTO
        {
            Name = "New Activity",
            Price = 5,
            DutchDescription = "NL",
            EnglishDescription = "EN",
            DateTimeStart = DateTime.UtcNow.AddDays(1),
            DateTimeEnd = DateTime.UtcNow.AddDays(2),
            UnenrollmentDeadline = DateTime.UtcNow.AddDays(3),
            Location = "Enschede",
            ShowInKoala = false,
            ShowOnWebsite = false,
            IsEnrollable = false,
            AreParticipantsVisible = true,
            IsAdultOnly = false,
            IsWeeklyDrinks = false,
            AllowedAudience = TargetAudience.All,
            SpecificationQuestionsJson = "[]"
        };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateActivity(_userId, dto));
    }

    [Fact]
    public async Task PatchActivity_EnrollmentDeadlineAfterEnd_ThrowsArgumentException()
    {
        var activity = CreateActivity("A1");
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        var originalEnrollmentDeadline = activity.EnrollmentDeadline;

        _permissionService.IsBoardOrCandidateBoardMember(_userId).Returns(true);

        var patchDoc = new JsonPatchDocument<Activity>();
        patchDoc.Replace(a => a.EnrollmentDeadline, activity.DateTimeEnd.AddDays(1));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.PatchActivity(_userId, activity.Id, patchDoc, CancellationToken.None));

        _db.ChangeTracker.Clear();
        var saved = await _db.Activities.FindAsync(activity.Id);
        Assert.NotNull(saved);
        Assert.Equal(originalEnrollmentDeadline, saved.EnrollmentDeadline);
    }

    [Fact]
    public async Task DeleteActivity_NotFound_ThrowsKeyNotFoundException()
    {
        _permissionService.IsBoardOrCandidateBoardMember(_userId).Returns(true);
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.DeleteActivity(_userId, 999u));
    }

    [Fact]
    public async Task DeleteActivity_Found_DeletesFromDbAndStorage()
    {
        var activity = CreateActivity("A1");
        activity.PosterPath = "poster.webp";
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        _permissionService.IsBoardOrCandidateBoardMember(_userId).Returns(true);

        await _service.DeleteActivity(_userId, activity.Id);

        _db.ChangeTracker.Clear();
        var saved = await _db.Activities.FindAsync(activity.Id);
        Assert.Null(saved);
        await _storageService.Received(1).DeleteFileAsync("posters", "poster.webp");
        _memoryCache.Received(1).Remove("poster-poster.webp");
    }

    [Fact]
    public async Task PatchActivity_NullDoc_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.PatchActivity(_userId, 1u, null!, CancellationToken.None));
    }

    [Fact]
    public async Task PatchActivity_NotFound_ThrowsKeyNotFoundException()
    {
        var patchDoc = new JsonPatchDocument<Activity>();
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.PatchActivity(_userId, 999u, patchDoc, CancellationToken.None));
    }

    [Fact]
    public async Task PatchActivity_PatchRestricted_ThrowsUnauthorizedAccessException()
    {
        var activity = CreateActivity("A1");
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        var patchDoc = new JsonPatchDocument<Activity>();
        patchDoc.Replace(a => a.PosterPath, "new.webp");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.PatchActivity(_userId, activity.Id, patchDoc, CancellationToken.None));
    }

    [Fact]
    public async Task PatchActivity_NonBoardPastActivity_ThrowsUnauthorizedAccessException()
    {
        var activity = CreateActivity("A1");
        activity.DateTimeEnd = DateTime.UtcNow.AddDays(-1);
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        _permissionService.IsBoardOrCandidateBoardMember(_userId).Returns(false);

        var patchDoc = new JsonPatchDocument<Activity>();
        patchDoc.Replace(a => a.Name, "New name");

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.PatchActivity(_userId, activity.Id, patchDoc, CancellationToken.None));
    }

    [Fact]
    public async Task PatchActivity_OrganizerChangesRestrictedField_ThrowsUnauthorizedAccessException()
    {
        var group = new Group { Id = 10, Name = "Organizer", Type = GroupType.Committee };
        _db.Groups.Add(group);

        var activity = CreateActivity("A1");
        activity.ShowInKoala = false;
        activity.OrganizerId = 10;
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        _permissionService.IsBoardOrCandidateBoardMember(_userId).Returns(false);
        _permissionService.IsInGroupInCurrentYear(_userId, 10).Returns(true);

        var patchDoc = new JsonPatchDocument<Activity>();
        patchDoc.Replace(a => a.VatRate, 21u);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.PatchActivity(_userId, activity.Id, patchDoc, CancellationToken.None));
    }

    [Fact]
    public async Task PatchActivity_OrganizerChangesAllowedField_Succeeds()
    {
        var group = new Group { Id = 10, Name = "Organizer", Type = GroupType.Committee };
        _db.Groups.Add(group);

        var activity = CreateActivity("A1");
        activity.ShowInKoala = false;
        activity.OrganizerId = 10;
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        _permissionService.IsBoardOrCandidateBoardMember(_userId).Returns(false);
        _permissionService.IsInGroupInCurrentYear(_userId, 10).Returns(true);
        _permissionService.HasPermission(_userId, Permission.EditActivityForGroup, 10u).Returns(true);

        var patchDoc = new JsonPatchDocument<Activity>();
        patchDoc.Replace(a => a.Name, "Updated by organizer");

        await _service.PatchActivity(_userId, activity.Id, patchDoc, CancellationToken.None);

        _db.ChangeTracker.Clear();
        var saved = await _db.Activities.FindAsync(activity.Id);
        Assert.Equal("Updated by organizer", saved?.Name);
    }

    [Fact]
    public async Task PatchActivity_OrganizerTriesToArchive_ThrowsUnauthorizedAccessException()
    {
        var group = new Group { Id = 10, Name = "Organizer", Type = GroupType.Committee };
        _db.Groups.Add(group);

        var activity = CreateActivity("A1");
        activity.ShowInKoala = false;
        activity.OrganizerId = 10;
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        _permissionService.IsBoardOrCandidateBoardMember(_userId).Returns(false);
        _permissionService.IsInGroupInCurrentYear(_userId, 10).Returns(true);
        _permissionService.HasPermission(_userId, Permission.EditActivityForGroup, 10u).Returns(true);

        var patchDoc = new JsonPatchDocument<Activity>();
        patchDoc.Replace(a => a.IsArchived, true);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.PatchActivity(_userId, activity.Id, patchDoc, CancellationToken.None));
    }

    [Fact]
    public async Task PatchActivity_BoardArchivesActivity_Succeeds()
    {
        var activity = CreateActivity("A1");
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        _permissionService.IsBoardOrCandidateBoardMember(_userId).Returns(true);

        var patchDoc = new JsonPatchDocument<Activity>();
        patchDoc.Replace(a => a.IsArchived, true);

        await _service.PatchActivity(_userId, activity.Id, patchDoc, CancellationToken.None);

        _db.ChangeTracker.Clear();
        var saved = await _db.Activities.FindAsync(activity.Id);
        Assert.True(saved?.IsArchived);
    }

    [Fact]
    public async Task PatchActivity_Valid_AppliesPatchAndUpdatesPricesAndWaitingList()
    {
        var activity = CreateActivity("A1");
        activity.Price = 10;
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        var member = CreateMember("1234567");
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        var enrollment = new Enrollment { MemberId = member.Id, ActivityId = activity.Id, Price = 10, RegisteredOn = DateTime.UtcNow, IsOnWaitingList = true };
        _db.Enrollments.Add(enrollment);
        await _db.SaveChangesAsync();

        _permissionService.IsBoardOrCandidateBoardMember(_userId).Returns(true);

        var patchDoc = new JsonPatchDocument<Activity>();
        patchDoc.Replace(a => a.Price, 20m);
        patchDoc.Replace(a => a.ParticipantLimit, 5u);

        var promoted = new List<Enrollment> { enrollment };
        _enrollmentService.PromoteFromWaitingList(activity.Id, 5, Arg.Any<CancellationToken>())
            .Returns(promoted);

        await _service.PatchActivity(_userId, activity.Id, patchDoc, CancellationToken.None);

        _db.ChangeTracker.Clear();
        var saved = await _db.Activities.FindAsync(activity.Id);
        Assert.Equal(20m, saved?.Price);
        Assert.Equal(5u, saved?.ParticipantLimit);

        var savedEnrollment = await _db.Enrollments.FirstAsync(e => e.MemberId == member.Id);
        Assert.Equal(20m, savedEnrollment.Price);
    }

    [Fact]
    public async Task PatchActivity_EndBeforeStart_ThrowsArgumentException()
    {
        var activity = CreateActivity("A1");
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        var originalDateTimeEnd = activity.DateTimeEnd;

        _permissionService.IsBoardOrCandidateBoardMember(_userId).Returns(true);

        var patchDoc = new JsonPatchDocument<Activity>();
        patchDoc.Replace(a => a.DateTimeEnd, activity.DateTimeStart.AddDays(-1));

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.PatchActivity(_userId, activity.Id, patchDoc, CancellationToken.None));

        _db.ChangeTracker.Clear();
        var saved = await _db.Activities.FindAsync(activity.Id);
        Assert.NotNull(saved);
        Assert.Equal(originalDateTimeEnd, saved.DateTimeEnd, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task PatchActivity_WithSpecificationQuestionsJson_AppliesPatchAndSyncsQuestions()
    {
        var activity = CreateActivity("A1");
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        _permissionService.IsBoardOrCandidateBoardMember(_userId).Returns(true);

        var patchDoc = new JsonPatchDocument<Activity>();
        patchDoc.Replace(a => a.Price, 20m);

        var jsonQuestions = "[{\"questionDutch\": \"Vraag 1\", \"questionEnglish\": \"Question 1\", \"type\": \"String\", \"isMandatory\": true, \"isPublic\": true, \"options\": []}]";
        patchDoc.Operations.Add(new Operation<Activity>("replace", "/SpecificationQuestionsJson", null, jsonQuestions));

        await _service.PatchActivity(_userId, activity.Id, patchDoc, CancellationToken.None);

        _db.ChangeTracker.Clear();
        var saved = await _db.Activities
            .Include(a => a.SpecificationQuestions)
            .FirstOrDefaultAsync(a => a.Id == activity.Id);

        Assert.Equal(20m, saved?.Price);
        Assert.Single(saved?.SpecificationQuestions ?? new List<SpecificationQuestion>());
        var question = saved!.SpecificationQuestions.First();
        Assert.Equal("Vraag 1", question.QuestionDutch);
        Assert.Equal("Question 1", question.QuestionEnglish);
        Assert.True(question.IsMandatory);
    }

    [Fact]
    public async Task UploadPoster_SavesNewPosterAndDeletesOld()
    {
        var activity = CreateActivity("A1");
        activity.PosterPath = "old.webp";
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        var formFile = Substitute.For<IFormFile>();
        formFile.FileName.Returns("new.png");
        formFile.ContentType.Returns("image/png");

        var compressedStream = new MemoryStream(new byte[] { 1, 2, 3 });
        _fileCompressor.CompressFileAsync(formFile)
            .Returns(Task.FromResult((Stream: (Stream)compressedStream, ContentType: "image/webp")));

        _storageService.SaveFileAsync(Arg.Any<Stream>(), "image/webp", "posters")
            .Returns(Task.FromResult("new.webp"));

        _permissionService.IsBoardOrCandidateBoardMember(_userId).Returns(true);

        await _service.UploadPoster(_userId, activity.Id, formFile);

        _db.ChangeTracker.Clear();
        var saved = await _db.Activities.FindAsync(activity.Id);
        Assert.Equal("new.webp", saved?.PosterPath);
        Assert.Equal("new.png", saved?.PosterFileName);

        await _storageService.Received(1).DeleteFileAsync("posters", "old.webp");
        _memoryCache.Received(1).Remove("poster-old.webp");
    }

    [Fact]
    public async Task UpdateActivity_SyncsQuestionsAndProcessesWaitingList()
    {
        var activity = CreateActivity("A1");
        activity.Price = 10;
        var existingQuestion = new SpecificationQuestion
        {
            Id = 100,
            QuestionDutch = "Q1",
            QuestionEnglish = "Q1",
            Type = QuestionType.String,
            IsMandatory = false,
            IsPublic = false,
            Activity = activity
        };
        activity.SpecificationQuestions.Add(existingQuestion);
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        _permissionService.IsBoardOrCandidateBoardMember(_userId).Returns(true);

        var dto = new PutActivityDTO
        {
            Name = "Updated Activity",
            Price = 15m,
            DutchDescription = "NL",
            EnglishDescription = "EN",
            DateTimeStart = DateTime.UtcNow.AddDays(1),
            DateTimeEnd = DateTime.UtcNow.AddDays(2),
            Location = "Enschede",
            ShowInKoala = true,
            ShowOnWebsite = true,
            IsEnrollable = true,
            AreParticipantsVisible = true,
            IsAdultOnly = false,
            IsWeeklyDrinks = false,
            AllowedAudience = TargetAudience.All,
            VatRate = 21,
            GLAccountId = "GL123",
            CostCenterId = "CC123",
            CostUnitId = "CU123",
            SpecificationQuestionsJson = "[{\"id\":100,\"questionDutch\":\"Updated Q1 Dutch\",\"questionEnglish\":\"Updated Q1 English\",\"type\":0,\"isMandatory\":true,\"isPublic\":true},{\"questionDutch\":\"New Q2 Dutch\",\"questionEnglish\":\"New Q2 English\",\"type\":1,\"isMandatory\":false,\"isPublic\":false}]"
        };

        await _service.UpdateActivity(_userId, activity.Id, dto);

        _db.ChangeTracker.Clear();
        var saved = await _db.Activities.Include(a => a.SpecificationQuestions).FirstAsync(a => a.Id == activity.Id);
        Assert.Equal("Updated Activity", saved.Name);
        Assert.Equal(15m, saved.Price);
        Assert.Equal(2, saved.SpecificationQuestions.Count);

        var q1 = saved.SpecificationQuestions.First(q => q.Id == 100);
        Assert.Equal("Updated Q1 Dutch", q1.QuestionDutch);
        Assert.True(q1.IsMandatory);

        var q2 = saved.SpecificationQuestions.First(q => q.Id != 100);
        Assert.Equal("New Q2 Dutch", q2.QuestionDutch);
        Assert.Equal(QuestionType.Boolean, q2.Type);
    }

    [Fact]
    public async Task UpdateActivity_PromotesWaitingList_SendsEmails()
    {
        var activity = CreateActivity("A1");
        activity.ParticipantLimit = 2;
        activity.AllowedAudience = TargetAudience.All;
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        var promoted = new List<Enrollment>
        {
            new Enrollment { MemberId = Guid.NewGuid(), ActivityId = activity.Id, Price = 10, RegisteredOn = DateTime.UtcNow }
        };
        _enrollmentService.PromoteFromWaitingList(activity.Id, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<Enrollment>>(promoted));

        _permissionService.IsBoardOrCandidateBoardMember(_userId).Returns(true);

        var dto = new PutActivityDTO
        {
            Name = activity.Name,
            Price = activity.Price,
            DutchDescription = activity.DutchDescription,
            EnglishDescription = activity.EnglishDescription,
            DateTimeStart = activity.DateTimeStart,
            DateTimeEnd = activity.DateTimeEnd,
            Location = activity.Location,
            ShowInKoala = activity.ShowInKoala,
            ShowOnWebsite = activity.ShowOnWebsite,
            IsEnrollable = activity.IsEnrollable,
            AreParticipantsVisible = activity.AreParticipantsVisible,
            IsAdultOnly = activity.IsAdultOnly,
            IsWeeklyDrinks = activity.IsWeeklyDrinks,
            AllowedAudience = activity.AllowedAudience,
            ParticipantLimit = 5,
        };

        await _service.UpdateActivity(_userId, activity.Id, dto);

        await _mailService.Received(1).SendEnrollmentPromotionEmail(promoted[0]);
    }

    [Fact]
    public async Task UpdateActivity_NonBoardPastActivity_ThrowsUnauthorizedAccessException()
    {
        var activity = CreateActivity("A1");
        activity.DateTimeEnd = DateTime.UtcNow.AddDays(-1);
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        _permissionService.IsBoardOrCandidateBoardMember(_userId).Returns(false);

        var dto = new PutActivityDTO
        {
            Name = activity.Name,
            Price = activity.Price,
            DutchDescription = activity.DutchDescription,
            EnglishDescription = activity.EnglishDescription,
            DateTimeStart = activity.DateTimeStart,
            DateTimeEnd = activity.DateTimeEnd,
            Location = activity.Location,
            ShowInKoala = activity.ShowInKoala,
            ShowOnWebsite = activity.ShowOnWebsite,
            IsEnrollable = activity.IsEnrollable,
            AreParticipantsVisible = activity.AreParticipantsVisible,
            IsAdultOnly = activity.IsAdultOnly,
            IsWeeklyDrinks = activity.IsWeeklyDrinks,
            AllowedAudience = activity.AllowedAudience,
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.UpdateActivity(_userId, activity.Id, dto));
    }

    [Fact]
    public async Task UpdateActivity_OrganizerChangesRestrictedField_SilentlyIgnoresChange()
    {
        var group = new Group { Id = 10, Name = "Organizer", Type = GroupType.Committee };
        _db.Groups.Add(group);

        var activity = CreateActivity("A1");
        activity.ShowInKoala = false;
        activity.ShowOnWebsite = false;
        activity.OrganizerId = 10;
        activity.VatRate = 21;
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        _permissionService.IsBoardOrCandidateBoardMember(_userId).Returns(false);
        _permissionService.IsInGroupInCurrentYear(_userId, 10).Returns(true);
        _permissionService.HasPermission(_userId, Permission.EditActivityForGroup, 10u).Returns(true);

        var dto = new PutActivityDTO
        {
            Name = "Updated by organizer",
            Price = activity.Price,
            DutchDescription = activity.DutchDescription,
            EnglishDescription = activity.EnglishDescription,
            DateTimeStart = activity.DateTimeStart,
            DateTimeEnd = activity.DateTimeEnd,
            Location = activity.Location,
            OrganizerId = activity.OrganizerId,
            ShowInKoala = false,
            ShowOnWebsite = false,
            IsEnrollable = activity.IsEnrollable,
            AreParticipantsVisible = activity.AreParticipantsVisible,
            IsAdultOnly = activity.IsAdultOnly,
            IsWeeklyDrinks = activity.IsWeeklyDrinks,
            AllowedAudience = activity.AllowedAudience,
            // An organizer isn't allowed to touch VatRate - only board members can. Rejecting the
            // request based on this guess would let them learn the real VatRate from whether the
            // request succeeds, so it must be silently ignored instead of rejected.
            VatRate = 9,
        };

        await _service.UpdateActivity(_userId, activity.Id, dto);

        _db.ChangeTracker.Clear();
        var saved = await _db.Activities.FindAsync(activity.Id);
        Assert.Equal("Updated by organizer", saved?.Name);
        Assert.Equal((uint?)21, saved?.VatRate);
    }

    [Fact]
    public async Task UpdateActivity_OrganizerChangesOnlyAllowedFields_Succeeds()
    {
        var group = new Group { Id = 10, Name = "Organizer", Type = GroupType.Committee };
        _db.Groups.Add(group);

        var activity = CreateActivity("A1");
        activity.ShowInKoala = false;
        activity.ShowOnWebsite = false;
        activity.OrganizerId = 10;
        activity.VatRate = 21;
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        _permissionService.IsBoardOrCandidateBoardMember(_userId).Returns(false);
        _permissionService.IsInGroupInCurrentYear(_userId, 10).Returns(true);
        _permissionService.HasPermission(_userId, Permission.EditActivityForGroup, 10u).Returns(true);

        var dto = new PutActivityDTO
        {
            Name = "Updated by organizer",
            Price = activity.Price,
            DutchDescription = activity.DutchDescription,
            EnglishDescription = activity.EnglishDescription,
            DateTimeStart = activity.DateTimeStart,
            DateTimeEnd = activity.DateTimeEnd,
            Location = activity.Location,
            OrganizerId = activity.OrganizerId,
            ShowInKoala = false,
            ShowOnWebsite = false,
            IsEnrollable = activity.IsEnrollable,
            AreParticipantsVisible = activity.AreParticipantsVisible,
            IsAdultOnly = activity.IsAdultOnly,
            IsWeeklyDrinks = activity.IsWeeklyDrinks,
            AllowedAudience = activity.AllowedAudience,
            VatRate = activity.VatRate,
            GLAccountId = activity.GLAccountId,
            CostCenterId = activity.CostCenterId,
            CostUnitId = activity.CostUnitId,
            PaymentDeadline = activity.PaymentDeadline,
        };

        await _service.UpdateActivity(_userId, activity.Id, dto);

        _db.ChangeTracker.Clear();
        var saved = await _db.Activities.FindAsync(activity.Id);
        Assert.Equal("Updated by organizer", saved?.Name);
    }

    [Fact]
    public async Task GetPoster_Cached_ReturnsFromCache()
    {
        var activity = CreateActivity("A1");
        activity.PosterPath = "path.webp";
        activity.PosterFileName = "poster.png";
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        var cachedBytes = new byte[] { 4, 5, 6 };
        object? cachedVal = (cachedBytes, "image/webp");
        _memoryCache.TryGetValue("poster-path.webp", out Arg.Any<object?>())
            .Returns(x =>
            {
                x[1] = cachedVal;
                return true;
            });

        var result = await _service.GetPoster(_userId, activity.Id, true);

        Assert.NotNull(result);
        Assert.Equal("image/webp", result.Value.ContentType);
        Assert.Equal("poster.png", result.Value.FileName);
    }

    [Fact]
    public async Task GetPoster_NotCached_LoadsFromStorageAndCaches()
    {
        var activity = CreateActivity("A1");
        activity.PosterPath = "path.webp";
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        _memoryCache.TryGetValue("poster-path.webp", out Arg.Any<object?>()).Returns(false);

        var fileStream = new MemoryStream(new byte[] { 10, 11 });
        var storageFile = new StorageFile(fileStream, "image/png", "path.webp");
        _storageService.GetFileAsync("posters", "path.webp").Returns(Task.FromResult<StorageFile?>(storageFile));

        var mockEntry = Substitute.For<ICacheEntry>();
        _memoryCache.CreateEntry(Arg.Any<object>()).Returns(mockEntry);

        var result = await _service.GetPoster(_userId, activity.Id, false);

        Assert.NotNull(result);
        Assert.Equal("image/png", result.Value.ContentType);
        _memoryCache.Received(1).CreateEntry("poster-path.webp");
        mockEntry.Received(1).Value = Arg.Is<(byte[] bytes, string contentType)>(val => val.contentType == "image/png" && val.bytes.SequenceEqual(new byte[] { 10, 11 }));
    }

    [Fact]
    public async Task GetEnrollmentsCsv_GeneratesValidCsv()
    {
        var member = CreateMember("1234567");
        member.PreferredLanguage = Language.EN;
        _db.Members.Add(member);

        var activity = CreateActivity("A1");
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        var enrollment = new Enrollment { MemberId = member.Id, ActivityId = activity.Id, Price = 10, RegisteredOn = DateTime.UtcNow, IsOnWaitingList = false };
        _db.Enrollments.Add(enrollment);
        await _db.SaveChangesAsync();

        _permissionService.IsBoardOrCandidateBoardMember(member.Id).Returns(true);

        var result = await _service.GetEnrollmentsCsv(member.Id, activity.Id, CancellationToken.None);

        Assert.NotNull(result.Content);
        Assert.Contains("First Name;Last Name;On Waiting List", Encoding.UTF8.GetString(result.Content));
    }

    [Fact]
    public async Task CreateActivity_ThrowsException_RollsBack()
    {
        using var throwDb = new ExceptionThrowingPostgresDbContext(_dbOptions);
        throwDb.Database.EnsureCreated();
        var service = new ActivityService(
            throwDb, _storageService, _fileCompressor, _permissionService,
            _enrollmentService, _mailService, _memoryCache,
            NullLogger<ActivityService>.Instance
        );

        _permissionService.IsBoardOrCandidateBoardMember(_userId).Returns(true);

        var dto = new PostActivityDTO
        {
            Name = "A1",
            Price = 10,
            DutchDescription = "NL",
            EnglishDescription = "EN",
            DateTimeStart = DateTime.UtcNow.AddDays(1),
            DateTimeEnd = DateTime.UtcNow.AddDays(2),
            Location = "Enschede",
            ShowInKoala = true,
            ShowOnWebsite = true,
            IsEnrollable = true,
            AreParticipantsVisible = true,
            IsAdultOnly = false,
            IsWeeklyDrinks = false,
            AllowedAudience = TargetAudience.All,
            VatRate = 21,
            GLAccountId = "GL123",
            CostCenterId = "CC123",
            CostUnitId = "CU123",
            SpecificationQuestionsJson = "[]"
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateActivity(_userId, dto));
    }

    [Fact]
    public async Task PatchActivity_PromotesWaitingList_SendsEmails_HandlesException()
    {
        var activity = CreateActivity("A1");
        activity.ParticipantLimit = 2;
        activity.AllowedAudience = TargetAudience.All;
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        var promoted = new List<Enrollment>
        {
            new Enrollment { MemberId = Guid.NewGuid(), ActivityId = activity.Id, Price = 10, RegisteredOn = DateTime.UtcNow }
        };
        _enrollmentService.PromoteFromWaitingList(activity.Id, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IEnumerable<Enrollment>>(promoted));

        _mailService.SendEnrollmentPromotionEmail(promoted[0]).Throws(new Exception("Mail error"));

        _permissionService.IsBoardOrCandidateBoardMember(_userId).Returns(true);

        var patchDoc = new Microsoft.AspNetCore.JsonPatch.JsonPatchDocument<Activity>();
        patchDoc.Replace(a => a.ParticipantLimit, (uint?)5);

        await _service.PatchActivity(_userId, activity.Id, patchDoc, CancellationToken.None);

        await _mailService.Received(1).SendEnrollmentPromotionEmail(promoted[0]);
    }

    [Fact]
    public async Task PatchActivity_ThrowsException_RollsBack()
    {
        var activity = CreateActivity("A1");
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        _permissionService.IsBoardOrCandidateBoardMember(_userId).Returns(true);

        _db.Database.GetDbConnection().Close();

        var patchDoc = new Microsoft.AspNetCore.JsonPatch.JsonPatchDocument<Activity>();
        patchDoc.Replace(a => a.Name, "New Name");

        await Assert.ThrowsAnyAsync<Exception>(() =>
            _service.PatchActivity(_userId, activity.Id, patchDoc, CancellationToken.None));
    }

    [Fact]
    public async Task UploadPoster_ActivityNotFound_ThrowsKeyNotFoundException()
    {
        var formFile = Substitute.For<IFormFile>();
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.UploadPoster(_userId, 999, formFile));
    }

    [Fact]
    public async Task UploadPoster_NullPoster_ClearsPoster()
    {
        var activity = CreateActivity("A1");
        activity.PosterPath = "old.webp";
        activity.PosterFileName = "old.png";
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        _permissionService.IsBoardOrCandidateBoardMember(_userId).Returns(true);

        await _service.UploadPoster(_userId, activity.Id, null);

        _db.ChangeTracker.Clear();
        var saved = await _db.Activities.FindAsync(activity.Id);
        Assert.Null(saved?.PosterPath);
        Assert.Null(saved?.PosterFileName);
        await _storageService.Received(1).DeleteFileAsync("posters", "old.webp");
    }

    [Fact]
    public async Task UploadPoster_ThrowsException_RollsBack()
    {
        var activity = CreateActivity("A1");
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        var formFile = Substitute.For<IFormFile>();
        formFile.FileName.Returns("test.png");
        formFile.ContentType.Returns("image/png");

        _fileCompressor.CompressFileAsync(formFile).Throws(new InvalidOperationException("Compress error"));

        _permissionService.IsBoardOrCandidateBoardMember(_userId).Returns(true);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.UploadPoster(_userId, activity.Id, formFile));
    }

    [Fact]
    public async Task UpdateActivity_ActivityNotFound_ThrowsKeyNotFoundException()
    {
        var dto = new PutActivityDTO
        {
            Name = "Updated",
            Price = 10m,
            DutchDescription = "NL",
            EnglishDescription = "EN",
            DateTimeStart = DateTime.UtcNow.AddDays(1),
            DateTimeEnd = DateTime.UtcNow.AddDays(2),
            Location = "Enschede",
            ShowInKoala = true,
            ShowOnWebsite = true,
            IsEnrollable = true,
            AreParticipantsVisible = true,
            IsAdultOnly = false,
            IsWeeklyDrinks = false,
            AllowedAudience = TargetAudience.All,
            VatRate = 21,
            GLAccountId = "GL123",
            CostCenterId = "CC123",
            CostUnitId = "CU123",
            SpecificationQuestionsJson = "[]"
        };
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.UpdateActivity(_userId, 999, dto));
    }

    [Fact]
    public async Task UpdateActivity_ChangesPrice_UpdatesEnrollments()
    {
        var member = CreateMember("1234567");
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        var activity = CreateActivity("A1");
        activity.Price = 10m;
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        var enrollment = new Enrollment { MemberId = member.Id, ActivityId = activity.Id, Price = 10m, RegisteredOn = DateTime.UtcNow, IsOnWaitingList = false };
        _db.Enrollments.Add(enrollment);
        await _db.SaveChangesAsync();

        _permissionService.IsBoardOrCandidateBoardMember(_userId).Returns(true);

        var dto = new PutActivityDTO
        {
            Name = "A1",
            Price = 20m,
            DutchDescription = "NL",
            EnglishDescription = "EN",
            DateTimeStart = DateTime.UtcNow.AddDays(1),
            DateTimeEnd = DateTime.UtcNow.AddDays(2),
            Location = "Enschede",
            ShowInKoala = true,
            ShowOnWebsite = true,
            IsEnrollable = true,
            AreParticipantsVisible = true,
            IsAdultOnly = false,
            IsWeeklyDrinks = false,
            AllowedAudience = TargetAudience.All,
            VatRate = 21,
            GLAccountId = "GL123",
            CostCenterId = "CC123",
            CostUnitId = "CU123",
            SpecificationQuestionsJson = "[]"
        };

        await _service.UpdateActivity(_userId, activity.Id, dto);

        _db.ChangeTracker.Clear();
        var savedEnrollment = await _db.Enrollments.FirstAsync(e => e.MemberId == enrollment.MemberId);
        Assert.Equal(20m, savedEnrollment.Price);
    }

    [Fact]
    public async Task UpdateActivity_NoWaitingListChange_ReachesElseBlock()
    {
        var activity = CreateActivity("A1");
        activity.ParticipantLimit = 5;
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        _permissionService.IsBoardOrCandidateBoardMember(_userId).Returns(true);

        var dto = new PutActivityDTO
        {
            Name = "A1",
            Price = 10m,
            DutchDescription = "NL",
            EnglishDescription = "EN",
            DateTimeStart = DateTime.UtcNow.AddDays(1),
            DateTimeEnd = DateTime.UtcNow.AddDays(2),
            Location = "Enschede",
            ParticipantLimit = 5,
            ShowInKoala = true,
            ShowOnWebsite = true,
            IsEnrollable = true,
            AreParticipantsVisible = true,
            IsAdultOnly = false,
            IsWeeklyDrinks = false,
            AllowedAudience = TargetAudience.All,
            VatRate = 21,
            GLAccountId = "GL123",
            CostCenterId = "CC123",
            CostUnitId = "CU123",
            SpecificationQuestionsJson = "[]"
        };

        await _service.UpdateActivity(_userId, activity.Id, dto);

        await _enrollmentService.DidNotReceive().PromoteFromWaitingList(Arg.Any<uint>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateActivity_WithExistingAndNewPoster_DeletesOldPoster()
    {
        var activity = CreateActivity("A1");
        activity.PosterPath = "old.webp";
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        var formFile = Substitute.For<IFormFile>();
        formFile.FileName.Returns("new.png");
        formFile.ContentType.Returns("image/png");

        var compressedStream = new MemoryStream(new byte[] { 1, 2, 3 });
        _fileCompressor.CompressFileAsync(formFile)
            .Returns(Task.FromResult((Stream: (Stream)compressedStream, ContentType: "image/webp")));

        _storageService.SaveFileAsync(Arg.Any<Stream>(), "image/webp", "posters")
            .Returns(Task.FromResult("new.webp"));

        _permissionService.IsBoardOrCandidateBoardMember(_userId).Returns(true);

        var dto = new PutActivityDTO
        {
            Name = "A1",
            Price = 10m,
            DutchDescription = "NL",
            EnglishDescription = "EN",
            DateTimeStart = DateTime.UtcNow.AddDays(1),
            DateTimeEnd = DateTime.UtcNow.AddDays(2),
            Location = "Enschede",
            ShowInKoala = true,
            ShowOnWebsite = true,
            IsEnrollable = true,
            AreParticipantsVisible = true,
            IsAdultOnly = false,
            IsWeeklyDrinks = false,
            AllowedAudience = TargetAudience.All,
            VatRate = 21,
            GLAccountId = "GL123",
            CostCenterId = "CC123",
            CostUnitId = "CU123",
            SpecificationQuestionsJson = "[]",
            Poster = formFile
        };

        await _service.UpdateActivity(_userId, activity.Id, dto);

        await _storageService.Received(1).DeleteFileAsync("posters", "old.webp");
        _memoryCache.Received(1).Remove("poster-old.webp");
    }

    [Fact]
    public async Task UpdateActivity_ThrowsException_RollsBack()
    {
        var activity = CreateActivity("A1");
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        _permissionService.IsBoardOrCandidateBoardMember(_userId).Returns(true);

        _db.Database.GetDbConnection().Close();

        var dto = new PutActivityDTO
        {
            Name = "Updated",
            Price = 10m,
            DutchDescription = "NL",
            EnglishDescription = "EN",
            DateTimeStart = DateTime.UtcNow.AddDays(1),
            DateTimeEnd = DateTime.UtcNow.AddDays(2),
            Location = "Enschede",
            ShowInKoala = true,
            ShowOnWebsite = true,
            IsEnrollable = true,
            AreParticipantsVisible = true,
            IsAdultOnly = false,
            IsWeeklyDrinks = false,
            AllowedAudience = TargetAudience.All,
            VatRate = 21,
            GLAccountId = "GL123",
            CostCenterId = "CC123",
            CostUnitId = "CU123",
            SpecificationQuestionsJson = "[]"
        };

        await Assert.ThrowsAnyAsync<Exception>(() =>
            _service.UpdateActivity(_userId, activity.Id, dto));
    }

    [Fact]
    public async Task GetPoster_NullOrEmptyPosterPath_ReturnsNull()
    {
        var activity = CreateActivity("A1");
        activity.PosterPath = null;
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        var result = await _service.GetPoster(_userId, activity.Id, false);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetPoster_PastActivity_EnforcesBoardPermission()
    {
        var activity = CreateActivity("A1");
        activity.PosterPath = "path.webp";
        activity.DateTimeEnd = DateTime.UtcNow.AddDays(-1);
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        var cachedBytes = new byte[] { 4, 5, 6 };
        object? cachedVal = (cachedBytes, "image/webp");
        _memoryCache.TryGetValue("poster-path.webp", out Arg.Any<object?>())
            .Returns(x =>
            {
                x[1] = cachedVal;
                return true;
            });

        var result = await _service.GetPoster(_userId, activity.Id, false);

        Assert.NotNull(result);
        _permissionService.Received(1).EnsureBoardOrCandidateBoardMember(_userId);
    }

    [Fact]
    public async Task GetPoster_NotShownInKoala_EnforcesBoardPermission()
    {
        var group = new Group { Id = 999, Name = "Test Organizer Group", Type = GroupType.Committee };
        _db.Groups.Add(group);
        await _db.SaveChangesAsync();

        var activity = CreateActivity("A1");
        activity.PosterPath = "path.webp";
        activity.ShowInKoala = false;
        activity.OrganizerId = 999;
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        var cachedBytes = new byte[] { 4, 5, 6 };
        object? cachedVal = (cachedBytes, "image/webp");
        _memoryCache.TryGetValue("poster-path.webp", out Arg.Any<object?>())
            .Returns(x =>
            {
                x[1] = cachedVal;
                return true;
            });

        _permissionService.IsInGroupInCurrentYear(_userId, 999).Returns(false);

        var result = await _service.GetPoster(_userId, activity.Id, false);

        Assert.NotNull(result);
        _permissionService.Received(1).EnsureBoardOrCandidateBoardMember(_userId);
    }

    [Fact]
    public async Task GetPoster_StorageReturnsNull_ReturnsNull()
    {
        var activity = CreateActivity("A1");
        activity.PosterPath = "path.webp";
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        _memoryCache.TryGetValue("poster-path.webp", out Arg.Any<object?>()).Returns(false);
        _storageService.GetFileAsync("posters", "path.webp").Returns(Task.FromResult<StorageFile?>(null));

        var result = await _service.GetPoster(_userId, activity.Id, false);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetPoster_GuestUserFutureShowOnWebsite_Succeeds()
    {
        var activity = CreateActivity("A1");
        activity.PosterPath = "path.webp";
        activity.ShowOnWebsite = true;
        activity.DateTimeEnd = DateTime.UtcNow.AddDays(5);
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        var cachedBytes = new byte[] { 4, 5, 6 };
        object? cachedVal = (cachedBytes, "image/webp");
        _memoryCache.TryGetValue("poster-path.webp", out Arg.Any<object?>())
            .Returns(x =>
            {
                x[1] = cachedVal;
                return true;
            });

        var result = await _service.GetPoster(null, activity.Id, false);

        Assert.NotNull(result);
        _permissionService.DidNotReceiveWithAnyArgs().EnsureBoardOrCandidateBoardMember(Arg.Any<Guid>());
    }

    [Fact]
    public async Task GetPoster_GuestUserNotShownOnWebsite_ThrowsUnauthorizedAccessException()
    {
        var activity = CreateActivity("A1");
        activity.PosterPath = "path.webp";
        activity.ShowOnWebsite = false;
        activity.DateTimeEnd = DateTime.UtcNow.AddDays(5);
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.GetPoster(null, activity.Id, false));
    }

    [Fact]
    public async Task GetEnrollmentsCsv_UserNotFound_ThrowsUnauthorizedAccessException()
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.GetEnrollmentsCsv(Guid.NewGuid(), 1, CancellationToken.None));
    }

    [Fact]
    public async Task GetEnrollmentsCsv_ActivityNotFound_ThrowsKeyNotFoundException()
    {
        var member = CreateMember("1234567");
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        _permissionService.IsBoardOrCandidateBoardMember(member.Id).Returns(true);

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.GetEnrollmentsCsv(member.Id, 999, CancellationToken.None));
    }

    [Fact]
    public async Task GetEnrollmentsCsv_WithQuestionsAndAnswers_GeneratesCorrectCsv()
    {
        var member = CreateMember("1234567");
        member.FirstName = "John";
        member.LastName = "Doe";
        member.PreferredLanguage = Language.NL;
        _db.Members.Add(member);

        var activity = CreateActivity("A1");
        var question = new SpecificationQuestion
        {
            Id = 50,
            QuestionDutch = "Vraag1",
            QuestionEnglish = "Question1",
            Type = QuestionType.String,
            Activity = activity
        };
        activity.SpecificationQuestions.Add(question);
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        var enrollment = new Enrollment
        {
            MemberId = member.Id,
            ActivityId = activity.Id,
            Price = 10,
            RegisteredOn = DateTime.UtcNow,
            IsOnWaitingList = false,
            SpecificationAnswers = new List<SpecificationAnswer>()
        };
        var answer = new SpecificationAnswer
        {
            SpecificationQuestionId = 50,
            Answer = "Antwoord;Met;Puntkomma",
            MemberId = member.Id,
            Enrollment = enrollment
        };
        enrollment.SpecificationAnswers.Add(answer);
        _db.Enrollments.Add(enrollment);
        await _db.SaveChangesAsync();

        _db.ChangeTracker.Clear();

        _permissionService.IsBoardOrCandidateBoardMember(member.Id).Returns(true);

        var result = await _service.GetEnrollmentsCsv(member.Id, activity.Id, CancellationToken.None);

        Assert.NotNull(result.Content);
        var csvStr = Encoding.UTF8.GetString(result.Content);
        Assert.Contains("Voornaam;Achternaam;Op Wachtlijst;Vraag1", csvStr);
        Assert.Contains("John;Doe;False;Antwoord,Met,Puntkomma", csvStr);
    }

    [Fact]
    public async Task CreateActivity_WithSpecificationQuestions_MapsQuestions()
    {
        _permissionService.IsBoardOrCandidateBoardMember(_userId).Returns(true);

        var dto = new PostActivityDTO
        {
            Name = "A1",
            Price = 10,
            DutchDescription = "NL",
            EnglishDescription = "EN",
            DateTimeStart = DateTime.UtcNow.AddDays(1),
            DateTimeEnd = DateTime.UtcNow.AddDays(2),
            Location = "Enschede",
            ShowInKoala = true,
            ShowOnWebsite = true,
            IsEnrollable = true,
            AreParticipantsVisible = true,
            IsAdultOnly = false,
            IsWeeklyDrinks = false,
            AllowedAudience = TargetAudience.All,
            VatRate = 21,
            GLAccountId = "GL123",
            CostCenterId = "CC123",
            CostUnitId = "CU123",
            SpecificationQuestionsJson = "[{\"questionDutch\":\"QD\",\"questionEnglish\":\"QE\",\"type\":0,\"isMandatory\":true,\"isPublic\":true,\"options\":[\"O1\",\"O2\"]}]"
        };

        var result = await _service.CreateActivity(_userId, dto);

        Assert.NotNull(result);
        Assert.Single(result.SpecificationQuestions);
        var q = result.SpecificationQuestions.First();
        Assert.Equal("QD", q.QuestionDutch);
        Assert.Equal("O1;O2", q.Options);
    }

    [Fact]
    public async Task UpdateActivity_InvalidQuestionId_ThrowsException()
    {
        var activity = CreateActivity("A1");
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        _permissionService.IsBoardOrCandidateBoardMember(_userId).Returns(true);

        var dto = new PutActivityDTO
        {
            Name = "Updated Name",
            Price = 10m,
            DutchDescription = "NL",
            EnglishDescription = "EN",
            DateTimeStart = DateTime.UtcNow.AddDays(1),
            DateTimeEnd = DateTime.UtcNow.AddDays(2),
            Location = "Enschede",
            ShowInKoala = true,
            ShowOnWebsite = true,
            IsEnrollable = true,
            AreParticipantsVisible = true,
            IsAdultOnly = false,
            IsWeeklyDrinks = false,
            AllowedAudience = TargetAudience.All,
            VatRate = 21,
            GLAccountId = "GL123",
            CostCenterId = "CC123",
            CostUnitId = "CU123",
            SpecificationQuestionsJson = "[{\"id\":999,\"questionDutch\":\"Q\",\"questionEnglish\":\"Q\",\"type\":0,\"isMandatory\":true,\"isPublic\":true}]"
        };

        await Assert.ThrowsAnyAsync<Exception>(() =>
            _service.UpdateActivity(_userId, activity.Id, dto));
    }
}
