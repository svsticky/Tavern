using Backend.Database;
using Backend.Models.Domain;
using Backend.Services.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Backend.Tests.Services;

public class PaymentValidationServiceTests : IDisposable
{
    private readonly PostgresDbContext _db;
    private readonly ILogger<PaymentValidationService> _loggerMock;
    private readonly PaymentValidationService _service;

    public PaymentValidationServiceTests()
    {
        var options = new DbContextOptionsBuilder<PostgresDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new PostgresDbContext(options);
        _db.Database.EnsureCreated();

        _loggerMock = Substitute.For<ILogger<PaymentValidationService>>();
        _service = new PaymentValidationService(_db, _loggerMock);
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }

    private Member CreateMember(Guid? id = null)
    {
        return new Member
        {
            Id = id ?? Guid.NewGuid(),
            FirstName = "John",
            LastName = "Doe",
            Email = Guid.NewGuid().ToString() + "@example.com",
            StudentNumber = "s" + Guid.NewGuid().ToString().Substring(0, 7),
            PhoneNumber = "+31612345678",
            Street = "Main Street",
            HouseNumber = "42",
            PostalCode = "1234AB",
            City = "Enschede"
        };
    }

    [Fact]
    public void HasPaidMembershipPaymentBeforeExpirationTime_MemberNotFound_ThrowsException()
    {
        var memberId = Guid.NewGuid();

        var exception = Assert.Throws<Exception>(() => _service.HasPaidMembershipPaymentBeforeExpirationTime(memberId));
        Assert.Equal($"Member with id {memberId} not found.", exception.Message);
    }

    [Fact]
    public void HasPaidMembershipPaymentBeforeExpirationTime_IsBegunstiger_ReturnsTrue()
    {
        var member = CreateMember();
        member.Begunstiger = true;
        _db.Members.Add(member);
        _db.SaveChanges();

        var result = _service.HasPaidMembershipPaymentBeforeExpirationTime(member.Id);

        Assert.True(result);
    }

    [Fact]
    public void HasPaidMembershipPaymentBeforeExpirationTime_MasterShouldNotPay_ReturnsTrue()
    {
        var member = CreateMember();
        _db.Members.Add(member);

        var study = new Study { Id = 1, Title = "Computer Science", Type = StudyType.Master };
        _db.Studies.Add(study);

        _db.StudyEnrollments.Add(new StudyEnrollment
        {
            MemberId = member.Id,
            StudyId = study.Id,
            Status = StudyStatus.Enrolled,
            EnrollmentDate = DateTimeOffset.UtcNow
        });

        _db.Settings.Add(new Setting { Name = "MastersShouldPayMembership", Value = "0" });
        _db.SaveChanges();

        var result = _service.HasPaidMembershipPaymentBeforeExpirationTime(member.Id);

        Assert.True(result);
    }

    [Fact]
    public void HasPaidMembershipPaymentBeforeExpirationTime_GratieShouldNotPay_ReturnsTrue()
    {
        var member = CreateMember();
        member.Gratie = true;
        _db.Members.Add(member);

        _db.Settings.Add(new Setting { Name = "GratieShouldPayMembership", Value = "0" });
        _db.SaveChanges();

        var result = _service.HasPaidMembershipPaymentBeforeExpirationTime(member.Id);

        Assert.True(result);
    }

    [Fact]
    public void HasPaidMembershipPaymentBeforeExpirationTime_ErelidShouldNotPay_ReturnsTrue()
    {
        var member = CreateMember();
        member.EreLid = true;
        _db.Members.Add(member);

        _db.Settings.Add(new Setting { Name = "ErelidShouldPayMembership", Value = "0" });
        _db.SaveChanges();

        var result = _service.HasPaidMembershipPaymentBeforeExpirationTime(member.Id);

        Assert.True(result);
    }

    [Fact]
    public void HasPaidMembershipPaymentBeforeExpirationTime_LidVanVerdiensteShouldNotPay_ReturnsTrue()
    {
        var member = CreateMember();
        member.LidVanVerdienste = true;
        _db.Members.Add(member);

        _db.Settings.Add(new Setting { Name = "LidVanVerdiensteShouldPayMembership", Value = "0" });
        _db.SaveChanges();

        var result = _service.HasPaidMembershipPaymentBeforeExpirationTime(member.Id);

        Assert.True(result);
    }

    [Fact]
    public void HasPaidMembershipPaymentBeforeExpirationTime_WithExpirationSetting_ChecksThreshold()
    {
        var member = CreateMember();
        _db.Members.Add(member);

        _db.Settings.Add(new Setting { Name = "MembershipPaymentExpirationTime", Value = "2" }); // 2 years

        // Add a payment that is expired (3 years ago)
        _db.MembershipPayments.Add(new MembershipPayment
        {
            Id = 1,
            MemberId = member.Id,
            Price = 15.00m,
            PaymentServiceId = "pay_1",
            PaymentIntentUrl = "http://url",
            PaidAt = DateTime.UtcNow.AddYears(-3)
        });

        // Add a payment that is valid (1 year ago)
        _db.MembershipPayments.Add(new MembershipPayment
        {
            Id = 2,
            MemberId = member.Id,
            Price = 15.00m,
            PaymentServiceId = "pay_2",
            PaymentIntentUrl = "http://url",
            PaidAt = DateTime.UtcNow.AddYears(-1)
        });

        _db.SaveChanges();

        var result = _service.HasPaidMembershipPaymentBeforeExpirationTime(member.Id);

        Assert.True(result);
    }

    [Fact]
    public void HasPaidMembershipPaymentBeforeExpirationTime_WithExpirationSetting_NoValidPayment_ReturnsFalse()
    {
        var member = CreateMember();
        _db.Members.Add(member);

        _db.Settings.Add(new Setting { Name = "MembershipPaymentExpirationTime", Value = "2" });

        // Add a payment that is expired (3 years ago)
        _db.MembershipPayments.Add(new MembershipPayment
        {
            Id = 1,
            MemberId = member.Id,
            Price = 15.00m,
            PaymentServiceId = "pay_1",
            PaymentIntentUrl = "http://url",
            PaidAt = DateTime.UtcNow.AddYears(-3)
        });

        _db.SaveChanges();

        var result = _service.HasPaidMembershipPaymentBeforeExpirationTime(member.Id);

        Assert.False(result);
    }

    [Fact]
    public void HasPaidMembershipPaymentBeforeExpirationTime_NoExpirationSetting_ReturnsTrueIfAnyPaid()
    {
        var member = CreateMember();
        _db.Members.Add(member);

        _db.MembershipPayments.Add(new MembershipPayment
        {
            Id = 1,
            MemberId = member.Id,
            Price = 15.00m,
            PaymentServiceId = "pay_1",
            PaymentIntentUrl = "http://url",
            PaidAt = DateTime.UtcNow.AddYears(-5)
        });

        _db.SaveChanges();

        var result = _service.HasPaidMembershipPaymentBeforeExpirationTime(member.Id);

        Assert.True(result);
    }

    [Fact]
    public void HasEverPaidMembershipPayment_MemberNotFound_ThrowsException()
    {
        var memberId = Guid.NewGuid();

        Assert.Throws<Exception>(() => _service.HasEverPaidMembershipPayment(memberId));
    }

    [Fact]
    public void HasEverPaidMembershipPayment_IsBegunstiger_ReturnsTrue()
    {
        var member = CreateMember();
        member.Begunstiger = true;
        _db.Members.Add(member);
        _db.SaveChanges();

        var result = _service.HasEverPaidMembershipPayment(member.Id);

        Assert.True(result);
    }

    [Fact]
    public void HasEverPaidMembershipPayment_IsMaster_ReturnsTrue()
    {
        var member = CreateMember();
        _db.Members.Add(member);

        var study = new Study { Id = 1, Title = "Computer Science", Type = StudyType.Master };
        _db.Studies.Add(study);

        _db.StudyEnrollments.Add(new StudyEnrollment
        {
            MemberId = member.Id,
            StudyId = study.Id,
            Status = StudyStatus.Enrolled,
            EnrollmentDate = DateTimeOffset.UtcNow
        });
        _db.SaveChanges();

        var result = _service.HasEverPaidMembershipPayment(member.Id);

        Assert.True(result);
    }

    [Fact]
    public void HasEverPaidMembershipPayment_HasPaid_ReturnsTrue()
    {
        var member = CreateMember();
        _db.Members.Add(member);

        _db.MembershipPayments.Add(new MembershipPayment
        {
            Id = 1,
            MemberId = member.Id,
            Price = 15.00m,
            PaymentServiceId = "pay_1",
            PaymentIntentUrl = "http://url",
            PaidAt = DateTime.UtcNow
        });
        _db.SaveChanges();

        var result = _service.HasEverPaidMembershipPayment(member.Id);

        Assert.True(result);
    }

    [Fact]
    public void HasEverPaidMembershipPayment_NotPaid_ReturnsFalse()
    {
        var member = CreateMember();
        _db.Members.Add(member);
        _db.SaveChanges();

        var result = _service.HasEverPaidMembershipPayment(member.Id);

        Assert.False(result);
    }

    [Fact]
    public void GetUnpaidEnrollmentsForMember_ReturnsCorrectEnrollments()
    {
        var member = CreateMember();
        _db.Members.Add(member);

        var activity1 = new Activity
        {
            Id = 1,
            Name = "Beer Pong",
            IsOpenForPayment = true,
            DateTimeStart = DateTimeOffset.UtcNow,
            DateTimeEnd = DateTimeOffset.UtcNow.AddHours(2),
            DutchDescription = "NL",
            EnglishDescription = "EN",
            Location = "Tavern",
            PaymentDeadline = DateTimeOffset.UtcNow.AddDays(7)
        };
        var activity2 = new Activity
        {
            Id = 2,
            Name = "Lan Party",
            IsOpenForPayment = true,
            DateTimeStart = DateTimeOffset.UtcNow,
            DateTimeEnd = DateTimeOffset.UtcNow.AddHours(2),
            DutchDescription = "NL",
            EnglishDescription = "EN",
            Location = "Tavern",
            PaymentDeadline = DateTimeOffset.UtcNow.AddDays(7)
        };
        _db.Activities.AddRange(activity1, activity2);

        var enrollment1 = new Enrollment
        {
            MemberId = member.Id,
            ActivityId = 1,
            Price = 10.00m,
            IsOnWaitingList = false,
            RegisteredOn = DateTime.UtcNow
        };
        var enrollment2 = new Enrollment
        {
            MemberId = member.Id,
            ActivityId = 2,
            Price = 15.00m,
            IsOnWaitingList = false,
            RegisteredOn = DateTime.UtcNow
        };
        _db.Enrollments.AddRange(enrollment1, enrollment2);

        // Add partial payment for enrollment1
        _db.EnrollmentPayments.Add(new EnrollmentPayment
        {
            Id = 1,
            MemberId = member.Id,
            ActivityId = 1,
            Price = 4.00m,
            PaidAt = DateTimeOffset.UtcNow,
            PaymentServiceId = "pay_e1",
            PaymentIntentUrl = "http://url"
        });

        // Add full payment for enrollment2
        _db.EnrollmentPayments.Add(new EnrollmentPayment
        {
            Id = 2,
            MemberId = member.Id,
            ActivityId = 2,
            Price = 15.00m,
            PaidAt = DateTimeOffset.UtcNow,
            PaymentServiceId = "pay_e2",
            PaymentIntentUrl = "http://url"
        });

        _db.SaveChanges();

        var result = _service.GetUnpaidEnrollmentsForMember(member.Id).ToList();

        Assert.Single(result);
        Assert.Equal(1u, result[0].Enrollment.ActivityId);
        Assert.Equal(6.00m, result[0].Balance);
    }

    [Fact]
    public void GetUnpaidAmountForEnrollment_ReturnsRemainingAmount()
    {
        var enrollment = new Enrollment
        {
            MemberId = Guid.NewGuid(),
            ActivityId = 5,
            Price = 20.00m,
            IsOnWaitingList = false,
            RegisteredOn = DateTime.UtcNow
        };

        var activity = new Activity
        {
            Id = 5,
            Name = "Gala",
            IsOpenForPayment = true,
            DateTimeStart = DateTimeOffset.UtcNow,
            DateTimeEnd = DateTimeOffset.UtcNow.AddHours(2),
            DutchDescription = "NL",
            EnglishDescription = "EN",
            Location = "Tavern",
            PaymentDeadline = DateTimeOffset.UtcNow.AddDays(7)
        };
        _db.Activities.Add(activity);
        enrollment.Activity = activity;

        _db.EnrollmentPayments.Add(new EnrollmentPayment
        {
            Id = 1,
            MemberId = enrollment.MemberId,
            ActivityId = 5,
            Price = 8.00m,
            PaidAt = DateTimeOffset.UtcNow,
            PaymentServiceId = "pay_e3",
            PaymentIntentUrl = "http://url"
        });
        _db.SaveChanges();

        var balance = _service.GetUnpaidAmountForEnrollment(enrollment);

        Assert.Equal(12.00m, balance);
    }

    [Fact]
    public void GetAllUnpaidEnrollments_ReturnsAllUnpaid()
    {
        var member1 = CreateMember();
        var member2 = CreateMember();
        _db.Members.AddRange(member1, member2);

        var activity = new Activity
        {
            Id = 1,
            Name = "Cantus",
            IsOpenForPayment = true,
            DateTimeStart = DateTimeOffset.UtcNow,
            DateTimeEnd = DateTimeOffset.UtcNow.AddHours(2),
            DutchDescription = "NL",
            EnglishDescription = "EN",
            Location = "Tavern",
            PaymentDeadline = DateTimeOffset.UtcNow.AddDays(7)
        };
        _db.Activities.Add(activity);

        var enrollment1 = new Enrollment
        {
            MemberId = member1.Id,
            ActivityId = 1,
            Price = 12.00m,
            IsOnWaitingList = false,
            RegisteredOn = DateTime.UtcNow
        };
        var enrollment2 = new Enrollment
        {
            MemberId = member2.Id,
            ActivityId = 1,
            Price = 12.00m,
            IsOnWaitingList = false,
            RegisteredOn = DateTime.UtcNow
        };
        _db.Enrollments.AddRange(enrollment1, enrollment2);

        // member1 paid fully
        _db.EnrollmentPayments.Add(new EnrollmentPayment
        {
            Id = 1,
            MemberId = member1.Id,
            ActivityId = 1,
            Price = 12.00m,
            PaidAt = DateTimeOffset.UtcNow,
            PaymentServiceId = "pay_m1",
            PaymentIntentUrl = "http://url"
        });
        _db.SaveChanges();

        var result = _service.GetAllUnpaidEnrollments().ToList();

        Assert.Single(result);
        Assert.Equal(member2.Id, result[0].Enrollment.MemberId);
        Assert.Equal(12.00m, result[0].Balance);
    }

    [Fact]
    public void GetAllOverpaidEnrollments_ReturnsAllOverpaid()
    {
        var member = CreateMember();
        _db.Members.Add(member);

        var activity = new Activity
        {
            Id = 1,
            Name = "Drinks",
            IsOpenForPayment = true,
            DateTimeStart = DateTimeOffset.UtcNow,
            DateTimeEnd = DateTimeOffset.UtcNow.AddHours(2),
            DutchDescription = "NL",
            EnglishDescription = "EN",
            Location = "Tavern",
            PaymentDeadline = DateTimeOffset.UtcNow.AddDays(7)
        };
        _db.Activities.Add(activity);

        var enrollment = new Enrollment
        {
            MemberId = member.Id,
            ActivityId = 1,
            Price = 10.00m,
            IsOnWaitingList = false,
            RegisteredOn = DateTime.UtcNow
        };
        _db.Enrollments.Add(enrollment);

        // paid 15.00m instead of 10.00m
        _db.EnrollmentPayments.Add(new EnrollmentPayment
        {
            Id = 1,
            MemberId = member.Id,
            ActivityId = 1,
            Price = 15.00m,
            PaidAt = DateTimeOffset.UtcNow,
            PaymentServiceId = "pay_over",
            PaymentIntentUrl = "http://url"
        });
        _db.SaveChanges();

        var result = _service.GetAllOverpaidEnrollments().ToList();

        Assert.Single(result);
        Assert.Equal(member.Id, result[0].Enrollment.MemberId);
        Assert.Equal(5.00m, result[0].Balance);
    }

    [Fact]
    public void MemberHasPaidAllActivities_NoUnpaid_ReturnsTrue()
    {
        var member = CreateMember();
        _db.Members.Add(member);
        _db.SaveChanges();

        var result = _service.MemberHasPaidAllActivities(member);

        Assert.True(result);
    }

    [Fact]
    public void MemberHasPaidAllActivities_HasUnpaid_ReturnsFalse()
    {
        var member = CreateMember();
        _db.Members.Add(member);

        var activity = new Activity
        {
            Id = 1,
            Name = "Drinks",
            IsOpenForPayment = true,
            DateTimeStart = DateTimeOffset.UtcNow,
            DateTimeEnd = DateTimeOffset.UtcNow.AddHours(2),
            DutchDescription = "NL",
            EnglishDescription = "EN",
            Location = "Tavern",
            PaymentDeadline = DateTimeOffset.UtcNow.AddDays(7)
        };
        _db.Activities.Add(activity);

        var enrollment = new Enrollment
        {
            MemberId = member.Id,
            ActivityId = 1,
            Price = 10.00m,
            IsOnWaitingList = false,
            RegisteredOn = DateTime.UtcNow
        };
        _db.Enrollments.Add(enrollment);
        _db.SaveChanges();

        var result = _service.MemberHasPaidAllActivities(member);

        Assert.False(result);
    }
}
