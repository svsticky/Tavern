using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Backend.Controllers.DTOs;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models;
using Backend.Models.Domain;
using Backend.Services.Domain;
using Backend.Services;
using Backend.Services.PaymentServices;
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

public class MemberServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<PostgresDbContext> _dbOptions;
    private readonly PostgresDbContext _db;
    private readonly IPermissionService _permissionService;
    private readonly IPaymentValidationService _paymentValidationService;
    private readonly IStorageService _storageService;
    private readonly AbstractPaymentService _paymentService;
    private readonly AuthOutboxWorker _authOutboxWorker;
    private readonly MailSubscriptionOutboxWorker _mailSubscriptionOutboxWorker;
    private readonly IAuthService _authService;
    private readonly IMailSubscriptionService _mailSubscriptionService;
    private readonly IMailinglistCurationService _mailinglistCurationService;
    private readonly IMemoryCache _memoryCache;
    private readonly MemberService _service;
    private readonly Guid _userId = Guid.NewGuid();

    public MemberServiceTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        _dbOptions = new DbContextOptionsBuilder<PostgresDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new TestPostgresDbContext(_dbOptions);
        _db.Database.EnsureCreated();

        _permissionService = Substitute.For<IPermissionService>();
        _paymentValidationService = Substitute.For<IPaymentValidationService>();
        _storageService = Substitute.For<IStorageService>();
        _paymentService = Substitute.For<AbstractPaymentService>(null, null);
        _authOutboxWorker = Substitute.For<AuthOutboxWorker>(null, NullLogger<AuthOutboxWorker>.Instance);
        _mailSubscriptionOutboxWorker = Substitute.For<MailSubscriptionOutboxWorker>(null, NullLogger<MailSubscriptionOutboxWorker>.Instance);
        _authService = Substitute.For<IAuthService>();
        _mailSubscriptionService = Substitute.For<IMailSubscriptionService>();
        _mailinglistCurationService = Substitute.For<IMailinglistCurationService>();
        _memoryCache = Substitute.For<IMemoryCache>();

        // Sensible defaults so tests that don't care about mailing lists don't have to mock these -
        // no curated lists, and the member isn't subscribed to anything at the provider.
        _mailinglistCurationService.GetVisibleProviderListIds(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new HashSet<string>());
        _mailSubscriptionService.GetMemberMailinglistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new List<MemberMailinglistDto>());

        _service = new MemberService(
            _db,
            _permissionService,
            _paymentValidationService,
            _storageService,
            _paymentService,
            _authOutboxWorker,
            _mailSubscriptionOutboxWorker,
            _authService,
            _mailSubscriptionService,
            _mailinglistCurationService,
            _memoryCache,
            NullLogger<MemberService>.Instance
        );
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private Member CreateTestMember(Guid id, string email = "test@example.com")
    {
        return new Member
        {
            Id = id,
            StudentNumber = "s" + id.ToString("N").Substring(0, 7),
            FirstName = "Test",
            LastName = "User",
            Email = email,
            PhoneNumber = "0612345678",
            Street = "Street",
            HouseNumber = "1",
            PostalCode = "1234AB",
            City = "Enschede",
            DateOfBirth = DateTimeOffset.UtcNow.AddYears(-20),
            AuthSystemUserId = Guid.NewGuid(),
            PreferredLanguage = Language.NL
        };
    }

    [Fact]
    public async Task GetMembers_AsBoard_ReturnsMembers()
    {
        // Arrange
        var member = CreateTestMember(Guid.NewGuid());
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        var dto = new GetMembersDto();

        // Act
        var result = await _service.GetMembers(dto, _userId, CancellationToken.None);

        // Assert
        Assert.NotEmpty(result);
        _permissionService.Received(1).EnsurePermission(_userId, Permission.ViewMembers, Arg.Any<uint?>());
    }

    [Fact]
    public async Task GetMember_Self_ReturnsMember()
    {
        // Arrange
        var member = CreateTestMember(_userId);
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        _permissionService.IsBoardOrCandidateBoardMember(_userId).Returns(false);

        // Act
        var result = await _service.GetMember(_userId, _userId, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(_userId, result.Id);
    }

    [Fact]
    public async Task GetMember_OtherAsBoard_ReturnsMember()
    {
        // Arrange
        var otherId = Guid.NewGuid();
        var member = CreateTestMember(otherId);
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        _permissionService.IsBoardOrCandidateBoardMember(_userId).Returns(true);

        // Act
        var result = await _service.GetMember(otherId, _userId, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(otherId, result.Id);
    }

    [Fact]
    public async Task CreateMember_BegunstigerWithoutUser_ThrowsUnauthorized()
    {
        // Arrange
        var dto = new PostMemberDTO
        {
            StudentNumber = "123456",
            FirstName = "A",
            LastName = "B",
            Email = "a@b.com",
            PhoneNumber = "0612345678",
            Street = "S",
            HouseNumber = "1",
            PostalCode = "1",
            City = "C",
            DateOfBirth = DateTimeOffset.UtcNow.AddYears(-20),
            Begunstiger = true,
            PreferredLanguage = Language.NL
        };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.CreateMember(dto, null, CancellationToken.None));
    }

    [Fact]
    public async Task CreateMember_BegunstigerNotBoard_ThrowsUnauthorized()
    {
        // Arrange
        var dto = new PostMemberDTO
        {
            StudentNumber = "123456",
            FirstName = "A",
            LastName = "B",
            Email = "a@b.com",
            PhoneNumber = "0612345678",
            Street = "S",
            HouseNumber = "1",
            PostalCode = "1",
            City = "C",
            DateOfBirth = DateTimeOffset.UtcNow.AddYears(-20),
            Begunstiger = true,
            PreferredLanguage = Language.NL
        };

        _permissionService.When(p => p.EnsurePermission(_userId, Permission.ManageMembers, Arg.Any<uint?>()))
            .Do(x => throw new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.CreateMember(dto, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task CreateMember_NoStudyEnrollments_ThrowsArgumentException()
    {
        // Arrange
        var dto = new PostMemberDTO
        {
            StudentNumber = "123456",
            FirstName = "A",
            LastName = "B",
            Email = "a@b.com",
            PhoneNumber = "0612345678",
            Street = "S",
            HouseNumber = "1",
            PostalCode = "1",
            City = "C",
            DateOfBirth = DateTimeOffset.UtcNow.AddYears(-20),
            StudyEnrollments = new List<PostStudyEnrollmentDTO>(),
            PreferredLanguage = Language.NL
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateMember(dto, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task CreateMember_EmptyStudentNumber_ThrowsArgumentException()
    {
        // Arrange
        var dto = new PostMemberDTO
        {
            StudentNumber = "",
            FirstName = "A",
            LastName = "B",
            Email = "a@b.com",
            PhoneNumber = "0612345678",
            Street = "S",
            HouseNumber = "1",
            PostalCode = "1",
            City = "C",
            DateOfBirth = DateTimeOffset.UtcNow.AddYears(-20),
            PreferredLanguage = Language.NL,
            StudyEnrollments = new List<PostStudyEnrollmentDTO>
            {
                new PostStudyEnrollmentDTO { StudyId = 1, MemberId = Guid.Empty, EnrollmentDate = new DateTimeOffset(new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc)), Status = StudyStatus.Enrolled }
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateMember(dto, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task CreateMember_FutureDOB_ThrowsArgumentException()
    {
        // Arrange
        var dto = new PostMemberDTO
        {
            StudentNumber = "123456",
            FirstName = "A",
            LastName = "B",
            Email = "a@b.com",
            PhoneNumber = "0612345678",
            Street = "S",
            HouseNumber = "1",
            PostalCode = "1",
            City = "C",
            DateOfBirth = DateTimeOffset.UtcNow.AddYears(1),
            PreferredLanguage = Language.NL,
            StudyEnrollments = new List<PostStudyEnrollmentDTO>
            {
                new PostStudyEnrollmentDTO { StudyId = 1, MemberId = Guid.Empty, EnrollmentDate = new DateTimeOffset(new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc)), Status = StudyStatus.Enrolled }
            }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateMember(dto, _userId, CancellationToken.None));
        Assert.Equal("Date of birth must be in the past.", ex.Message);
    }

    [Fact]
    public async Task CreateMember_MinorNoParentPhone_ThrowsArgumentException()
    {
        // Arrange
        var dto = new PostMemberDTO
        {
            StudentNumber = "123456",
            FirstName = "A",
            LastName = "B",
            Email = "a@b.com",
            PhoneNumber = "0612345678",
            Street = "S",
            HouseNumber = "1",
            PostalCode = "1",
            City = "C",
            DateOfBirth = DateTimeOffset.UtcNow.AddYears(-17), // 17 years old
            ParentPhoneNumber = null,
            PreferredLanguage = Language.NL,
            StudyEnrollments = new List<PostStudyEnrollmentDTO>
            {
                new PostStudyEnrollmentDTO { StudyId = 1, MemberId = Guid.Empty, EnrollmentDate = new DateTimeOffset(new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc)), Status = StudyStatus.Enrolled }
            }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateMember(dto, _userId, CancellationToken.None));
        Assert.Equal("Parent phone number required for minors.", ex.Message);
    }

    [Fact]
    public async Task CreateMember_ValidRequest_SavesMember()
    {
        // Arrange
        var study = new Study { Id = 1, Title = "Study", NominalDurationYears = 3, Type = StudyType.Bachelor };
        _db.Studies.Add(study);
        await _db.SaveChangesAsync();

        var dto = new PostMemberDTO
        {
            StudentNumber = "123456",
            FirstName = "A",
            LastName = "B",
            Email = "a@b.com",
            PhoneNumber = "0612345678",
            Street = "S",
            HouseNumber = "1",
            PostalCode = "1",
            City = "C",
            DateOfBirth = DateTimeOffset.UtcNow.AddYears(-20),
            PreferredLanguage = Language.NL,
            SubscribedMailinglistIds = new List<string> { "id_news" },
            StudyEnrollments = new List<PostStudyEnrollmentDTO>
            {
                new PostStudyEnrollmentDTO { StudyId = 1, MemberId = Guid.Empty, EnrollmentDate = new DateTimeOffset(new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc)), Status = StudyStatus.Enrolled }
            }
        };

        // Act
        var result = await _service.CreateMember(dto, _userId, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        _db.ChangeTracker.Clear();
        var saved = await _db.Members.FindAsync(result.Id);
        Assert.NotNull(saved);
        Assert.Equal("a@b.com", saved.Email);
        _authOutboxWorker.Received(1).EnqueueTask(AuthTaskType.Create, result.Id, Arg.Any<PostgresDbContext>());
        _mailSubscriptionOutboxWorker.Received(1).EnqueueUpdateSubscriptionsTask(
            "a@b.com",
            Arg.Is<IEnumerable<string>>(ids => ids.SequenceEqual(new[] { "id_news" })),
            _db);
    }

    [Fact]
    public async Task CreateMember_BegunstigerAsBoardMember_Succeeds()
    {
        // Arrange
        var dto = new PostMemberDTO
        {
            StudentNumber = "123456",
            FirstName = "A",
            LastName = "B",
            Email = "begunstiger@b.com",
            PhoneNumber = "0612345678",
            Street = "S",
            HouseNumber = "1",
            PostalCode = "1",
            City = "C",
            DateOfBirth = DateTimeOffset.UtcNow.AddYears(-20),
            Begunstiger = true,
            PreferredLanguage = Language.NL
        };

        // Act
        var result = await _service.CreateMember(dto, _userId, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        _permissionService.Received(1).EnsurePermission(_userId, Permission.ManageMembers, Arg.Any<uint?>());
        _db.ChangeTracker.Clear();
        var saved = await _db.Members.FindAsync(result.Id);
        Assert.NotNull(saved);
        Assert.True(saved.Begunstiger);
    }

    [Fact]
    public async Task CreateMember_NoStudyEnrollments_AsAnonymous_ThrowsArgumentException()
    {
        // Arrange
        var dto = new PostMemberDTO
        {
            StudentNumber = "123456",
            FirstName = "A",
            LastName = "B",
            Email = "a@b.com",
            PhoneNumber = "0612345678",
            Street = "S",
            HouseNumber = "1",
            PostalCode = "1",
            City = "C",
            DateOfBirth = DateTimeOffset.UtcNow.AddYears(-20),
            StudyEnrollments = new List<PostStudyEnrollmentDTO>(),
            PreferredLanguage = Language.NL
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateMember(dto, null, CancellationToken.None));
    }

    [Fact]
    public async Task CreateMember_NoStudyEnrollments_AsBoardMember_Succeeds()
    {
        // Arrange
        var dto = new PostMemberDTO
        {
            StudentNumber = "123456",
            FirstName = "A",
            LastName = "B",
            Email = "board-created@b.com",
            PhoneNumber = "0612345678",
            Street = "S",
            HouseNumber = "1",
            PostalCode = "1",
            City = "C",
            DateOfBirth = DateTimeOffset.UtcNow.AddYears(-20),
            StudyEnrollments = new List<PostStudyEnrollmentDTO>(),
            PreferredLanguage = Language.NL
        };

        _permissionService.IsBoardOrCandidateBoardMember(_userId).Returns(true);
        _permissionService.HasPermissionOrBoard(_userId, Permission.ManageMembers, Arg.Any<uint?>()).Returns(true);

        // Act
        var result = await _service.CreateMember(dto, _userId, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        _db.ChangeTracker.Clear();
        var saved = await _db.Members.FindAsync(result.Id);
        Assert.NotNull(saved);
        Assert.Equal("board-created@b.com", saved.Email);
    }

    [Fact]
    public async Task CreateMember_NullStudyEnrollments_AsBoardMember_Succeeds()
    {
        // Arrange
        var dto = new PostMemberDTO
        {
            StudentNumber = "123456",
            FirstName = "A",
            LastName = "B",
            Email = "board-created-null-studies@b.com",
            PhoneNumber = "0612345678",
            Street = "S",
            HouseNumber = "1",
            PostalCode = "1",
            City = "C",
            DateOfBirth = DateTimeOffset.UtcNow.AddYears(-20),
            StudyEnrollments = null,
            PreferredLanguage = Language.NL
        };

        _permissionService.IsBoardOrCandidateBoardMember(_userId).Returns(true);
        _permissionService.HasPermissionOrBoard(_userId, Permission.ManageMembers, Arg.Any<uint?>()).Returns(true);

        // Act
        var result = await _service.CreateMember(dto, _userId, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        _db.ChangeTracker.Clear();
        var saved = await _db.Members.FindAsync(result.Id);
        Assert.NotNull(saved);
        Assert.Equal("board-created-null-studies@b.com", saved.Email);
    }

    [Fact]
    public async Task DeleteMember_NotFound_ThrowsKeyNotFoundException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.DeleteMember(Guid.NewGuid(), _userId, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteMember_WithUnpaidActivities_ThrowsInvalidOperationException()
    {
        // Arrange
        var member = CreateTestMember(Guid.NewGuid());
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        _paymentValidationService.MemberHasPaidAllActivities(Arg.Any<Member>()).Returns(false);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.DeleteMember(member.Id, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteMember_ValidRequest_AnonymizesAndSoftDeletesMember()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var member = CreateTestMember(memberId);
        member.ProfilePicturePath = "pic.webp";
        var originalEmail = member.Email;
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        _paymentValidationService.MemberHasPaidAllActivities(Arg.Any<Member>()).Returns(true);

        // Act
        await _service.DeleteMember(memberId, _userId, CancellationToken.None);

        // Assert
        _db.ChangeTracker.Clear();
        var deletedQueried = await _db.Members.FindAsync(memberId);
        Assert.Null(deletedQueried);

        var anonymized = await _db.Members.IgnoreQueryFilters().FirstOrDefaultAsync(m => m.Id == memberId);
        Assert.NotNull(anonymized);
        Assert.True(anonymized.IsDeleted);
        Assert.Equal("Deleted", anonymized.FirstName);
        Assert.Equal("Member", anonymized.LastName);
        Assert.Equal($"deleted-{memberId}@deleted.local", anonymized.Email);
        Assert.Equal($"DELETED-{memberId}", anonymized.StudentNumber);
        Assert.Null(anonymized.ProfilePicturePath);

        _authOutboxWorker.Received(1).EnqueueTask(AuthTaskType.Delete, member.AuthSystemUserId!.Value, Arg.Any<PostgresDbContext>());
        await _storageService.Received(1).DeleteFileAsync("profile-pictures", "pic.webp");
        _memoryCache.Received(1).Remove("prof-pic-pic.webp");
        _mailSubscriptionOutboxWorker.Received(1).EnqueueDeleteTask(originalEmail, _db);
    }

    [Fact]
    public async Task DeleteMember_WithActiveFutureEnrollment_ThrowsInvalidOperationException()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var member = CreateTestMember(memberId);
        _db.Members.Add(member);

        var futureActivity = new Activity
        {
            Name = "Future Hackathon",
            Price = 10m,
            DutchDescription = "NL",
            EnglishDescription = "EN",
            DateTimeStart = DateTime.UtcNow.AddDays(1),
            DateTimeEnd = DateTime.UtcNow.AddDays(2),
            Location = "Enschede",
            IsOpenForPayment = true,
            PaymentDeadline = DateTimeOffset.UtcNow.AddDays(1)
        };
        _db.Activities.Add(futureActivity);
        await _db.SaveChangesAsync();

        _db.Enrollments.Add(new Enrollment
        {
            MemberId = memberId,
            ActivityId = futureActivity.Id,
            Price = 10m,
            RegisteredOn = DateTime.UtcNow,
            IsOnWaitingList = false
        });
        await _db.SaveChangesAsync();

        _paymentValidationService.MemberHasPaidAllActivities(Arg.Any<Member>()).Returns(true);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.DeleteMember(memberId, _userId, CancellationToken.None));

        Assert.Equal("Member has future enrollments and cannot be deleted.", exception.Message);

        _db.ChangeTracker.Clear();
        var memberStillExists = await _db.Members.FindAsync(memberId);
        Assert.NotNull(memberStillExists);
    }

    [Fact]
    public async Task DeleteMember_WithWaitingListFutureEnrollment_RemovesEnrollmentAndSucceeds()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var member = CreateTestMember(memberId);
        _db.Members.Add(member);

        var futureActivity = new Activity
        {
            Name = "Future Gala",
            Price = 15m,
            DutchDescription = "NL",
            EnglishDescription = "EN",
            DateTimeStart = DateTime.UtcNow.AddDays(3),
            DateTimeEnd = DateTime.UtcNow.AddDays(4),
            Location = "Enschede",
            IsOpenForPayment = true,
            PaymentDeadline = DateTimeOffset.UtcNow.AddDays(2)
        };
        _db.Activities.Add(futureActivity);
        await _db.SaveChangesAsync();

        var waitingListEnrollment = new Enrollment
        {
            MemberId = memberId,
            ActivityId = futureActivity.Id,
            Price = 15m,
            RegisteredOn = DateTime.UtcNow,
            IsOnWaitingList = true
        };
        _db.Enrollments.Add(waitingListEnrollment);
        await _db.SaveChangesAsync();

        _paymentValidationService.MemberHasPaidAllActivities(Arg.Any<Member>()).Returns(true);

        // Act
        await _service.DeleteMember(memberId, _userId, CancellationToken.None);

        // Assert
        _db.ChangeTracker.Clear();
        var remainingEnrollment = await _db.Enrollments
            .FirstOrDefaultAsync(e => e.MemberId == memberId && e.ActivityId == futureActivity.Id);

        Assert.Null(remainingEnrollment);

        var deletedMember = await _db.Members.IgnoreQueryFilters().FirstOrDefaultAsync(m => m.Id == memberId);
        Assert.NotNull(deletedMember);
        Assert.True(deletedMember.IsDeleted);
    }

    [Fact]
    public async Task DeleteMember_WithPastActivityEnrollment_RetainsEnrollmentForHistory()
    {
        // Arrange
        var memberId = Guid.NewGuid();
        var member = CreateTestMember(memberId);
        _db.Members.Add(member);

        var pastActivity = new Activity
        {
            Name = "Past Workshop",
            Price = 5m,
            DutchDescription = "NL",
            EnglishDescription = "EN",
            DateTimeStart = DateTime.UtcNow.AddDays(-5),
            DateTimeEnd = DateTime.UtcNow.AddDays(-4),
            Location = "Enschede",
            IsOpenForPayment = true,
            PaymentDeadline = DateTimeOffset.UtcNow.AddDays(-6)
        };
        _db.Activities.Add(pastActivity);
        await _db.SaveChangesAsync();

        var pastEnrollment = new Enrollment
        {
            MemberId = memberId,
            ActivityId = pastActivity.Id,
            Price = 5m,
            RegisteredOn = DateTime.UtcNow.AddDays(-10),
            IsOnWaitingList = false
        };
        _db.Enrollments.Add(pastEnrollment);
        await _db.SaveChangesAsync();

        _paymentValidationService.MemberHasPaidAllActivities(Arg.Any<Member>()).Returns(true);

        // Act
        await _service.DeleteMember(memberId, _userId, CancellationToken.None);

        // Assert - Past enrollment should stay in DB for historical/financial integrity
        _db.ChangeTracker.Clear();
        var preservedEnrollment = await _db.Enrollments
            .FirstOrDefaultAsync(e => e.MemberId == memberId && e.ActivityId == pastActivity.Id);

        Assert.NotNull(preservedEnrollment);
    }

    [Fact]
    public async Task PatchMember_RestrictedFieldBySelf_ThrowsUnauthorized()
    {
        // Arrange
        var member = CreateTestMember(_userId);
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        var patchDoc = new JsonPatchDocument<Member>(
            new List<Operation<Member>>
            {
                new Operation<Member>("replace", "/StudentNumber", null, "s9999999")
            },
            new DefaultContractResolver()
        );

        _permissionService.When(p => p.EnsurePermission(_userId, Permission.ManageMembers, Arg.Any<uint?>()))
            .Do(x => throw new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.PatchMember(_userId, patchDoc, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task PatchMember_MoveOperationFromDisallowedField_ThrowsUnauthorized()
    {
        // Arrange - the "from" path of a move/copy operation must also be checked against the allowed
        // list, otherwise a member could smuggle a disallowed field's value out via a "move" whose
        // destination path is allowed.
        var member = CreateTestMember(_userId);
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        var patchDoc = new JsonPatchDocument<Member>(
            new List<Operation<Member>>
            {
                new Operation<Member>("move", "/PhoneNumber", "/StudentNumber")
            },
            new DefaultContractResolver()
        );

        _permissionService.When(p => p.EnsurePermission(_userId, Permission.ManageMembers, Arg.Any<uint?>()))
            .Do(x => throw new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.PatchMember(_userId, patchDoc, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task PatchMember_AllowedFieldWithDifferentCasing_Self_Succeeds()
    {
        // Arrange - AllowedFields is compared case-insensitively.
        var member = CreateTestMember(_userId);
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        var patchDoc = new JsonPatchDocument<Member>(
            new List<Operation<Member>>
            {
                new Operation<Member>("replace", "/PHONENUMBER", null, "0699999999")
            },
            new DefaultContractResolver()
        );

        // Act
        await _service.PatchMember(_userId, patchDoc, _userId, CancellationToken.None);

        // Assert - no elevated check should have been required for the member's own allowed-field edit.
        _permissionService.DidNotReceive().EnsurePermission(_userId, Permission.ManageMembers, Arg.Any<uint?>());
        _db.ChangeTracker.Clear();
        var updated = await _db.Members.FindAsync(_userId);
        Assert.NotNull(updated);
        Assert.Equal("0699999999", updated.PhoneNumber);
    }

    [Fact]
    public async Task PatchMember_ValidRequest_UpdatesDb()
    {
        // Arrange
        var member = CreateTestMember(_userId);
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        var patchDoc = new JsonPatchDocument<Member>();
        patchDoc.Replace(m => m.Street, "New Street");

        // Act
        await _service.PatchMember(_userId, patchDoc, _userId, CancellationToken.None);

        // Assert
        _db.ChangeTracker.Clear();
        var updated = await _db.Members.FindAsync(_userId);
        Assert.NotNull(updated);
        Assert.Equal("New Street", updated.Street);
        _authOutboxWorker.Received(1).EnqueueTask(AuthTaskType.Sync, member.Id, Arg.Any<PostgresDbContext>());
        _mailSubscriptionOutboxWorker.DidNotReceiveWithAnyArgs().EnqueueUpdateSubscriptionsTask(default!, default!, default!);
    }

    [Fact]
    public async Task PatchMember_EmptyStudentNumber_ThrowsValidationException()
    {
        // Arrange
        var otherId = Guid.NewGuid();
        var member = CreateTestMember(otherId);
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        var patchDoc = new JsonPatchDocument<Member>(
            new List<Operation<Member>>
            {
                new Operation<Member>("replace", "/StudentNumber", null, "")
            },
            new DefaultContractResolver()
        );

        // Act & Assert
        await Assert.ThrowsAsync<System.ComponentModel.DataAnnotations.ValidationException>(() =>
            _service.PatchMember(otherId, patchDoc, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateMember_ValidRequest_UpdatesDb()
    {
        // Arrange
        var member = CreateTestMember(_userId);
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        var dto = new MemberUpdateDTO
        {
            StudentNumber = member.StudentNumber,
            FirstName = member.FirstName,
            LastName = member.LastName,
            Email = member.Email,
            PhoneNumber = "0698765432",
            Street = member.Street,
            HouseNumber = member.HouseNumber,
            PostalCode = member.PostalCode,
            City = member.City,
            DateOfBirth = member.DateOfBirth,
            PreferredLanguage = member.PreferredLanguage
        };

        // Act
        await _service.UpdateMember(_userId, dto, _userId, CancellationToken.None);

        // Assert
        _db.ChangeTracker.Clear();
        var updated = await _db.Members.FindAsync(_userId);
        Assert.NotNull(updated);
        Assert.Equal("0698765432", updated.PhoneNumber);
        _mailSubscriptionOutboxWorker.DidNotReceiveWithAnyArgs().EnqueueUpdateSubscriptionsTask(default!, default!, default!);
    }

    [Fact]
    public async Task UpdateMember_BoardMemberUpdatesSomeoneElse_AppliesUpdate()
    {
        // Arrange
        var member = CreateTestMember(Guid.NewGuid());
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        var boardUserId = Guid.NewGuid();
        _permissionService.IsBoardOrCandidateBoardMember(boardUserId).Returns(true);
        _permissionService.HasPermissionOrBoard(boardUserId, Permission.ManageMembers, Arg.Any<uint?>()).Returns(true);

        var dto = new MemberUpdateDTO
        {
            StudentNumber = member.StudentNumber,
            FirstName = "Updated By Board",
            LastName = member.LastName,
            Email = member.Email,
            PhoneNumber = member.PhoneNumber,
            Street = member.Street,
            HouseNumber = member.HouseNumber,
            PostalCode = member.PostalCode,
            City = member.City,
            DateOfBirth = member.DateOfBirth,
            PreferredLanguage = member.PreferredLanguage
        };

        // Act
        await _service.UpdateMember(member.Id, dto, boardUserId, CancellationToken.None);

        // Assert
        _permissionService.Received(1).EnsurePermission(boardUserId, Permission.ManageMembers, Arg.Any<uint?>());
        _db.ChangeTracker.Clear();
        var updated = await _db.Members.FindAsync(member.Id);
        Assert.NotNull(updated);
        Assert.Equal("Updated By Board", updated.FirstName);
    }

    [Fact]
    public async Task UpdateMember_NonBoardChangesRestrictedField_SilentlyIgnoresChange()
    {
        // Arrange
        var member = CreateTestMember(_userId);
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        _permissionService.IsBoardOrCandidateBoardMember(_userId).Returns(false);

        var dto = new MemberUpdateDTO
        {
            StudentNumber = member.StudentNumber,
            // A member isn't allowed to change their own name - only board members can. Rejecting
            // the request based on this guess would let them learn the real value from whether the
            // request succeeds, so it must be silently ignored instead of rejected.
            FirstName = "NewName",
            LastName = member.LastName,
            Email = member.Email,
            PhoneNumber = "0698765432",
            Street = member.Street,
            HouseNumber = member.HouseNumber,
            PostalCode = member.PostalCode,
            City = member.City,
            DateOfBirth = member.DateOfBirth,
            PreferredLanguage = member.PreferredLanguage
        };

        // Act
        await _service.UpdateMember(_userId, dto, _userId, CancellationToken.None);

        // Assert
        _db.ChangeTracker.Clear();
        var updated = await _db.Members.FindAsync(_userId);
        Assert.NotNull(updated);
        Assert.Equal("Test", updated.FirstName);
        Assert.Equal("0698765432", updated.PhoneNumber);
    }

    [Fact]
    public async Task UpdateMember_EmptyStudentNumber_ThrowsValidationException()
    {
        // Arrange
        var member = CreateTestMember(_userId);
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        // Only board members can change StudentNumber - use a board member here so the invalid
        // value actually reaches validation, instead of being silently ignored.
        _permissionService.IsBoardOrCandidateBoardMember(_userId).Returns(true);
        _permissionService.HasPermissionOrBoard(_userId, Permission.ManageMembers, Arg.Any<uint?>()).Returns(true);

        var dto = new MemberUpdateDTO
        {
            StudentNumber = "",
            FirstName = member.FirstName,
            LastName = member.LastName,
            Email = member.Email,
            PhoneNumber = member.PhoneNumber,
            Street = member.Street,
            HouseNumber = member.HouseNumber,
            PostalCode = member.PostalCode,
            City = member.City,
            DateOfBirth = member.DateOfBirth,
            PreferredLanguage = member.PreferredLanguage
        };

        // Act & Assert
        await Assert.ThrowsAsync<System.ComponentModel.DataAnnotations.ValidationException>(() =>
            _service.UpdateMember(_userId, dto, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteProfilePicture_ValidRequest_ClearsPicture()
    {
        // Arrange
        var member = CreateTestMember(_userId);
        member.ProfilePicturePath = "pic.webp";
        member.ProfilePictureFileName = "pic.png";
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        // Act
        await _service.DeleteProfilePicture(_userId, _userId, CancellationToken.None);

        // Assert
        _db.ChangeTracker.Clear();
        var updated = await _db.Members.FindAsync(_userId);
        Assert.NotNull(updated);
        Assert.Null(updated.ProfilePicturePath);
        Assert.Null(updated.ProfilePictureFileName);
        await _storageService.Received(1).DeleteFileAsync("profile-pictures", "pic.webp");
        _memoryCache.Received(1).Remove("prof-pic-pic.webp");
    }

    [Fact]
    public async Task RefreshEmail_ValidRequest_SyncsWithAuthSystem()
    {
        // Arrange
        var authUserId = Guid.NewGuid();
        var member = CreateTestMember(Guid.NewGuid());
        member.AuthSystemUserId = authUserId;
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        _authService.GetEmail(authUserId).Returns(Task.FromResult("new-email@example.com"));

        // Act
        await _service.RefreshEmail(authUserId, CancellationToken.None);

        // Assert
        _authOutboxWorker.Received(1).EnqueueTask(AuthTaskType.RefreshEmail, member.Id, Arg.Any<PostgresDbContext>());
        _mailSubscriptionOutboxWorker.Received(1).EnqueueMigrateEmailTask(member.Email, "new-email@example.com", _db);
    }

    [Fact]
    public async Task CreateMember_BegunstigerDuplicateEmail_PaidBegunstigerFee_ThrowsInvalidOperationException()
    {
        // Arrange
        var otherId = Guid.NewGuid();
        var existing = CreateTestMember(otherId, "dup@example.com");
        existing.Begunstiger = true;
        _db.Members.Add(existing);

        var payment = new BegunstigerPayment
        {
            MemberId = existing.Id,
            PaymentServiceId = "pay_begunstiger_dup",
            PaymentIntentUrl = "http://intent-begunstiger",
            Price = 15m
        };
        _db.BegunstigerPayments.Add(payment);
        await _db.SaveChangesAsync();

        var study = new Study { Id = 1, Title = "S", NominalDurationYears = 3, Type = StudyType.Bachelor };
        _db.Studies.Add(study);
        await _db.SaveChangesAsync();

        _paymentService.GetPaymentAsync("pay_begunstiger_dup")
            .Returns(Task.FromResult(new GetPaymentResponse("pay_begunstiger_dup", PaymentStatus.Paid, null)));

        var dto = new PostMemberDTO
        {
            StudentNumber = "999",
            FirstName = "A",
            LastName = "B",
            Email = "dup@example.com",
            PhoneNumber = "0612345678",
            Street = "S",
            HouseNumber = "1",
            PostalCode = "1",
            City = "C",
            DateOfBirth = DateTimeOffset.UtcNow.AddYears(-20),
            PreferredLanguage = Language.NL,
            StudyEnrollments = new List<PostStudyEnrollmentDTO>
            {
                new PostStudyEnrollmentDTO { StudyId = 1, MemberId = Guid.Empty, EnrollmentDate = new DateTimeOffset(new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc)), Status = StudyStatus.Enrolled }
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateMember(dto, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task CreateMember_BegunstigerDuplicateEmail_PendingBegunstigerFee_CancelsPaymentAndRemovesExisting()
    {
        // Arrange
        var otherId = Guid.NewGuid();
        var existing = CreateTestMember(otherId, "dup-pending-begunstiger@example.com");
        existing.Begunstiger = true;
        _db.Members.Add(existing);

        var payment = new BegunstigerPayment
        {
            MemberId = existing.Id,
            PaymentServiceId = "pay_begunstiger_pending",
            PaymentIntentUrl = "http://intent-begunstiger-pending",
            Price = 15m
        };
        _db.BegunstigerPayments.Add(payment);
        await _db.SaveChangesAsync();

        var study = new Study { Id = 17, Title = "S", NominalDurationYears = 3, Type = StudyType.Bachelor };
        _db.Studies.Add(study);
        await _db.SaveChangesAsync();

        _paymentService.GetPaymentAsync("pay_begunstiger_pending")
            .Returns(Task.FromResult(new GetPaymentResponse("pay_begunstiger_pending", PaymentStatus.Pending, null)));

        var dto = new PostMemberDTO
        {
            StudentNumber = "999",
            FirstName = "A",
            LastName = "B",
            Email = "dup-pending-begunstiger@example.com",
            PhoneNumber = "0612345678",
            Street = "S",
            HouseNumber = "1",
            PostalCode = "1",
            City = "C",
            DateOfBirth = DateTimeOffset.UtcNow.AddYears(-20),
            PreferredLanguage = Language.NL,
            StudyEnrollments = new List<PostStudyEnrollmentDTO>
            {
                new PostStudyEnrollmentDTO { StudyId = 17, MemberId = Guid.Empty, EnrollmentDate = new DateTimeOffset(new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc)), Status = StudyStatus.Enrolled }
            }
        };

        // Act
        var result = await _service.CreateMember(dto, _userId, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        await _paymentService.Received(1).CancelPaymentAsync("pay_begunstiger_pending");

        _db.ChangeTracker.Clear();
        var oldMemberDeleted = await _db.Members.FindAsync(existing.Id);
        Assert.Null(oldMemberDeleted);
    }

    [Fact]
    public async Task RefreshEmail_MemberNotFound_ThrowsKeyNotFoundException()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.RefreshEmail(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task PatchMember_MemberNotFound_ThrowsKeyNotFoundException()
    {
        var patchDoc = new JsonPatchDocument<Member>();
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.PatchMember(Guid.NewGuid(), patchDoc, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateMember_MemberNotFound_ThrowsKeyNotFoundException()
    {
        var dto = new MemberUpdateDTO
        {
            StudentNumber = "123456",
            FirstName = "New",
            LastName = "Name",
            Email = "new@example.com",
            PhoneNumber = "0612345678",
            Street = "S",
            HouseNumber = "1",
            PostalCode = "1",
            City = "C",
            DateOfBirth = DateTimeOffset.UtcNow.AddYears(-20),
            PreferredLanguage = Language.NL
        };
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.UpdateMember(Guid.NewGuid(), dto, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteProfilePicture_MemberNotFound_ReturnsSilently()
    {
        // Act (should not throw anything)
        await _service.DeleteProfilePicture(Guid.NewGuid(), _userId, CancellationToken.None);
    }

    [Fact]
    public async Task DeleteProfilePicture_NoPicture_ThrowsKeyNotFoundException()
    {
        var member = CreateTestMember(_userId);
        member.ProfilePicturePath = null;
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.DeleteProfilePicture(_userId, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task CreateMember_DuplicateEmail_MasterStudyAndNoPaySetting_ThrowsInvalidOperationException()
    {
        // Arrange
        var study = new Study { Id = 10, Title = "Master Study", NominalDurationYears = 2, Type = StudyType.Master };
        _db.Studies.Add(study);

        var existing = CreateTestMember(Guid.NewGuid(), "dup@example.com");
        _db.Members.Add(existing);
        await _db.SaveChangesAsync();

        var se = new StudyEnrollment { MemberId = existing.Id, StudyId = study.Id, EnrollmentDate = new DateTimeOffset(new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc)), Status = StudyStatus.Enrolled };
        _db.StudyEnrollments.Add(se);
        await _db.SaveChangesAsync();

        var dto = new PostMemberDTO
        {
            StudentNumber = "123456",
            FirstName = "A",
            LastName = "B",
            Email = "dup@example.com",
            PhoneNumber = "0612345678",
            Street = "S",
            HouseNumber = "1",
            PostalCode = "1",
            City = "C",
            DateOfBirth = DateTimeOffset.UtcNow.AddYears(-20),
            PreferredLanguage = Language.NL,
            StudyEnrollments = new List<PostStudyEnrollmentDTO>
            {
                new PostStudyEnrollmentDTO { StudyId = 10, MemberId = Guid.Empty, EnrollmentDate = new DateTimeOffset(new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc)), Status = StudyStatus.Enrolled }
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateMember(dto, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task CreateMember_DuplicateEmail_GratieAndNoPaySetting_ThrowsInvalidOperationException()
    {
        // Arrange
        var existing = CreateTestMember(Guid.NewGuid(), "dup2@example.com");
        existing.Gratie = true;
        _db.Members.Add(existing);
        await _db.SaveChangesAsync();

        var study = new Study { Id = 11, Title = "S", NominalDurationYears = 3, Type = StudyType.Bachelor };
        _db.Studies.Add(study);
        await _db.SaveChangesAsync();

        var dto = new PostMemberDTO
        {
            StudentNumber = "123456",
            FirstName = "A",
            LastName = "B",
            Email = "dup2@example.com",
            PhoneNumber = "0612345678",
            Street = "S",
            HouseNumber = "1",
            PostalCode = "1",
            City = "C",
            DateOfBirth = DateTimeOffset.UtcNow.AddYears(-20),
            PreferredLanguage = Language.NL,
            StudyEnrollments = new List<PostStudyEnrollmentDTO>
            {
                new PostStudyEnrollmentDTO { StudyId = 11, MemberId = Guid.Empty, EnrollmentDate = new DateTimeOffset(new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc)), Status = StudyStatus.Enrolled }
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateMember(dto, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task CreateMember_DuplicateEmail_ErelidAndNoPaySetting_ThrowsInvalidOperationException()
    {
        // Arrange
        var existing = CreateTestMember(Guid.NewGuid(), "dup3@example.com");
        existing.EreLid = true;
        _db.Members.Add(existing);
        await _db.SaveChangesAsync();

        var study = new Study { Id = 12, Title = "S", NominalDurationYears = 3, Type = StudyType.Bachelor };
        _db.Studies.Add(study);
        await _db.SaveChangesAsync();

        var dto = new PostMemberDTO
        {
            StudentNumber = "123456",
            FirstName = "A",
            LastName = "B",
            Email = "dup3@example.com",
            PhoneNumber = "0612345678",
            Street = "S",
            HouseNumber = "1",
            PostalCode = "1",
            City = "C",
            DateOfBirth = DateTimeOffset.UtcNow.AddYears(-20),
            PreferredLanguage = Language.NL,
            StudyEnrollments = new List<PostStudyEnrollmentDTO>
            {
                new PostStudyEnrollmentDTO { StudyId = 12, MemberId = Guid.Empty, EnrollmentDate = new DateTimeOffset(new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc)), Status = StudyStatus.Enrolled }
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateMember(dto, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task CreateMember_DuplicateEmail_LidVanVerdiensteAndNoPaySetting_ThrowsInvalidOperationException()
    {
        // Arrange
        var existing = CreateTestMember(Guid.NewGuid(), "dup4@example.com");
        existing.LidVanVerdienste = true;
        _db.Members.Add(existing);
        await _db.SaveChangesAsync();

        var study = new Study { Id = 13, Title = "S", NominalDurationYears = 3, Type = StudyType.Bachelor };
        _db.Studies.Add(study);
        await _db.SaveChangesAsync();

        var dto = new PostMemberDTO
        {
            StudentNumber = "123456",
            FirstName = "A",
            LastName = "B",
            Email = "dup4@example.com",
            PhoneNumber = "0612345678",
            Street = "S",
            HouseNumber = "1",
            PostalCode = "1",
            City = "C",
            DateOfBirth = DateTimeOffset.UtcNow.AddYears(-20),
            PreferredLanguage = Language.NL,
            StudyEnrollments = new List<PostStudyEnrollmentDTO>
            {
                new PostStudyEnrollmentDTO { StudyId = 13, MemberId = Guid.Empty, EnrollmentDate = new DateTimeOffset(new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc)), Status = StudyStatus.Enrolled }
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateMember(dto, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task CreateMember_DuplicateEmail_PaidPayment_ThrowsInvalidOperationException()
    {
        // Arrange
        var existing = CreateTestMember(Guid.NewGuid(), "dup5@example.com");
        _db.Members.Add(existing);

        var payment = new MembershipPayment
        {
            MemberId = existing.Id,
            PaymentServiceId = "pay_123",
            PaymentIntentUrl = "http://intent1",
            Price = 15m
        };
        _db.MembershipPayments.Add(payment);
        await _db.SaveChangesAsync();

        var study = new Study { Id = 14, Title = "S", NominalDurationYears = 3, Type = StudyType.Bachelor };
        _db.Studies.Add(study);
        await _db.SaveChangesAsync();

        var paymentResponse = new GetPaymentResponse("pay_123", PaymentStatus.Paid, null);
        _paymentService.GetPaymentAsync("pay_123").Returns(Task.FromResult(paymentResponse));

        var dto = new PostMemberDTO
        {
            StudentNumber = "123456",
            FirstName = "A",
            LastName = "B",
            Email = "dup5@example.com",
            PhoneNumber = "0612345678",
            Street = "S",
            HouseNumber = "1",
            PostalCode = "1",
            City = "C",
            DateOfBirth = DateTimeOffset.UtcNow.AddYears(-20),
            PreferredLanguage = Language.NL,
            StudyEnrollments = new List<PostStudyEnrollmentDTO>
            {
                new PostStudyEnrollmentDTO { StudyId = 14, MemberId = Guid.Empty, EnrollmentDate = new DateTimeOffset(new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc)), Status = StudyStatus.Enrolled }
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateMember(dto, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task CreateMember_DuplicateEmail_ExistingMemberHasActivityEnrollment_ThrowsInvalidOperationException()
    {
        // Arrange
        var existing = CreateTestMember(Guid.NewGuid(), "dup7@example.com");
        _db.Members.Add(existing);

        var activity = new Activity
        {
            Name = "Act",
            Price = 15m,
            DutchDescription = "NL",
            EnglishDescription = "EN",
            DateTimeStart = DateTime.UtcNow.AddDays(1),
            DateTimeEnd = DateTime.UtcNow.AddDays(2),
            Location = "Enschede",
            IsOpenForPayment = true,
            PaymentDeadline = DateTimeOffset.UtcNow.AddDays(5)
        };
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        _db.Enrollments.Add(new Enrollment { MemberId = existing.Id, ActivityId = activity.Id, Price = 15m, RegisteredOn = DateTime.UtcNow, IsOnWaitingList = false });
        await _db.SaveChangesAsync();

        var study = new Study { Id = 16, Title = "S", NominalDurationYears = 3, Type = StudyType.Bachelor };
        _db.Studies.Add(study);
        await _db.SaveChangesAsync();

        var dto = new PostMemberDTO
        {
            StudentNumber = "123456",
            FirstName = "A",
            LastName = "B",
            Email = "dup7@example.com",
            PhoneNumber = "0612345678",
            Street = "S",
            HouseNumber = "1",
            PostalCode = "1",
            City = "C",
            DateOfBirth = DateTimeOffset.UtcNow.AddYears(-20),
            PreferredLanguage = Language.NL,
            StudyEnrollments = new List<PostStudyEnrollmentDTO>
            {
                new PostStudyEnrollmentDTO { StudyId = 16, MemberId = Guid.Empty, EnrollmentDate = new DateTimeOffset(new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc)), Status = StudyStatus.Enrolled }
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateMember(dto, _userId, CancellationToken.None));

        _db.ChangeTracker.Clear();
        var stillExists = await _db.Members.FindAsync(existing.Id);
        Assert.NotNull(stillExists);
    }

    [Fact]
    public async Task CreateMember_DuplicateEmail_PendingPayment_CancelsPaymentAndRemovesExisting()
    {
        // Arrange
        var existing = CreateTestMember(Guid.NewGuid(), "dup6@example.com");
        _db.Members.Add(existing);

        var payment = new MembershipPayment
        {
            MemberId = existing.Id,
            PaymentServiceId = "pay_456",
            PaymentIntentUrl = "http://intent2",
            Price = 15m
        };
        _db.MembershipPayments.Add(payment);
        await _db.SaveChangesAsync();

        var study = new Study { Id = 15, Title = "S", NominalDurationYears = 3, Type = StudyType.Bachelor };
        _db.Studies.Add(study);
        await _db.SaveChangesAsync();

        var paymentResponse = new GetPaymentResponse("pay_456", PaymentStatus.Pending, null);
        _paymentService.GetPaymentAsync("pay_456").Returns(Task.FromResult(paymentResponse));

        var dto = new PostMemberDTO
        {
            StudentNumber = "123456",
            FirstName = "A",
            LastName = "B",
            Email = "dup6@example.com",
            PhoneNumber = "0612345678",
            Street = "S",
            HouseNumber = "1",
            PostalCode = "1",
            City = "C",
            DateOfBirth = DateTimeOffset.UtcNow.AddYears(-20),
            PreferredLanguage = Language.NL,
            StudyEnrollments = new List<PostStudyEnrollmentDTO>
            {
                new PostStudyEnrollmentDTO { StudyId = 15, MemberId = Guid.Empty, EnrollmentDate = new DateTimeOffset(new DateTime(2025, 9, 1, 0, 0, 0, DateTimeKind.Utc)), Status = StudyStatus.Enrolled }
            }
        };

        // Act
        var result = await _service.CreateMember(dto, _userId, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        await _paymentService.Received(1).CancelPaymentAsync("pay_456");

        _db.ChangeTracker.Clear();
        var oldMemberDeleted = await _db.Members.FindAsync(existing.Id);
        Assert.Null(oldMemberDeleted);
    }

    [Fact]
    public async Task GetMemberMailinglists_Self_ReturnsOnlyVisibleProviderLists()
    {
        // Arrange
        var member = CreateTestMember(_userId, "self@example.com");
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        var lists = new List<MemberMailinglistDto>
        {
            new("id_news", "Newsletter", true),
            new("id_alumni", "Alumni", false)
        };
        _mailSubscriptionService.GetMemberMailinglistsAsync("self@example.com", Arg.Any<CancellationToken>()).Returns(lists);
        _mailinglistCurationService.GetVisibleProviderListIds(false, Arg.Any<CancellationToken>())
            .Returns(new HashSet<string> { "id_news" });

        // Act
        var result = await _service.GetMemberMailinglists(_userId, false, _userId, CancellationToken.None);

        // Assert
        var single = Assert.Single(result);
        Assert.Equal("id_news", single.Id);
        _permissionService.DidNotReceive().EnsurePermission(_userId, Permission.ManageMembers, Arg.Any<uint?>());
    }

    [Fact]
    public async Task GetMemberMailinglists_Other_RequiresBoard()
    {
        // Arrange
        var otherId = Guid.NewGuid();
        var member = CreateTestMember(otherId, "other@example.com");
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        _permissionService.When(p => p.EnsurePermission(_userId, Permission.ManageMembers, Arg.Any<uint?>()))
            .Do(x => throw new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.GetMemberMailinglists(otherId, false, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task GetMemberMailinglists_MemberNotFound_ThrowsKeyNotFoundException()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.GetMemberMailinglists(Guid.NewGuid(), false, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateMemberMailinglists_Self_EnqueuesUpdateTask()
    {
        // Arrange
        var member = CreateTestMember(_userId, "self@example.com");
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        var ids = new List<string> { "id_news", "id_events" };

        // Act
        await _service.UpdateMemberMailinglists(_userId, ids, false, _userId, CancellationToken.None);

        // Assert
        _permissionService.DidNotReceive().EnsureBoardOrCandidateBoardMember(_userId);
        _mailSubscriptionOutboxWorker.Received(1).EnqueueUpdateSubscriptionsTask(
            "self@example.com",
            Arg.Is<IEnumerable<string>>(actual => actual.OrderBy(x => x).SequenceEqual(ids.OrderBy(x => x))),
            _db);
    }

    [Fact]
    public async Task UpdateMemberMailinglists_MemberNotFound_ThrowsKeyNotFoundException()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.UpdateMemberMailinglists(Guid.NewGuid(), new List<string>(), false, _userId, CancellationToken.None));
    }

    /// <summary>
    /// The critical correctness case for this feature: saving from the General context
    /// (includeYearlyRenewal=false) must not touch subscription state for a list outside that
    /// context - e.g. a YearlyRenewalOnly "alumni" list the member is already subscribed to - since
    /// UpdateMemberSubscriptionsAsync at the provider does a full replace of everything not
    /// explicitly included.
    /// </summary>
    [Fact]
    public async Task UpdateMemberMailinglists_GeneralContext_PreservesSubscriptionsOutsideContext()
    {
        // Arrange
        var member = CreateTestMember(_userId, "self@example.com");
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        // Member is currently subscribed to a General list (being edited) and a YearlyRenewalOnly
        // list (not shown/editable in this context) plus an uncurated provider list.
        var currentState = new List<MemberMailinglistDto>
        {
            new("id_news", "Newsletter", true),
            new("id_alumni", "Alumni", true),
            new("id_uncurated", "Uncurated", true)
        };
        _mailSubscriptionService.GetMemberMailinglistsAsync("self@example.com", Arg.Any<CancellationToken>()).Returns(currentState);

        // Only "id_news" is visible in the General context.
        _mailinglistCurationService.GetVisibleProviderListIds(false, Arg.Any<CancellationToken>())
            .Returns(new HashSet<string> { "id_news" });

        // The member unsubscribes from the one General list they can see.
        var submittedIds = new List<string>();

        // Act
        await _service.UpdateMemberMailinglists(_userId, submittedIds, false, _userId, CancellationToken.None);

        // Assert - the final set sent to the provider must still include the two lists outside the
        // General context ("id_alumni", "id_uncurated"), even though the submitted set was empty.
        _mailSubscriptionOutboxWorker.Received(1).EnqueueUpdateSubscriptionsTask(
            "self@example.com",
            Arg.Is<IEnumerable<string>>(actual =>
                actual.OrderBy(x => x).SequenceEqual(new[] { "id_alumni", "id_uncurated" }.OrderBy(x => x))),
            _db);
    }

    [Fact]
    public async Task UpdateMemberMailinglists_YearlyRenewalContext_SubmittedSetCoversEverythingCurated()
    {
        // Arrange
        var member = CreateTestMember(_userId, "self@example.com");
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        var currentState = new List<MemberMailinglistDto>
        {
            new("id_news", "Newsletter", true),
            new("id_alumni", "Alumni", false),
            new("id_uncurated", "Uncurated", true)
        };
        _mailSubscriptionService.GetMemberMailinglistsAsync("self@example.com", Arg.Any<CancellationToken>()).Returns(currentState);

        // Both General and YearlyRenewalOnly lists are visible in this context.
        _mailinglistCurationService.GetVisibleProviderListIds(true, Arg.Any<CancellationToken>())
            .Returns(new HashSet<string> { "id_news", "id_alumni" });

        // Member subscribes to alumni too, in the yearly renewal form.
        var submittedIds = new List<string> { "id_news", "id_alumni" };

        // Act
        await _service.UpdateMemberMailinglists(_userId, submittedIds, true, _userId, CancellationToken.None);

        // Assert - the uncurated provider list the member happens to already be on is still
        // preserved, since it's outside even the full curated context.
        _mailSubscriptionOutboxWorker.Received(1).EnqueueUpdateSubscriptionsTask(
            "self@example.com",
            Arg.Is<IEnumerable<string>>(actual =>
                actual.OrderBy(x => x).SequenceEqual(new[] { "id_news", "id_alumni", "id_uncurated" }.OrderBy(x => x))),
            _db);
    }

    [Fact]
    public async Task SendActivationEmail_MemberLinked_QueuesEmailAndMarksSent()
    {
        // Arrange
        var member = CreateTestMember(Guid.NewGuid());
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        _paymentService.TryQueueActivationEmailAsync(member.Id).Returns(true);

        // Act
        var status = await _service.SendActivationEmail(member.Id, CancellationToken.None);

        // Assert
        Assert.Equal(ActivationEmailStatus.Sent, status);
        await _paymentService.Received(1).TryQueueActivationEmailAsync(member.Id);
    }

    [Fact]
    public async Task SendActivationEmail_LostRaceToQueueEmail_ReturnsAlreadySent()
    {
        // Arrange - the webhook or PaymentSyncService claimed it for this member first (see
        // AbstractPaymentService.TryQueueActivationEmailAsync's atomic check-and-set).
        var member = CreateTestMember(Guid.NewGuid());
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        _paymentService.TryQueueActivationEmailAsync(member.Id).Returns(false);

        // Act
        var status = await _service.SendActivationEmail(member.Id, CancellationToken.None);

        // Assert
        Assert.Equal(ActivationEmailStatus.AlreadySent, status);
    }

    [Fact]
    public async Task SendActivationEmail_AlreadySent_DoesNotQueueAgain()
    {
        // Arrange
        var member = CreateTestMember(Guid.NewGuid());
        member.ActivationEmailSentAt = DateTimeOffset.UtcNow.AddMinutes(-5);
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        // Act
        var status = await _service.SendActivationEmail(member.Id, CancellationToken.None);

        // Assert
        Assert.Equal(ActivationEmailStatus.AlreadySent, status);
        _authOutboxWorker.DidNotReceiveWithAnyArgs().EnqueueTask(default, default, default!);
    }

    [Fact]
    public async Task SendActivationEmail_NotLinkedToAuthSystem_ReturnsPending()
    {
        // Arrange
        var member = CreateTestMember(Guid.NewGuid());
        member.AuthSystemUserId = null;
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        // Act
        var status = await _service.SendActivationEmail(member.Id, CancellationToken.None);

        // Assert
        Assert.Equal(ActivationEmailStatus.Pending, status);
        _authOutboxWorker.DidNotReceiveWithAnyArgs().EnqueueTask(default, default, default!);

        var updated = await _db.Members.FindAsync(member.Id);
        Assert.Null(updated!.ActivationEmailSentAt);
    }

    [Fact]
    public async Task SendActivationEmail_MemberNotFound_ThrowsKeyNotFound()
    {
        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.SendActivationEmail(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task SendActivationEmail_MembershipPaymentCancelledAtMollie_ReturnsPaymentRequired()
    {
        // Arrange - simulates a member redirected back from Mollie after cancelling: Mollie's
        // redirect fires regardless of outcome, so a MembershipPayment row exists but was never paid,
        // and Mollie's own live status for it is definitively Failed (cancelled/expired/failed).
        var member = CreateTestMember(Guid.NewGuid());
        _db.Members.Add(member);
        _db.MembershipPayments.Add(new MembershipPayment
        {
            MemberId = member.Id,
            PaymentServiceId = "pay_cancelled",
            PaymentIntentUrl = "http://intent",
            Price = 7.50m
        });
        await _db.SaveChangesAsync();

        _paymentService.GetPaymentAsync("pay_cancelled")
            .Returns(Task.FromResult(new GetPaymentResponse("pay_cancelled", PaymentStatus.Failed, null)));

        // Act
        var status = await _service.SendActivationEmail(member.Id, CancellationToken.None);

        // Assert
        Assert.Equal(ActivationEmailStatus.PaymentRequired, status);
        _authOutboxWorker.DidNotReceiveWithAnyArgs().EnqueueTask(default, default, default!);

        var updated = await _db.Members.FindAsync(member.Id);
        Assert.Null(updated!.ActivationEmailSentAt);
    }

    [Fact]
    public async Task SendActivationEmail_MembershipPaymentStillPendingAtMollie_ReturnsPendingForRetry()
    {
        // Arrange - the payment hasn't resolved yet at Mollie (e.g. a bank transfer still settling).
        // This must not be treated the same as a failed payment: the caller should retry rather than
        // tell the member to register again.
        var member = CreateTestMember(Guid.NewGuid());
        _db.Members.Add(member);
        _db.MembershipPayments.Add(new MembershipPayment
        {
            MemberId = member.Id,
            PaymentServiceId = "pay_pending",
            PaymentIntentUrl = "http://intent",
            Price = 7.50m
        });
        await _db.SaveChangesAsync();

        _paymentService.GetPaymentAsync("pay_pending")
            .Returns(Task.FromResult(new GetPaymentResponse("pay_pending", PaymentStatus.Pending, null)));

        // Act
        var status = await _service.SendActivationEmail(member.Id, CancellationToken.None);

        // Assert
        Assert.Equal(ActivationEmailStatus.Pending, status);
        _authOutboxWorker.DidNotReceiveWithAnyArgs().EnqueueTask(default, default, default!);
    }

    [Fact]
    public async Task SendActivationEmail_MembershipPaymentPaidAtMollieButNotLocallyYet_QueuesEmailWithoutWaitingForSync()
    {
        // Arrange - the local record isn't marked paid yet (the webhook hasn't landed, or previously
        // failed to process, as happened with the Mollie status-spelling bug), but Mollie's live status
        // is already Paid. Activation should proceed immediately rather than making the member wait for
        // the webhook retry or the next PaymentSyncService pass to mark PaidAt locally - those still
        // own catching up PaidAt/Sync/Accounting, this only needs to know it's safe to send the email.
        var member = CreateTestMember(Guid.NewGuid());
        _db.Members.Add(member);
        _db.MembershipPayments.Add(new MembershipPayment
        {
            MemberId = member.Id,
            PaymentServiceId = "pay_actually_paid",
            PaymentIntentUrl = "http://intent",
            Price = 7.50m
        });
        await _db.SaveChangesAsync();

        _paymentService.GetPaymentAsync("pay_actually_paid")
            .Returns(Task.FromResult(new GetPaymentResponse("pay_actually_paid", PaymentStatus.Paid, DateTimeOffset.UtcNow)));
        _paymentService.TryQueueActivationEmailAsync(member.Id).Returns(true);

        // Act
        var status = await _service.SendActivationEmail(member.Id, CancellationToken.None);

        // Assert
        Assert.Equal(ActivationEmailStatus.Sent, status);
        await _paymentService.DidNotReceiveWithAnyArgs().HandleWebhookAsync(default!);
        await _paymentService.Received(1).TryQueueActivationEmailAsync(member.Id);
    }

    [Fact]
    public async Task SendActivationEmail_MembershipPaymentPaid_QueuesEmail()
    {
        // Arrange
        var member = CreateTestMember(Guid.NewGuid());
        _db.Members.Add(member);
        _db.MembershipPayments.Add(new MembershipPayment
        {
            MemberId = member.Id,
            PaymentServiceId = "pay_paid",
            PaymentIntentUrl = "http://intent",
            Price = 7.50m,
            PaidAt = DateTimeOffset.UtcNow
        });
        await _db.SaveChangesAsync();

        _paymentService.TryQueueActivationEmailAsync(member.Id).Returns(true);

        // Act
        var status = await _service.SendActivationEmail(member.Id, CancellationToken.None);

        // Assert
        Assert.Equal(ActivationEmailStatus.Sent, status);
        await _paymentService.Received(1).TryQueueActivationEmailAsync(member.Id);
    }
}
