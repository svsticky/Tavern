using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Backend.Controllers.DTOs;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models;
using Backend.Models.Domain;
using Backend.Services.MailServices;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Backend.Tests.Services.MailServices;

// A test implementation of AbstractMailService to test base class logic
public class MockAbstractMailService : AbstractMailService
{
    public MailRecipient? LastFrom { get; private set; }
    public MailRecipient[]? LastTo { get; private set; }
    public string? LastSubject { get; private set; }
    public string? LastHtmlContent { get; private set; }
    public List<string> SentToEmails { get; } = new();

    /// <summary>
    /// When set, recipients whose mail address satisfies this predicate make SendEmailCoreAsync throw,
    /// so tests can simulate a single bad recipient in an otherwise-successful batch.
    /// </summary>
    public Func<string, bool>? FailFor { get; set; }

    public MockAbstractMailService(
        PostgresDbContext db,
        IPaymentValidationService paymentValidationService,
        IPermissionService permissionService,
        ILogger<AbstractMailService> logger) : base(db, paymentValidationService, permissionService, logger)
    {
    }

    protected override Task SendEmailCoreAsync(MailRecipient from, MailRecipient[] to, string subject, string htmlContent, CancellationToken ct)
    {
        LastFrom = from;
        LastTo = to;
        LastSubject = subject;
        LastHtmlContent = htmlContent;
        SentToEmails.AddRange(to.Select(r => r.Mail));

        if (FailFor != null && to.Any(r => FailFor(r.Mail)))
        {
            throw new InvalidOperationException($"Simulated send failure for {to[0].Mail}");
        }

        return Task.CompletedTask;
    }

    public string PublicStripHtml(string html) => StripHtml(html);
}

public class MailServicesTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly PostgresDbContext _db;
    private readonly IPaymentValidationService _paymentMock;
    private readonly IPermissionService _permissionMock;

    public MailServicesTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        var dbOptions = new DbContextOptionsBuilder<PostgresDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new PostgresDbContext(dbOptions);
        _db.Database.EnsureCreated();

        _paymentMock = Substitute.For<IPaymentValidationService>();
        _permissionMock = Substitute.For<IPermissionService>();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task SendEnrollmentPromotionEmail_English_SendsCorrectDetails()
    {
        // Arrange
        _db.Settings.Add(new Setting { Name = "ActivityUpdateEmailSender", Value = "sender@example.com" });
        await _db.SaveChangesAsync();

        var mailService = new MockAbstractMailService(_db, _paymentMock, _permissionMock, NullLogger<AbstractMailService>.Instance);

        var member = new Member
        {
            Id = Guid.NewGuid(),
            FirstName = "Alice",
            LastName = "Doe",
            Email = "alice@example.com",
            PreferredLanguage = Language.EN,
            StudentNumber = "s1",
            PhoneNumber = "1",
            Street = "St",
            HouseNumber = "1",
            PostalCode = "1",
            City = "Enschede"
        };
        var activity = new Activity
        {
            Id = 1,
            Name = "Fancy Event",
            DutchDescription = "NL",
            EnglishDescription = "EN",
            DateTimeStart = DateTime.UtcNow,
            DateTimeEnd = DateTime.UtcNow.AddHours(2),
            Location = "Enschede",
            AllowedAudience = TargetAudience.All,
            PaymentDeadline = DateTimeOffset.UtcNow
        };
        var enrollment = new Enrollment
        {
            ActivityId = 1,
            MemberId = member.Id,
            Price = 0,
            RegisteredOn = DateTime.UtcNow,
            IsOnWaitingList = false,
            Member = member,
            Activity = activity
        };

        // Act
        await mailService.SendEnrollmentPromotionEmail(enrollment);

        // Assert
        Assert.NotNull(mailService.LastFrom);
        Assert.Equal("sender@example.com", mailService.LastFrom.Mail);
        Assert.Equal("alice@example.com", mailService.LastTo?[0].Mail);
        Assert.Contains("Your enrollment for Fancy Event is confirmed!", mailService.LastSubject);
        Assert.Contains("Dear Alice", mailService.LastHtmlContent);
    }

    [Fact]
    public async Task SendEnrollmentPromotionEmail_Dutch_SendsCorrectDetails()
    {
        // Arrange
        _db.Settings.Add(new Setting { Name = "ActivityUpdateEmailSender", Value = "sender@example.com" });
        await _db.SaveChangesAsync();

        var mailService = new MockAbstractMailService(_db, _paymentMock, _permissionMock, NullLogger<AbstractMailService>.Instance);

        var member = new Member
        {
            Id = Guid.NewGuid(),
            FirstName = "Alice",
            LastName = "Doe",
            Email = "alice@example.com",
            PreferredLanguage = Language.NL,
            StudentNumber = "s1",
            PhoneNumber = "1",
            Street = "St",
            HouseNumber = "1",
            PostalCode = "1",
            City = "Enschede"
        };
        var activity = new Activity
        {
            Id = 1,
            Name = "Fancy Event",
            DutchDescription = "NL",
            EnglishDescription = "EN",
            DateTimeStart = DateTime.UtcNow,
            DateTimeEnd = DateTime.UtcNow.AddHours(2),
            Location = "Enschede",
            AllowedAudience = TargetAudience.All,
            PaymentDeadline = DateTimeOffset.UtcNow
        };
        var enrollment = new Enrollment
        {
            ActivityId = 1,
            MemberId = member.Id,
            Price = 0,
            RegisteredOn = DateTime.UtcNow,
            IsOnWaitingList = false,
            Member = member,
            Activity = activity
        };

        // Act
        await mailService.SendEnrollmentPromotionEmail(enrollment);

        // Assert
        Assert.NotNull(mailService.LastFrom);
        Assert.Equal("sender@example.com", mailService.LastFrom.Mail);
        Assert.Equal("alice@example.com", mailService.LastTo?[0].Mail);
        Assert.Contains("Je inschrijving voor Fancy Event is bevestigd!", mailService.LastSubject);
        Assert.Contains("Beste Alice", mailService.LastHtmlContent);
    }

    [Fact]
    public async Task SendEnrollmentPromotionEmail_SenderNotConfigured_SkipsSend()
    {
        // Arrange - no "ActivityUpdateEmailSender" setting
        var mailService = new MockAbstractMailService(_db, _paymentMock, _permissionMock, NullLogger<AbstractMailService>.Instance);

        var member = new Member
        {
            Id = Guid.NewGuid(),
            FirstName = "Alice",
            LastName = "Doe",
            Email = "alice@example.com",
            PreferredLanguage = Language.EN,
            StudentNumber = "s1",
            PhoneNumber = "1",
            Street = "St",
            HouseNumber = "1",
            PostalCode = "1",
            City = "Enschede"
        };
        var activity = new Activity
        {
            Id = 1,
            Name = "Fancy Event",
            DutchDescription = "NL",
            EnglishDescription = "EN",
            DateTimeStart = DateTime.UtcNow,
            DateTimeEnd = DateTime.UtcNow.AddHours(2),
            Location = "Enschede",
            AllowedAudience = TargetAudience.All,
            PaymentDeadline = DateTimeOffset.UtcNow
        };
        var enrollment = new Enrollment
        {
            ActivityId = 1,
            MemberId = member.Id,
            Price = 0,
            RegisteredOn = DateTime.UtcNow,
            IsOnWaitingList = false,
            Member = member,
            Activity = activity
        };

        // Act
        await mailService.SendEnrollmentPromotionEmail(enrollment);

        // Assert
        Assert.Null(mailService.LastTo);
    }

    [Fact]
    public async Task SendEnrollmentPromotionEmail_UnsupportedLanguage_ThrowsInvalidOperationException()
    {
        // Arrange
        _db.Settings.Add(new Setting { Name = "ActivityUpdateEmailSender", Value = "sender@example.com" });
        await _db.SaveChangesAsync();

        var mailService = new MockAbstractMailService(_db, _paymentMock, _permissionMock, NullLogger<AbstractMailService>.Instance);

        var member = new Member
        {
            Id = Guid.NewGuid(),
            FirstName = "Alice",
            LastName = "Doe",
            Email = "alice@example.com",
            PreferredLanguage = (Language)99,
            StudentNumber = "s1",
            PhoneNumber = "1",
            Street = "St",
            HouseNumber = "1",
            PostalCode = "1",
            City = "Enschede"
        };
        var activity = new Activity
        {
            Id = 1,
            Name = "Fancy Event",
            DutchDescription = "NL",
            EnglishDescription = "EN",
            DateTimeStart = DateTime.UtcNow,
            DateTimeEnd = DateTime.UtcNow.AddHours(2),
            Location = "Enschede",
            AllowedAudience = TargetAudience.All,
            PaymentDeadline = DateTimeOffset.UtcNow
        };
        var enrollment = new Enrollment
        {
            ActivityId = 1,
            MemberId = member.Id,
            Price = 0,
            RegisteredOn = DateTime.UtcNow,
            IsOnWaitingList = false,
            Member = member,
            Activity = activity
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => mailService.SendEnrollmentPromotionEmail(enrollment));
    }

    [Fact]
    public async Task SendOutstandingPaymentMails_Success_SendsEmails()
    {
        // Arrange
        _db.Settings.Add(new Setting { Name = "FinancialEmailSender", Value = "finance@example.com" });
        await _db.SaveChangesAsync();

        var mailService = new MockAbstractMailService(_db, _paymentMock, _permissionMock, NullLogger<AbstractMailService>.Instance);

        var member = new Member
        {
            Id = Guid.NewGuid(),
            FirstName = "Alice",
            LastName = "Doe",
            Email = "alice@example.com",
            PreferredLanguage = Language.EN,
            StudentNumber = "s1",
            PhoneNumber = "1",
            Street = "St",
            HouseNumber = "1",
            PostalCode = "1",
            City = "Enschede"
        };
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        var activity = new Activity
        {
            Id = 1,
            Name = "Fancy Event",
            DutchDescription = "NL",
            EnglishDescription = "EN",
            DateTimeStart = DateTime.UtcNow,
            DateTimeEnd = DateTime.UtcNow.AddHours(2),
            Location = "Enschede",
            AllowedAudience = TargetAudience.All,
            PaymentDeadline = DateTimeOffset.UtcNow
        };
        var enrollment = new Enrollment
        {
            ActivityId = 1,
            MemberId = member.Id,
            Price = 10,
            RegisteredOn = DateTime.UtcNow,
            IsOnWaitingList = false,
            Member = member,
            Activity = activity
        };

        var balances = new List<EnrollmentBalance>
        {
            new EnrollmentBalance { Enrollment = enrollment, Balance = 10 }
        };
        _paymentMock.GetAllUnpaidEnrollments().Returns(balances);

        // Act
        await mailService.SendOutstandingPaymentMails();

        // Assert
        Assert.NotNull(mailService.LastFrom);
        Assert.Equal("finance@example.com", mailService.LastFrom.Mail);
        Assert.Equal("alice@example.com", mailService.LastTo?[0].Mail);
        Assert.Equal("Outstanding payments for activities", mailService.LastSubject);
        Assert.Contains("Dear Alice", mailService.LastHtmlContent);
        Assert.Contains("Fancy Event: €10", mailService.LastHtmlContent);

        var persistedMember = await _db.Members.AsNoTracking().FirstAsync(m => m.Id == member.Id);
        Assert.NotNull(persistedMember.OutstandingPaymentMailSentAt);
    }

    [Fact]
    public async Task SendOutstandingPaymentMails_RetryAfterRecentSend_DoesNotResendToAlreadyMailedMember()
    {
        // Arrange - mirrors a Hangfire retry re-running the whole job shortly after a prior attempt
        // already mailed this member; they should not be mailed a second time.
        _db.Settings.Add(new Setting { Name = "FinancialEmailSender", Value = "finance@example.com" });
        await _db.SaveChangesAsync();

        var mailService = new MockAbstractMailService(_db, _paymentMock, _permissionMock, NullLogger<AbstractMailService>.Instance);

        var member = new Member
        {
            Id = Guid.NewGuid(),
            FirstName = "Alice",
            LastName = "Doe",
            Email = "alice@example.com",
            PreferredLanguage = Language.EN,
            StudentNumber = "s1",
            PhoneNumber = "1",
            Street = "St",
            HouseNumber = "1",
            PostalCode = "1",
            City = "Enschede",
            OutstandingPaymentMailSentAt = DateTimeOffset.Now.AddHours(-1)
        };
        var activity = new Activity
        {
            Id = 1,
            Name = "Fancy Event",
            DutchDescription = "NL",
            EnglishDescription = "EN",
            DateTimeStart = DateTime.UtcNow,
            DateTimeEnd = DateTime.UtcNow.AddHours(2),
            Location = "Enschede",
            AllowedAudience = TargetAudience.All,
            PaymentDeadline = DateTimeOffset.UtcNow
        };
        var enrollment = new Enrollment
        {
            ActivityId = 1,
            MemberId = member.Id,
            Price = 10,
            RegisteredOn = DateTime.UtcNow,
            IsOnWaitingList = false,
            Member = member,
            Activity = activity
        };

        var balances = new List<EnrollmentBalance>
        {
            new EnrollmentBalance { Enrollment = enrollment, Balance = 10 }
        };
        _paymentMock.GetAllUnpaidEnrollments().Returns(balances);

        // Act
        await mailService.SendOutstandingPaymentMails();

        // Assert
        Assert.Null(mailService.LastTo);
    }

    [Fact]
    public async Task SendOutstandingPaymentMails_PreviousSendOlderThan24Hours_ResendsMail()
    {
        // Arrange - a member mailed more than 24 hours ago (e.g. last week's run) should still be
        // eligible again, so the idempotency guard must not skip everyone forever.
        _db.Settings.Add(new Setting { Name = "FinancialEmailSender", Value = "finance@example.com" });
        await _db.SaveChangesAsync();

        var mailService = new MockAbstractMailService(_db, _paymentMock, _permissionMock, NullLogger<AbstractMailService>.Instance);

        var member = new Member
        {
            Id = Guid.NewGuid(),
            FirstName = "Alice",
            LastName = "Doe",
            Email = "alice@example.com",
            PreferredLanguage = Language.EN,
            StudentNumber = "s1",
            PhoneNumber = "1",
            Street = "St",
            HouseNumber = "1",
            PostalCode = "1",
            City = "Enschede",
            OutstandingPaymentMailSentAt = DateTimeOffset.Now.AddHours(-25)
        };
        var activity = new Activity
        {
            Id = 1,
            Name = "Fancy Event",
            DutchDescription = "NL",
            EnglishDescription = "EN",
            DateTimeStart = DateTime.UtcNow,
            DateTimeEnd = DateTime.UtcNow.AddHours(2),
            Location = "Enschede",
            AllowedAudience = TargetAudience.All,
            PaymentDeadline = DateTimeOffset.UtcNow
        };
        var enrollment = new Enrollment
        {
            ActivityId = 1,
            MemberId = member.Id,
            Price = 10,
            RegisteredOn = DateTime.UtcNow,
            IsOnWaitingList = false,
            Member = member,
            Activity = activity
        };

        var balances = new List<EnrollmentBalance>
        {
            new EnrollmentBalance { Enrollment = enrollment, Balance = 10 }
        };
        _paymentMock.GetAllUnpaidEnrollments().Returns(balances);

        // Act
        await mailService.SendOutstandingPaymentMails();

        // Assert
        Assert.Equal("alice@example.com", mailService.LastTo?[0].Mail);
    }

    [Fact]
    public async Task SendOutstandingPaymentMails_OneMemberFails_StillMailsRemainingMembers()
    {
        // Arrange - a bad recipient (e.g. a rejected address) must not abort the rest of the batch;
        // everyone else on the list should still be mailed.
        _db.Settings.Add(new Setting { Name = "FinancialEmailSender", Value = "finance@example.com" });
        await _db.SaveChangesAsync();

        var mailService = new MockAbstractMailService(_db, _paymentMock, _permissionMock, NullLogger<AbstractMailService>.Instance)
        {
            FailFor = email => email == "alice@example.com"
        };

        var failingMember = new Member
        {
            Id = Guid.NewGuid(),
            FirstName = "Alice",
            LastName = "Doe",
            Email = "alice@example.com",
            PreferredLanguage = Language.EN,
            StudentNumber = "s1",
            PhoneNumber = "1",
            Street = "St",
            HouseNumber = "1",
            PostalCode = "1",
            City = "Enschede"
        };
        var okMember = new Member
        {
            Id = Guid.NewGuid(),
            FirstName = "Bram",
            LastName = "Doe",
            Email = "bram@example.com",
            PreferredLanguage = Language.EN,
            StudentNumber = "s2",
            PhoneNumber = "2",
            Street = "St",
            HouseNumber = "2",
            PostalCode = "1",
            City = "Enschede"
        };
        var activity = new Activity
        {
            Id = 1,
            Name = "Fancy Event",
            DutchDescription = "NL",
            EnglishDescription = "EN",
            DateTimeStart = DateTime.UtcNow,
            DateTimeEnd = DateTime.UtcNow.AddHours(2),
            Location = "Enschede",
            AllowedAudience = TargetAudience.All,
            PaymentDeadline = DateTimeOffset.UtcNow
        };
        var failingEnrollment = new Enrollment
        {
            ActivityId = 1,
            MemberId = failingMember.Id,
            Price = 10,
            RegisteredOn = DateTime.UtcNow,
            IsOnWaitingList = false,
            Member = failingMember,
            Activity = activity
        };
        var okEnrollment = new Enrollment
        {
            ActivityId = 1,
            MemberId = okMember.Id,
            Price = 10,
            RegisteredOn = DateTime.UtcNow,
            IsOnWaitingList = false,
            Member = okMember,
            Activity = activity
        };

        var balances = new List<EnrollmentBalance>
        {
            new EnrollmentBalance { Enrollment = failingEnrollment, Balance = 10 },
            new EnrollmentBalance { Enrollment = okEnrollment, Balance = 10 }
        };
        _paymentMock.GetAllUnpaidEnrollments().Returns(balances);

        // Act & Assert
        var aggregate = await Assert.ThrowsAsync<AggregateException>(() => mailService.SendOutstandingPaymentMails());
        Assert.Single(aggregate.InnerExceptions);
        Assert.Contains("bram@example.com", mailService.SentToEmails);
        Assert.Contains("alice@example.com", mailService.SentToEmails);
    }

    [Fact]
    public async Task SendOutstandingPaymentMails_SenderNotConfigured_SkipsSend()
    {
        // Arrange - no "FinancialEmailSender" setting
        var mailService = new MockAbstractMailService(_db, _paymentMock, _permissionMock, NullLogger<AbstractMailService>.Instance);
        _paymentMock.GetAllUnpaidEnrollments().Returns(new List<EnrollmentBalance>());

        // Act
        await mailService.SendOutstandingPaymentMails();

        // Assert
        Assert.Null(mailService.LastTo);
    }

    [Fact]
    public async Task SendOutstandingPaymentMails_UnsupportedLanguage_ThrowsInvalidOperationException()
    {
        // Arrange
        _db.Settings.Add(new Setting { Name = "FinancialEmailSender", Value = "finance@example.com" });
        await _db.SaveChangesAsync();

        var mailService = new MockAbstractMailService(_db, _paymentMock, _permissionMock, NullLogger<AbstractMailService>.Instance);

        var member = new Member
        {
            Id = Guid.NewGuid(),
            FirstName = "Alice",
            LastName = "Doe",
            Email = "alice@example.com",
            PreferredLanguage = (Language)99,
            StudentNumber = "s1",
            PhoneNumber = "1",
            Street = "St",
            HouseNumber = "1",
            PostalCode = "1",
            City = "Enschede"
        };
        var activity = new Activity
        {
            Id = 1,
            Name = "Fancy Event",
            DutchDescription = "NL",
            EnglishDescription = "EN",
            DateTimeStart = DateTime.UtcNow,
            DateTimeEnd = DateTime.UtcNow.AddHours(2),
            Location = "Enschede",
            AllowedAudience = TargetAudience.All,
            PaymentDeadline = DateTimeOffset.UtcNow
        };
        var enrollment = new Enrollment
        {
            ActivityId = 1,
            MemberId = member.Id,
            Price = 10,
            RegisteredOn = DateTime.UtcNow,
            IsOnWaitingList = false,
            Member = member,
            Activity = activity
        };

        var balances = new List<EnrollmentBalance>
        {
            new EnrollmentBalance { Enrollment = enrollment, Balance = 10 }
        };
        _paymentMock.GetAllUnpaidEnrollments().Returns(balances);

        // Act & Assert - the batch still throws overall (so Hangfire surfaces/retries the failure), but
        // as an AggregateException, since a single bad recipient must not abort the rest of the batch.
        var aggregate = await Assert.ThrowsAsync<AggregateException>(() => mailService.SendOutstandingPaymentMails());
        Assert.IsType<InvalidOperationException>(Assert.Single(aggregate.InnerExceptions));
    }

    [Fact]
    public async Task SendStudyStatusUpdateMails_Success_SendsEmails()
    {
        // Arrange
        _db.Settings.Add(new Setting { Name = "MainBoardMail", Value = "board@example.com" });

        var member = new Member
        {
            Id = Guid.NewGuid(),
            FirstName = "Bob",
            LastName = "Sponge",
            Email = "bob@example.com",
            PreferredLanguage = Language.NL,
            StudentNumber = "s2",
            PhoneNumber = "2",
            Street = "Ocean",
            HouseNumber = "124",
            PostalCode = "1234",
            City = "Bikini Bottom"
        };

        var study = new Study
        {
            Id = 1,
            Title = "Computer Science",
            NominalDurationYears = 3
        };

        var studyEnrollment = new StudyEnrollment
        {
            Id = 1,
            MemberId = member.Id,
            StudyId = 1,
            EnrollmentDate = DateTime.Now.AddYears(-4), // 4 years ago is greater than nominal 3 years
            Status = StudyStatus.Enrolled,
            Study = study
        };

        member.StudyEnrollments = new List<StudyEnrollment> { studyEnrollment };
        _db.Members.Add(member);
        _db.Studies.Add(study);
        _db.StudyEnrollments.Add(studyEnrollment);
        await _db.SaveChangesAsync();

        var mailService = new MockAbstractMailService(_db, _paymentMock, _permissionMock, NullLogger<AbstractMailService>.Instance);

        // Act
        await mailService.SendStudyStatusUpdateMails();

        // Assert
        Assert.NotNull(mailService.LastFrom);
        Assert.Equal("board@example.com", mailService.LastFrom.Mail);
        Assert.Equal("bob@example.com", mailService.LastTo?[0].Mail);
        Assert.Equal("example.com", mailService.LastTo?[0].Mail.Split('@')[1]);
        Assert.Equal("Controleer lidmaatschap en studievoortgang", mailService.LastSubject);
        Assert.Contains("Beste Bob", mailService.LastHtmlContent);

        var persistedMember = await _db.Members.AsNoTracking().FirstAsync(m => m.Id == member.Id);
        Assert.NotNull(persistedMember.StudyStatusMailSentAt);
    }

    [Fact]
    public async Task SendStudyStatusUpdateMails_RetryAfterRecentSend_DoesNotResendToAlreadyMailedMember()
    {
        // Arrange - mirrors a Hangfire retry re-running the whole job shortly after a prior attempt
        // already mailed this member; they should not be mailed a second time.
        _db.Settings.Add(new Setting { Name = "MainBoardMail", Value = "board@example.com" });

        var member = new Member
        {
            Id = Guid.NewGuid(),
            FirstName = "Bob",
            LastName = "Sponge",
            Email = "bob@example.com",
            PreferredLanguage = Language.NL,
            StudentNumber = "s2",
            PhoneNumber = "2",
            Street = "Ocean",
            HouseNumber = "124",
            PostalCode = "1234",
            City = "Bikini Bottom",
            StudyStatusMailSentAt = DateTimeOffset.Now.AddHours(-1)
        };

        var study = new Study
        {
            Id = 1,
            Title = "Computer Science",
            NominalDurationYears = 3
        };

        var studyEnrollment = new StudyEnrollment
        {
            Id = 1,
            MemberId = member.Id,
            StudyId = 1,
            EnrollmentDate = DateTime.Now.AddYears(-4), // 4 years ago is greater than nominal 3 years
            Status = StudyStatus.Enrolled,
            Study = study
        };

        member.StudyEnrollments = new List<StudyEnrollment> { studyEnrollment };
        _db.Members.Add(member);
        _db.Studies.Add(study);
        _db.StudyEnrollments.Add(studyEnrollment);
        await _db.SaveChangesAsync();

        var mailService = new MockAbstractMailService(_db, _paymentMock, _permissionMock, NullLogger<AbstractMailService>.Instance);

        // Act
        await mailService.SendStudyStatusUpdateMails();

        // Assert
        Assert.Null(mailService.LastTo);
    }

    [Fact]
    public async Task SendStudyStatusUpdateMails_PreviousSendOlderThan24Hours_ResendsMail()
    {
        // Arrange - a member mailed more than 24 hours ago (e.g. last year's run) should still be
        // eligible again, so the idempotency guard must not skip everyone forever.
        _db.Settings.Add(new Setting { Name = "MainBoardMail", Value = "board@example.com" });

        var member = new Member
        {
            Id = Guid.NewGuid(),
            FirstName = "Bob",
            LastName = "Sponge",
            Email = "bob@example.com",
            PreferredLanguage = Language.NL,
            StudentNumber = "s2",
            PhoneNumber = "2",
            Street = "Ocean",
            HouseNumber = "124",
            PostalCode = "1234",
            City = "Bikini Bottom",
            StudyStatusMailSentAt = DateTimeOffset.Now.AddHours(-25)
        };

        var study = new Study
        {
            Id = 1,
            Title = "Computer Science",
            NominalDurationYears = 3
        };

        var studyEnrollment = new StudyEnrollment
        {
            Id = 1,
            MemberId = member.Id,
            StudyId = 1,
            EnrollmentDate = DateTime.Now.AddYears(-4), // 4 years ago is greater than nominal 3 years
            Status = StudyStatus.Enrolled,
            Study = study
        };

        member.StudyEnrollments = new List<StudyEnrollment> { studyEnrollment };
        _db.Members.Add(member);
        _db.Studies.Add(study);
        _db.StudyEnrollments.Add(studyEnrollment);
        await _db.SaveChangesAsync();

        var mailService = new MockAbstractMailService(_db, _paymentMock, _permissionMock, NullLogger<AbstractMailService>.Instance);

        // Act
        await mailService.SendStudyStatusUpdateMails();

        // Assert
        Assert.Equal("bob@example.com", mailService.LastTo?[0].Mail);
    }

    [Fact]
    public async Task SendStudyStatusUpdateMails_OneMemberFails_StillMailsRemainingMembers()
    {
        // Arrange - a bad recipient (e.g. a rejected address) must not abort the rest of the batch;
        // everyone else on the list should still be mailed.
        _db.Settings.Add(new Setting { Name = "MainBoardMail", Value = "board@example.com" });

        var failingMember = new Member
        {
            Id = Guid.NewGuid(),
            FirstName = "Carl",
            LastName = "Winslow",
            Email = "carl@example.com",
            PreferredLanguage = Language.EN,
            StudentNumber = "s3",
            PhoneNumber = "3",
            Street = "Main St",
            HouseNumber = "1",
            PostalCode = "1234",
            City = "Chicago"
        };
        var okMember = new Member
        {
            Id = Guid.NewGuid(),
            FirstName = "Dana",
            LastName = "Scully",
            Email = "dana@example.com",
            PreferredLanguage = Language.EN,
            StudentNumber = "s4",
            PhoneNumber = "4",
            Street = "Main St",
            HouseNumber = "2",
            PostalCode = "1234",
            City = "Chicago"
        };
        _db.Members.Add(failingMember);
        _db.Members.Add(okMember);
        await _db.SaveChangesAsync();

        var mailService = new MockAbstractMailService(_db, _paymentMock, _permissionMock, NullLogger<AbstractMailService>.Instance)
        {
            FailFor = email => email == "carl@example.com"
        };

        // Act & Assert
        var aggregate = await Assert.ThrowsAsync<AggregateException>(() => mailService.SendStudyStatusUpdateMails());
        Assert.Single(aggregate.InnerExceptions);
        Assert.Contains("carl@example.com", mailService.SentToEmails);
        Assert.Contains("dana@example.com", mailService.SentToEmails);

        var persistedOkMember = await _db.Members.AsNoTracking().FirstAsync(m => m.Id == okMember.Id);
        Assert.NotNull(persistedOkMember.StudyStatusMailSentAt);
        var persistedFailingMember = await _db.Members.AsNoTracking().FirstAsync(m => m.Id == failingMember.Id);
        Assert.Null(persistedFailingMember.StudyStatusMailSentAt);
    }

    [Fact]
    public async Task SendStudyStatusUpdateMails_MemberWithNoStudyHistory_SendsNeverStudiedMail()
    {
        // Arrange
        _db.Settings.Add(new Setting { Name = "MainBoardMail", Value = "board@example.com" });

        var member = new Member
        {
            Id = Guid.NewGuid(),
            FirstName = "Carl",
            LastName = "Wheezer",
            Email = "carl@example.com",
            PreferredLanguage = Language.EN,
            Begunstiger = false,
            StudentNumber = "s3",
            PhoneNumber = "3",
            Street = "Retroville",
            HouseNumber = "1",
            PostalCode = "1234",
            City = "Retroville"
        };
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        var mailService = new MockAbstractMailService(_db, _paymentMock, _permissionMock, NullLogger<AbstractMailService>.Instance);

        // Act
        await mailService.SendStudyStatusUpdateMails();

        // Assert
        Assert.Equal("carl@example.com", mailService.LastTo?[0].Mail);
        Assert.Equal("Check your membership", mailService.LastSubject);
        Assert.Contains("Dear Carl", mailService.LastHtmlContent);
        Assert.Contains("update-account-status", mailService.LastHtmlContent);
    }

    [Fact]
    public async Task SendStudyStatusUpdateMails_NoStudyHistory_UnsupportedLanguage_ThrowsInvalidOperationException()
    {
        // Arrange
        _db.Settings.Add(new Setting { Name = "MainBoardMail", Value = "board@example.com" });

        var member = new Member
        {
            Id = Guid.NewGuid(),
            FirstName = "Carl",
            LastName = "Wheezer",
            Email = "carl@example.com",
            PreferredLanguage = (Language)99,
            Begunstiger = false,
            StudentNumber = "s3",
            PhoneNumber = "3",
            Street = "Retroville",
            HouseNumber = "1",
            PostalCode = "1234",
            City = "Retroville"
        };
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        var mailService = new MockAbstractMailService(_db, _paymentMock, _permissionMock, NullLogger<AbstractMailService>.Instance);

        // Act & Assert - the batch still throws overall (so Hangfire surfaces/retries the failure), but
        // as an AggregateException, since a single bad recipient must not abort the rest of the batch.
        var aggregate = await Assert.ThrowsAsync<AggregateException>(() => mailService.SendStudyStatusUpdateMails());
        Assert.IsType<InvalidOperationException>(Assert.Single(aggregate.InnerExceptions));
    }

    [Fact]
    public async Task SendStudyStatusUpdateMails_OutstandingDuration_UnsupportedLanguage_ThrowsInvalidOperationException()
    {
        // Arrange
        _db.Settings.Add(new Setting { Name = "MainBoardMail", Value = "board@example.com" });

        var member = new Member
        {
            Id = Guid.NewGuid(),
            FirstName = "Erin",
            LastName = "Hanson",
            Email = "erin@example.com",
            PreferredLanguage = (Language)99,
            StudentNumber = "s5",
            PhoneNumber = "5",
            Street = "Main St",
            HouseNumber = "1",
            PostalCode = "1234",
            City = "Enschede"
        };

        var study = new Study
        {
            Id = 1,
            Title = "Computer Science",
            NominalDurationYears = 3
        };

        var studyEnrollment = new StudyEnrollment
        {
            Id = 1,
            MemberId = member.Id,
            StudyId = 1,
            EnrollmentDate = DateTime.Now.AddYears(-4),
            Status = StudyStatus.Enrolled,
            Study = study
        };

        member.StudyEnrollments = new List<StudyEnrollment> { studyEnrollment };
        _db.Members.Add(member);
        _db.Studies.Add(study);
        _db.StudyEnrollments.Add(studyEnrollment);
        await _db.SaveChangesAsync();

        var mailService = new MockAbstractMailService(_db, _paymentMock, _permissionMock, NullLogger<AbstractMailService>.Instance);

        // Act & Assert - the batch still throws overall (so Hangfire surfaces/retries the failure), but
        // as an AggregateException, since a single bad recipient must not abort the rest of the batch.
        var aggregate = await Assert.ThrowsAsync<AggregateException>(() => mailService.SendStudyStatusUpdateMails());
        Assert.IsType<InvalidOperationException>(Assert.Single(aggregate.InnerExceptions));
    }

    [Fact]
    public async Task SendStudyStatusUpdateMails_BegunstigerWithNoStudyHistory_DoesNotSendMail()
    {
        // Arrange
        _db.Settings.Add(new Setting { Name = "MainBoardMail", Value = "board@example.com" });

        var member = new Member
        {
            Id = Guid.NewGuid(),
            FirstName = "Donna",
            LastName = "Sinclair",
            Email = "donna@example.com",
            PreferredLanguage = Language.EN,
            Begunstiger = true,
            StudentNumber = "s4",
            PhoneNumber = "4",
            Street = "Main St",
            HouseNumber = "1",
            PostalCode = "1234",
            City = "Enschede"
        };
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        var mailService = new MockAbstractMailService(_db, _paymentMock, _permissionMock, NullLogger<AbstractMailService>.Instance);

        // Act
        await mailService.SendStudyStatusUpdateMails();

        // Assert
        Assert.Null(mailService.LastTo);
    }

    [Fact]
    public async Task SendStudyStatusUpdateMails_NoSenderConfigured_SkipsSend()
    {
        // Arrange - no "MainBoardMail" setting
        var member = new Member
        {
            Id = Guid.NewGuid(),
            FirstName = "Carl",
            LastName = "Wheezer",
            Email = "carl@example.com",
            PreferredLanguage = Language.EN,
            Begunstiger = false,
            StudentNumber = "s3",
            PhoneNumber = "3",
            Street = "Retroville",
            HouseNumber = "1",
            PostalCode = "1234",
            City = "Retroville"
        };
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        var mailService = new MockAbstractMailService(_db, _paymentMock, _permissionMock, NullLogger<AbstractMailService>.Instance);

        // Act
        await mailService.SendStudyStatusUpdateMails();

        // Assert
        Assert.Null(mailService.LastTo);
    }

    [Fact]
    public async Task SendStudyStatusUpdateMails_MemberWithoutActiveStudy_SendsCheckProgressMail()
    {
        // Arrange
        _db.Settings.Add(new Setting { Name = "MainBoardMail", Value = "board@example.com" });

        var member = new Member
        {
            Id = Guid.NewGuid(),
            FirstName = "Erin",
            LastName = "Hanson",
            Email = "erin@example.com",
            PreferredLanguage = Language.EN,
            StudentNumber = "s5",
            PhoneNumber = "5",
            Street = "Main St",
            HouseNumber = "1",
            PostalCode = "1234",
            City = "Enschede"
        };

        var study = new Study
        {
            Id = 1,
            Title = "Computer Science",
            NominalDurationYears = 3
        };

        // Dropped out recently, so this doesn't fall into the "outstanding duration" bucket either
        var studyEnrollment = new StudyEnrollment
        {
            Id = 1,
            MemberId = member.Id,
            StudyId = 1,
            EnrollmentDate = DateTime.Now.AddMonths(-1),
            Status = StudyStatus.DroppedOut,
            Study = study
        };

        member.StudyEnrollments = new List<StudyEnrollment> { studyEnrollment };
        _db.Members.Add(member);
        _db.Studies.Add(study);
        _db.StudyEnrollments.Add(studyEnrollment);
        await _db.SaveChangesAsync();

        var mailService = new MockAbstractMailService(_db, _paymentMock, _permissionMock, NullLogger<AbstractMailService>.Instance);

        // Act
        await mailService.SendStudyStatusUpdateMails();

        // Assert
        Assert.Equal("erin@example.com", mailService.LastTo?[0].Mail);
        Assert.Equal("Check membership and study progress", mailService.LastSubject);
        Assert.Contains("Dear Erin", mailService.LastHtmlContent);
    }

    [Fact]
    public void StripHtml_StripsHtmlCorrectly()
    {
        // Arrange
        var mailService = new MockAbstractMailService(_db, _paymentMock, _permissionMock, NullLogger<AbstractMailService>.Instance);
        var html = "<h1>Title</h1><p>Body text.<br/>Another line.</p>";

        // Act
        var result = mailService.PublicStripHtml(html);

        // Assert
        Assert.Equal("Title\r\nBody text.\r\nAnother line.", result);
    }

    [Fact]
    public async Task SMTPMailService_SendEmailCoreAsync_ThrowsConnectException_WhenPortClosed()
    {
        // Arrange
        Environment.SetEnvironmentVariable("SMTP_HOST", "127.0.0.1");
        Environment.SetEnvironmentVariable("SMTP_PORT", "12345"); // closed port
        Environment.SetEnvironmentVariable("SMTP_STARTTLS", "false");
        Environment.SetEnvironmentVariable("SMTP_USER", "");
        Environment.SetEnvironmentVariable("SMTP_PASS", "");

        var smtpService = new SMTPMailService(
            _db,
            _paymentMock,
            _permissionMock,
            NullLogger<SMTPMailService>.Instance,
            NullLogger<AbstractMailService>.Instance
        );

        var from = new MailRecipient { Mail = "from@example.com", Name = "From" };
        var to = new[] { new MailRecipient { Mail = "to@example.com", Name = "To" } };

        // Act & Assert
        // SMTP connect should fail since port 12345 on localhost is closed
        await Assert.ThrowsAnyAsync<Exception>(() =>
            smtpService.SendEmailAsync(new PostMailDTO
            {
                Recipients = to,
                Subject = "Sub",
                HtmlContent = "Content"
            }, Guid.NewGuid(), CancellationToken.None)); // will throw because permission check throws first if user not in db
    }

    [Fact]
    public async Task MailgunService_SendEmailCoreAsync_ThrowsException_WhenDomainInvalid()
    {
        // Arrange
        Environment.SetEnvironmentVariable("MAILGUN_TOKEN", "mock_token");
        Environment.SetEnvironmentVariable("MAILGUN_PUBLIC_KEY", "mock_key");
        Environment.SetEnvironmentVariable("MAILGUN_API_BASE_URL", "http://127.0.0.1:54321");

        var httpClientFactoryMock = Substitute.For<IHttpClientFactory>();
        httpClientFactoryMock.CreateClient(Arg.Any<string>()).Returns(_ => new HttpClient());

        var mailgunService = new MailgunService(
            _db,
            _paymentMock,
            _permissionMock,
            NullLogger<MailgunService>.Instance,
            NullLogger<AbstractMailService>.Instance,
            httpClientFactoryMock
        );

        var from = new MailRecipient { Mail = "from@example.com", Name = "From" };
        var to = new[] { new MailRecipient { Mail = "to@example.com", Name = "To" } };

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(() =>
            mailgunService.SendEmailAsync(new PostMailDTO
            {
                Recipients = to,
                Subject = "Sub",
                HtmlContent = "Content"
            }, Guid.NewGuid(), CancellationToken.None));
    }
}
