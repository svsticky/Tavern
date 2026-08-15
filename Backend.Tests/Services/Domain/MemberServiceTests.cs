using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Backend.Controllers.DTOs;
using Backend.Database;
using Backend.Interfaces;
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
        _memoryCache = Substitute.For<IMemoryCache>();

        _service = new MemberService(
            _db,
            _permissionService,
            _paymentValidationService,
            _storageService,
            _paymentService,
            _authOutboxWorker,
            _mailSubscriptionOutboxWorker,
            _authService,
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
        _permissionService.Received(1).EnsureBoardOrCandidateBoardMember(_userId);
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

        _permissionService.When(p => p.EnsureBoardOrCandidateBoardMember(_userId))
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
            PreferredLanguage = Language.NL
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateMember(dto, _userId, CancellationToken.None));
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
            PreferredLanguage = Language.NL
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateMember(dto, _userId, CancellationToken.None));
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
        await _authOutboxWorker.Received(1).EnqueueTask(AuthTaskType.Create, result.Id);
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

        await _authOutboxWorker.Received(1).EnqueueTask(AuthTaskType.Delete, member.AuthSystemUserId!.Value);
        await _storageService.Received(1).DeleteFileAsync("profile-pictures", "pic.webp");
        _memoryCache.Received(1).Remove("prof-pic-pic.webp");
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

        _permissionService.When(p => p.EnsureBoardOrCandidateBoardMember(_userId))
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

        _permissionService.When(p => p.EnsureBoardOrCandidateBoardMember(_userId))
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

        // Assert - no board check should have been required for the member's own allowed-field edit.
        _permissionService.DidNotReceive().EnsureBoardOrCandidateBoardMember(_userId);
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
        await _authOutboxWorker.Received(1).EnqueueTask(AuthTaskType.Sync, member.AuthSystemUserId!.Value);
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
            FirstName = "NewName",
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
        await _service.UpdateMember(_userId, dto, _userId, CancellationToken.None);

        // Assert
        _db.ChangeTracker.Clear();
        var updated = await _db.Members.FindAsync(_userId);
        Assert.NotNull(updated);
        Assert.Equal("NewName", updated.FirstName);
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
        await _authOutboxWorker.Received(1).EnqueueTask(AuthTaskType.RefreshEmail, authUserId);
        _mailSubscriptionOutboxWorker.Received(1).EnqueueTask("new-email@example.com", member.MailSubscriptions, _db);
    }

    [Fact]
    public async Task CreateMember_BegunstigerDuplicateEmail_ThrowsInvalidOperationException()
    {
        // Arrange
        var otherId = Guid.NewGuid();
        var existing = CreateTestMember(otherId, "dup@example.com");
        existing.Begunstiger = true;
        _db.Members.Add(existing);
        await _db.SaveChangesAsync();

        var study = new Study { Id = 1, Title = "S", NominalDurationYears = 3, Type = StudyType.Bachelor };
        _db.Studies.Add(study);
        await _db.SaveChangesAsync();

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
}
