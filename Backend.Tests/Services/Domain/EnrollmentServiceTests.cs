using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Backend.Controllers.DTOs;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models;
using Backend.Models.Domain;
using Backend.Services.Domain;
using Backend.Services.MailServices;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Operations;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Serialization;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Backend.Tests.Services.Domain;

public class EnrollmentServiceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<PostgresDbContext> _dbOptions;
    private readonly PostgresDbContext _db;
    private readonly IPermissionService _permissionService;
    private readonly IPaymentValidationService _paymentValidationService;
    private readonly AbstractMailService _mailService;
    private readonly EnrollmentService _service;
    private readonly Guid _userId = Guid.NewGuid();

    public EnrollmentServiceTests()
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
        _mailService = Substitute.For<AbstractMailService>(
            _db,
            _paymentValidationService,
            _permissionService,
            NullLogger<AbstractMailService>.Instance
        );

        _service = new EnrollmentService(
            _db,
            _permissionService,
            _paymentValidationService,
            _mailService,
            NullLogger<EnrollmentService>.Instance
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
    public async Task GetEnrollments_UserIsBoardMember_ReturnsFilteredEnrollments()
    {
        // Arrange
        var member = CreateMember("1234567");
        var activity = CreateActivity("Activity 1");
        _db.Members.Add(member);
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        var enrollment = new Enrollment
        {
            ActivityId = activity.Id,
            MemberId = member.Id,
            Price = 10,
            RegisteredOn = DateTime.UtcNow,
            IsOnWaitingList = false
        };
        _db.Enrollments.Add(enrollment);
        await _db.SaveChangesAsync();

        _permissionService.IsBoardOrCandidateBoardMember(_userId).Returns(true);
        _permissionService.HasPermissionOrBoard(_userId, Permission.ViewMembers, Arg.Any<uint?>()).Returns(true);

        var dto = new GetEnrollmentsDTO { FromMemberId = member.Id };

        // Act
        var result = await _service.GetEnrollments(dto, _userId, CancellationToken.None);

        // Assert
        var list = result.ToList();
        Assert.Single(list);
        Assert.Equal(member.Id, list[0].Member?.Id);
    }

    [Fact]
    public async Task GetEnrollments_UserIsNotBoardMemberAndRequestsOthers_EnsuresBoardPermission()
    {
        // Arrange
        var member1 = CreateMember("1111111");
        var member2 = CreateMember("2222222");
        var activity = CreateActivity("Activity 1");
        _db.Members.AddRange(member1, member2);
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        var enrollment = new Enrollment
        {
            ActivityId = activity.Id,
            MemberId = member2.Id,
            Price = 10,
            RegisteredOn = DateTime.UtcNow,
            IsOnWaitingList = false
        };
        _db.Enrollments.Add(enrollment);
        await _db.SaveChangesAsync();

        _permissionService.IsBoardOrCandidateBoardMember(member1.Id).Returns(false);
        _permissionService.When(p => p.EnsureBoardOrCandidateBoardMember(member1.Id))
            .Do(x => throw new UnauthorizedAccessException());

        var dto = new GetEnrollmentsDTO { FromMemberId = member2.Id };

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.GetEnrollments(dto, member1.Id, CancellationToken.None));
    }

    [Fact]
    public async Task GetEnrollment_Found_ReturnsDto()
    {
        // Arrange
        var member = CreateMember("1234567");
        var activity = CreateActivity("Activity 1");
        _db.Members.Add(member);
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        var enrollment = new Enrollment
        {
            ActivityId = activity.Id,
            MemberId = member.Id,
            Price = 10,
            RegisteredOn = DateTime.UtcNow,
            IsOnWaitingList = false
        };
        _db.Enrollments.Add(enrollment);
        await _db.SaveChangesAsync();

        _permissionService.IsBoardOrCandidateBoardMember(member.Id).Returns(false);

        // Act
        var result = await _service.GetEnrollment(activity.Id, member.Id, member.Id, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(member.Id, result.Member?.Id);
    }

    [Fact]
    public async Task GetEnrollment_NotFound_ReturnsNull()
    {
        // Arrange
        var member = CreateMember("1234567");
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        _permissionService.IsBoardOrCandidateBoardMember(member.Id).Returns(false);

        // Act
        var result = await _service.GetEnrollment(999u, member.Id, member.Id, CancellationToken.None);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetEnrollment_UnauthorizedUser_ThrowsUnauthorizedException()
    {
        // Arrange
        var member1 = CreateMember("1111111");
        var member2 = CreateMember("2222222");
        var activity = CreateActivity("Activity 1");
        _db.Members.AddRange(member1, member2);
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        _permissionService.IsBoardOrCandidateBoardMember(member1.Id).Returns(false);
        _permissionService.When(p => p.EnsureBoardOrCandidateBoardMember(member1.Id))
            .Do(x => throw new UnauthorizedAccessException());

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.GetEnrollment(activity.Id, member2.Id, member1.Id, CancellationToken.None));
    }

    [Fact]
    public async Task CreateEnrollment_MemberNotFound_ThrowsKeyNotFoundException()
    {
        var dto = new PostEnrollmentDTO { MemberId = Guid.NewGuid(), ActivityId = 1 };
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.CreateEnrollment(dto, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task CreateEnrollment_ActivityNotFound_ThrowsKeyNotFoundException()
    {
        var member = CreateMember("1234567");
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        var dto = new PostEnrollmentDTO { MemberId = member.Id, ActivityId = 999 };
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.CreateEnrollment(dto, member.Id, CancellationToken.None));
    }

    [Fact]
    public async Task CreateEnrollment_NoPaidMembership_ThrowsArgumentException()
    {
        var member = CreateMember("1234567");
        var activity = CreateActivity("Activity 1");
        _db.Members.Add(member);
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        _paymentValidationService.HasPaidMembershipPaymentBeforeExpirationTime(member.Id).Returns(false);

        var dto = new PostEnrollmentDTO { MemberId = member.Id, ActivityId = activity.Id };
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateEnrollment(dto, member.Id, CancellationToken.None));
    }

    [Fact]
    public async Task CreateEnrollment_Suspended_ThrowsArgumentException()
    {
        var member = CreateMember("1234567");
        member.Suspended = true;
        var activity = CreateActivity("Activity 1");
        _db.Members.Add(member);
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        _paymentValidationService.HasPaidMembershipPaymentBeforeExpirationTime(member.Id).Returns(true);

        var dto = new PostEnrollmentDTO { MemberId = member.Id, ActivityId = activity.Id };
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateEnrollment(dto, member.Id, CancellationToken.None));
    }

    [Fact]
    public async Task CreateEnrollment_AlreadyEnrolled_ThrowsArgumentException()
    {
        var member = CreateMember("1234567");
        var activity = CreateActivity("Activity 1");
        _db.Members.Add(member);
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        var enrollment = new Enrollment
        {
            ActivityId = activity.Id,
            MemberId = member.Id,
            Price = 10,
            RegisteredOn = DateTime.UtcNow,
            IsOnWaitingList = false
        };
        _db.Enrollments.Add(enrollment);
        await _db.SaveChangesAsync();

        _paymentValidationService.HasPaidMembershipPaymentBeforeExpirationTime(member.Id).Returns(true);

        var dto = new PostEnrollmentDTO { MemberId = member.Id, ActivityId = activity.Id };
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateEnrollment(dto, member.Id, CancellationToken.None));
    }

    [Fact]
    public async Task CreateEnrollment_AdultOnlyUnderage_ThrowsArgumentException()
    {
        var member = CreateMember("1234567");
        member.DateOfBirth = DateTime.UtcNow.AddYears(1);
        var activity = CreateActivity("Adult Activity");
        activity.IsAdultOnly = true;

        _db.Members.Add(member);
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        _paymentValidationService.HasPaidMembershipPaymentBeforeExpirationTime(member.Id).Returns(true);

        var dto = new PostEnrollmentDTO { MemberId = member.Id, ActivityId = activity.Id };
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateEnrollment(dto, member.Id, CancellationToken.None));
    }

    [Fact]
    public async Task CreateEnrollment_ActivityNotVisible_ThrowsUnauthorizedException()
    {
        var member = CreateMember("1234567");
        var activity = CreateActivity("Secret Activity");
        activity.ShowInKoala = false;

        _db.Members.Add(member);
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        _paymentValidationService.HasPaidMembershipPaymentBeforeExpirationTime(member.Id).Returns(true);
        _permissionService.IsBoardOrCandidateBoardMember(member.Id).Returns(false);

        var dto = new PostEnrollmentDTO { MemberId = member.Id, ActivityId = activity.Id };
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.CreateEnrollment(dto, member.Id, CancellationToken.None));
    }

    [Fact]
    public async Task CreateEnrollment_EnrollableFalseButOpenDateInPast_SetsEnrollableTrueAndProceeds()
    {
        var member = CreateMember("1234567");
        var activity = CreateActivity("Activity Open");
        activity.IsEnrollable = false;
        activity.EnrollOpenDate = DateTimeOffset.UtcNow.AddMinutes(-5);

        _db.Members.Add(member);
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        _paymentValidationService.HasPaidMembershipPaymentBeforeExpirationTime(member.Id).Returns(true);
        _permissionService.IsBoardOrCandidateBoardMember(member.Id).Returns(false);

        var dto = new PostEnrollmentDTO { MemberId = member.Id, ActivityId = activity.Id };

        var result = await _service.CreateEnrollment(dto, member.Id, CancellationToken.None);

        Assert.NotNull(result);
        _db.ChangeTracker.Clear();
        var savedActivity = await _db.Activities.FindAsync(activity.Id);
        Assert.True(savedActivity?.IsEnrollable);
        Assert.Null(savedActivity?.EnrollOpenDate);
    }

    [Fact]
    public async Task CreateEnrollment_EnrollableFalseAndOpenDateInFuture_ThrowsUnauthorizedAccessException()
    {
        var member = CreateMember("1234567");
        var activity = CreateActivity("Activity Future");
        activity.IsEnrollable = false;
        activity.EnrollOpenDate = DateTimeOffset.UtcNow.AddDays(1);

        _db.Members.Add(member);
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        _paymentValidationService.HasPaidMembershipPaymentBeforeExpirationTime(member.Id).Returns(true);
        _permissionService.IsBoardOrCandidateBoardMember(member.Id).Returns(false);

        var dto = new PostEnrollmentDTO { MemberId = member.Id, ActivityId = activity.Id };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.CreateEnrollment(dto, member.Id, CancellationToken.None));
    }

    [Fact]
    public async Task CreateEnrollment_DeadlinePassed_ThrowsUnauthorizedAccessException()
    {
        var member = CreateMember("1234567");
        var activity = CreateActivity("Activity Old");
        activity.EnrollmentDeadline = DateTime.UtcNow.AddDays(-1);

        _db.Members.Add(member);
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        _paymentValidationService.HasPaidMembershipPaymentBeforeExpirationTime(member.Id).Returns(true);
        _permissionService.IsBoardOrCandidateBoardMember(member.Id).Returns(false);

        var dto = new PostEnrollmentDTO { MemberId = member.Id, ActivityId = activity.Id };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.CreateEnrollment(dto, member.Id, CancellationToken.None));
    }

    [Fact]
    public async Task CreateEnrollment_DeadlinePassedButIsBoard_SavesEnrollment()
    {
        var member = CreateMember("1234567");
        var activity = CreateActivity("Activity Old");
        activity.EnrollmentDeadline = DateTime.UtcNow.AddDays(-1);

        _db.Members.Add(member);
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        _paymentValidationService.HasPaidMembershipPaymentBeforeExpirationTime(member.Id).Returns(true);
        _permissionService.IsBoardOrCandidateBoardMember(_userId).Returns(true);

        var dto = new PostEnrollmentDTO { MemberId = member.Id, ActivityId = activity.Id };

        var result = await _service.CreateEnrollment(dto, _userId, CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task CreateEnrollment_ExceedsParticipantLimit_GoesToWaitingList()
    {
        var member1 = CreateMember("1111111");
        var member2 = CreateMember("2222222");
        var activity = CreateActivity("Limited Activity");
        activity.ParticipantLimit = 1;

        _db.Members.AddRange(member1, member2);
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        var enrollment1 = new Enrollment { MemberId = member1.Id, ActivityId = activity.Id, Price = 10, RegisteredOn = DateTime.UtcNow, IsOnWaitingList = false };
        _db.Enrollments.Add(enrollment1);
        await _db.SaveChangesAsync();

        _paymentValidationService.HasPaidMembershipPaymentBeforeExpirationTime(member2.Id).Returns(true);
        _permissionService.IsBoardOrCandidateBoardMember(member2.Id).Returns(false);

        var dto = new PostEnrollmentDTO { MemberId = member2.Id, ActivityId = activity.Id };

        var result = await _service.CreateEnrollment(dto, member2.Id, CancellationToken.None);

        Assert.True(result.IsOnWaitingList);
    }

    [Fact]
    public async Task CreateEnrollment_NotAllowedAudience_GoesToWaitingList()
    {
        var member = CreateMember("1234567");
        var activity = CreateActivity("Freshmen Activity");
        activity.AllowedAudience = TargetAudience.FirstYears; // FirstYears will require study enrollment in database which isn't there, so they are not in audience

        _db.Members.Add(member);
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        _paymentValidationService.HasPaidMembershipPaymentBeforeExpirationTime(member.Id).Returns(true);
        _permissionService.IsBoardOrCandidateBoardMember(member.Id).Returns(false);

        var dto = new PostEnrollmentDTO { MemberId = member.Id, ActivityId = activity.Id };

        var result = await _service.CreateEnrollment(dto, member.Id, CancellationToken.None);

        Assert.True(result.IsOnWaitingList);
    }

    [Fact]
    public async Task DeleteEnrollment_NotOnWaitingList_PromotesNextFromWaitingListAndMails()
    {
        var memberToDelete = CreateMember("1111111");
        var memberToPromote = CreateMember("2222222");
        var activity = CreateActivity("Waitlist Activity");
        activity.ParticipantLimit = 1;

        _db.Members.AddRange(memberToDelete, memberToPromote);
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        var enrollment1 = new Enrollment { MemberId = memberToDelete.Id, ActivityId = activity.Id, Price = 10, RegisteredOn = DateTime.UtcNow.AddMinutes(-10), IsOnWaitingList = false };
        var enrollment2 = new Enrollment { MemberId = memberToPromote.Id, ActivityId = activity.Id, Price = 10, RegisteredOn = DateTime.UtcNow.AddMinutes(-5), IsOnWaitingList = true };
        _db.Enrollments.AddRange(enrollment1, enrollment2);
        await _db.SaveChangesAsync();

        _permissionService.IsBoardOrCandidateBoardMember(memberToDelete.Id).Returns(false);

        // Act
        await _service.DeleteEnrollment(activity.Id, memberToDelete.Id, memberToDelete.Id, CancellationToken.None);

        // Assert
        _db.ChangeTracker.Clear();
        var deleted = await _db.Enrollments.FirstOrDefaultAsync(e => e.MemberId == memberToDelete.Id && e.ActivityId == activity.Id);
        Assert.Null(deleted);

        var promoted = await _db.Enrollments.FirstOrDefaultAsync(e => e.MemberId == memberToPromote.Id && e.ActivityId == activity.Id);
        Assert.NotNull(promoted);
        Assert.False(promoted.IsOnWaitingList);

        await _mailService.Received(1).SendEnrollmentPromotionEmail(Arg.Is<Enrollment>(e => e.MemberId == memberToPromote.Id));
    }

    [Fact]
    public async Task DeleteEnrollment_MailSendThrows_SwallowsException()
    {
        var memberToDelete = CreateMember("1111111");
        var memberToPromote = CreateMember("2222222");
        var activity = CreateActivity("Waitlist Activity");
        activity.ParticipantLimit = 1;

        _db.Members.AddRange(memberToDelete, memberToPromote);
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        var enrollment1 = new Enrollment { MemberId = memberToDelete.Id, ActivityId = activity.Id, Price = 10, RegisteredOn = DateTime.UtcNow.AddMinutes(-10), IsOnWaitingList = false };
        var enrollment2 = new Enrollment { MemberId = memberToPromote.Id, ActivityId = activity.Id, Price = 10, RegisteredOn = DateTime.UtcNow.AddMinutes(-5), IsOnWaitingList = true };
        _db.Enrollments.AddRange(enrollment1, enrollment2);
        await _db.SaveChangesAsync();

        _permissionService.IsBoardOrCandidateBoardMember(memberToDelete.Id).Returns(false);
        _mailService.SendEnrollmentPromotionEmail(Arg.Any<Enrollment>()).Throws(new Exception("Mail error"));

        // Act & Assert
        await _service.DeleteEnrollment(activity.Id, memberToDelete.Id, memberToDelete.Id, CancellationToken.None);
    }

    [Fact]
    public async Task DeleteEnrollment_NotFound_ThrowsKeyNotFoundException()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.DeleteEnrollment(999u, Guid.NewGuid(), _userId, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteEnrollment_DeadlinePassed_ThrowsUnauthorizedAccessException()
    {
        var member = CreateMember("1234567");
        var activity = CreateActivity("Deadline Passed");
        activity.DateTimeStart = DateTime.UtcNow.AddDays(-1);
        activity.DateTimeEnd = DateTime.UtcNow.AddDays(-1);

        _db.Members.Add(member);
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        var enrollment = new Enrollment { MemberId = member.Id, ActivityId = activity.Id, Price = 10, RegisteredOn = DateTime.UtcNow, IsOnWaitingList = false };
        _db.Enrollments.Add(enrollment);
        await _db.SaveChangesAsync();

        _permissionService.IsBoardOrCandidateBoardMember(member.Id).Returns(false);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.DeleteEnrollment(activity.Id, member.Id, member.Id, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteEnrollment_BoardMemberDeletesOtherPastDeadline_UsesActingUsersBoardStatus()
    {
        // The unenrollment-deadline check must be based on whether the *acting* user (userId) is a board
        // member, not the enrolled member being removed - a board member should be able to unenroll a
        // regular member even after that member's own unenrollment deadline has passed.
        var boardMember = CreateMember("1111111");
        var regularMember = CreateMember("2222222");
        var activity = CreateActivity("Deadline Passed");
        activity.DateTimeStart = DateTime.UtcNow.AddDays(-1);
        activity.DateTimeEnd = DateTime.UtcNow.AddDays(-1);
        activity.UnenrollmentDeadline = DateTime.UtcNow.AddDays(-1);

        _db.Members.AddRange(boardMember, regularMember);
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        var enrollment = new Enrollment { MemberId = regularMember.Id, ActivityId = activity.Id, Price = 10, RegisteredOn = DateTime.UtcNow, IsOnWaitingList = false };
        _db.Enrollments.Add(enrollment);
        await _db.SaveChangesAsync();

        _permissionService.IsBoardOrCandidateBoardMember(boardMember.Id).Returns(true);
        _permissionService.IsBoardOrCandidateBoardMember(regularMember.Id).Returns(false);

        // Act - boardMember (userId) removes regularMember (enrolledUser) after the deadline passed.
        await _service.DeleteEnrollment(activity.Id, regularMember.Id, boardMember.Id, CancellationToken.None);

        // Assert
        _db.ChangeTracker.Clear();
        var remaining = await _db.Enrollments.FirstOrDefaultAsync(e => e.MemberId == regularMember.Id && e.ActivityId == activity.Id);
        Assert.Null(remaining);
    }

    [Fact]
    public async Task DeleteEnrollment_DeleteOthersWithoutBoard_ThrowsUnauthorized()
    {
        var member1 = CreateMember("1111111");
        var member2 = CreateMember("2222222");
        var activity = CreateActivity("Activity");

        _db.Members.AddRange(member1, member2);
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        _permissionService.When(p => p.EnsureBoardOrCandidateBoardMember(member1.Id))
            .Do(x => throw new UnauthorizedAccessException());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.DeleteEnrollment(activity.Id, member2.Id, member1.Id, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateEnrollment_EnrollmentNotFound_ThrowsKeyNotFoundException()
    {
        var dto = new PostEnrollmentDTO { MemberId = Guid.NewGuid(), ActivityId = 1 };
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.UpdateEnrollment(dto, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateEnrollment_DeadlinePassed_ThrowsUnauthorizedAccessException()
    {
        var member = CreateMember("1234567");
        var activity = CreateActivity("Deadline Passed");
        activity.DateTimeEnd = DateTime.UtcNow.AddDays(-1);

        _db.Members.Add(member);
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        var enrollment = new Enrollment { MemberId = member.Id, ActivityId = activity.Id, Price = 10, RegisteredOn = DateTime.UtcNow, IsOnWaitingList = false };
        _db.Enrollments.Add(enrollment);
        await _db.SaveChangesAsync();

        _permissionService.IsBoardOrCandidateBoardMember(member.Id).Returns(false);

        var dto = new PostEnrollmentDTO { MemberId = member.Id, ActivityId = activity.Id };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.UpdateEnrollment(dto, member.Id, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateEnrollment_MembersUpdateOthers_ThrowsUnauthorizedAccessException()
    {
        var member1 = CreateMember("1111111");
        var member2 = CreateMember("2222222");
        var activity = CreateActivity("Activity");

        _db.Members.AddRange(member1, member2);
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        var enrollment = new Enrollment { MemberId = member2.Id, ActivityId = activity.Id, Price = 10, RegisteredOn = DateTime.UtcNow, IsOnWaitingList = false };
        _db.Enrollments.Add(enrollment);
        await _db.SaveChangesAsync();

        _permissionService.IsBoardOrCandidateBoardMember(member1.Id).Returns(false);

        var dto = new PostEnrollmentDTO { MemberId = member2.Id, ActivityId = activity.Id };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.UpdateEnrollment(dto, member1.Id, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateEnrollment_ValidRequest_UpdatesSpecificationAnswers()
    {
        var member = CreateMember("1234567");
        var activity = CreateActivity("Activity");
        var question = new SpecificationQuestion
        {
            Id = 10,
            QuestionDutch = "Leeftijd?",
            QuestionEnglish = "Age?",
            Type = QuestionType.Number,
            IsMandatory = true,
            Options = ""
        };
        activity.SpecificationQuestions = new List<SpecificationQuestion> { question };

        _db.Members.Add(member);
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        var enrollment = new Enrollment { MemberId = member.Id, ActivityId = activity.Id, Price = 10, RegisteredOn = DateTime.UtcNow, IsOnWaitingList = false };
        _db.Enrollments.Add(enrollment);
        await _db.SaveChangesAsync();

        _permissionService.IsBoardOrCandidateBoardMember(member.Id).Returns(false);

        var dto = new PostEnrollmentDTO
        {
            MemberId = member.Id,
            ActivityId = activity.Id,
            SpecificationAnswers = new List<PostSpecificationAnswerDTO>
            {
                new PostSpecificationAnswerDTO { QuestionId = 10, Answer = "25" }
            }
        };

        await _service.UpdateEnrollment(dto, member.Id, CancellationToken.None);

        _db.ChangeTracker.Clear();
        var updatedEnrollment = await _db.Enrollments.Include(e => e.SpecificationAnswers).FirstAsync(e => e.MemberId == member.Id);
        Assert.Single(updatedEnrollment.SpecificationAnswers);
        Assert.Equal("25", updatedEnrollment.SpecificationAnswers.First().Answer);
    }

    [Fact]
    public async Task PatchEnrollment_NullPatchDoc_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.PatchEnrollment(1u, Guid.NewGuid(), null!, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task PatchEnrollment_PatchId_ThrowsArgumentException()
    {
        var patchDoc = new JsonPatchDocument<Enrollment>();
        patchDoc.Replace(e => e.ActivityId, 999u);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.PatchEnrollment(1u, Guid.NewGuid(), patchDoc, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task PatchEnrollment_NotFound_ThrowsKeyNotFoundException()
    {
        var patchDoc = new JsonPatchDocument<Enrollment>();
        patchDoc.Replace(e => e.SpecificationAnswers, new List<SpecificationAnswer>());

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _service.PatchEnrollment(1u, Guid.NewGuid(), patchDoc, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task PatchEnrollment_DeadlinePassed_ThrowsUnauthorizedAccessException()
    {
        var member = CreateMember("1234567");
        var activity = CreateActivity("Deadline Passed");
        activity.DateTimeEnd = DateTime.UtcNow.AddDays(-1);

        _db.Members.Add(member);
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        var enrollment = new Enrollment { MemberId = member.Id, ActivityId = activity.Id, Price = 10, RegisteredOn = DateTime.UtcNow, IsOnWaitingList = false };
        _db.Enrollments.Add(enrollment);
        await _db.SaveChangesAsync();

        _permissionService.IsBoardOrCandidateBoardMember(member.Id).Returns(false);

        var patchDoc = new JsonPatchDocument<Enrollment>();
        patchDoc.Replace(e => e.SpecificationAnswers, new List<SpecificationAnswer>());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.PatchEnrollment(activity.Id, member.Id, patchDoc, member.Id, CancellationToken.None));
    }

    [Fact]
    public async Task PatchEnrollment_MembersPatchOthers_ThrowsUnauthorizedAccessException()
    {
        var member1 = CreateMember("1111111");
        var member2 = CreateMember("2222222");
        var activity = CreateActivity("Activity");

        _db.Members.AddRange(member1, member2);
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        var enrollment = new Enrollment { MemberId = member2.Id, ActivityId = activity.Id, Price = 10, RegisteredOn = DateTime.UtcNow, IsOnWaitingList = false };
        _db.Enrollments.Add(enrollment);
        await _db.SaveChangesAsync();

        _permissionService.IsBoardOrCandidateBoardMember(member1.Id).Returns(false);

        var patchDoc = new JsonPatchDocument<Enrollment>();
        patchDoc.Replace(e => e.SpecificationAnswers, new List<SpecificationAnswer>());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _service.PatchEnrollment(activity.Id, member2.Id, patchDoc, member1.Id, CancellationToken.None));
    }

    [Fact]
    public async Task PatchEnrollment_RestrictedField_ThrowsArgumentException()
    {
        var patchDoc = new JsonPatchDocument<Enrollment>();
        patchDoc.Replace(e => e.Price, 20);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.PatchEnrollment(1u, Guid.NewGuid(), patchDoc, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task PatchEnrollment_ValidRequest_AppliesChanges()
    {
        var member = CreateMember("1234567");
        var activity = CreateActivity("Activity");

        _db.Members.Add(member);
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        var enrollment = new Enrollment { MemberId = member.Id, ActivityId = activity.Id, Price = 10, RegisteredOn = DateTime.UtcNow, IsOnWaitingList = false };
        _db.Enrollments.Add(enrollment);
        await _db.SaveChangesAsync();

        _permissionService.IsBoardOrCandidateBoardMember(member.Id).Returns(false);

        var patchDoc = new JsonPatchDocument<Enrollment>();
        patchDoc.Replace(e => e.SpecificationAnswers, new List<SpecificationAnswer>());

        await _service.PatchEnrollment(activity.Id, member.Id, patchDoc, member.Id, CancellationToken.None);

        _db.ChangeTracker.Clear();
        var updated = await _db.Enrollments.Include(e => e.SpecificationAnswers).FirstAsync(e => e.MemberId == member.Id);
        Assert.Empty(updated.SpecificationAnswers);
        Assert.Equal(10, updated.Price);
    }

    [Fact]
    public async Task PromoteFromWaitingList_PromotesInOrderAndChangesWaitingListStatus()
    {
        var member1 = CreateMember("1111111");
        var member2 = CreateMember("2222222");
        var activity = CreateActivity("Activity");

        _db.Members.AddRange(member1, member2);
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        var enrollment1 = new Enrollment { MemberId = member1.Id, ActivityId = activity.Id, Price = 10, RegisteredOn = DateTime.UtcNow.AddMinutes(-10), IsOnWaitingList = true };
        var enrollment2 = new Enrollment { MemberId = member2.Id, ActivityId = activity.Id, Price = 10, RegisteredOn = DateTime.UtcNow.AddMinutes(-5), IsOnWaitingList = true };
        _db.Enrollments.AddRange(enrollment1, enrollment2);
        await _db.SaveChangesAsync();

        var result = await _service.PromoteFromWaitingList(activity.Id, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(member1.Id, result.MemberId);

        await _db.SaveChangesAsync();
        _db.ChangeTracker.Clear();
        var list = await _db.Enrollments.Where(e => e.ActivityId == activity.Id).ToListAsync();

        // Only the single promoted enrollment (member1, earliest RegisteredOn) should leave the
        // waiting list - member2's enrollment was not promoted and should remain on it.
        Assert.False(list.Single(e => e.MemberId == member1.Id).IsOnWaitingList);
        Assert.True(list.Single(e => e.MemberId == member2.Id).IsOnWaitingList);
    }

    [Fact]
    public async Task CreateEnrollment_WithSpecificationAnswers_CreatesSpecificationAnswers()
    {
        var member = CreateMember("1234567");
        var activity = CreateActivity("Activity");
        var question = new SpecificationQuestion
        {
            Id = 10,
            QuestionDutch = "Leeftijd?",
            QuestionEnglish = "Age?",
            Type = QuestionType.Number,
            IsMandatory = true,
            Options = ""
        };
        activity.SpecificationQuestions = new List<SpecificationQuestion> { question };

        _db.Members.Add(member);
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        _permissionService.IsBoardOrCandidateBoardMember(member.Id).Returns(false);
        _paymentValidationService.HasPaidMembershipPaymentBeforeExpirationTime(member.Id).Returns(true);

        var dto = new PostEnrollmentDTO
        {
            MemberId = member.Id,
            ActivityId = activity.Id,
            SpecificationAnswers = new List<PostSpecificationAnswerDTO>
            {
                new PostSpecificationAnswerDTO { QuestionId = 10, Answer = "25" }
            }
        };

        var result = await _service.CreateEnrollment(dto, member.Id, CancellationToken.None);

        Assert.NotNull(result);
        _db.ChangeTracker.Clear();
        var created = await _db.Enrollments.Include(e => e.SpecificationAnswers).FirstAsync(e => e.MemberId == member.Id);
        Assert.Single(created.SpecificationAnswers);
        Assert.Equal("25", created.SpecificationAnswers.First().Answer);
    }
}
