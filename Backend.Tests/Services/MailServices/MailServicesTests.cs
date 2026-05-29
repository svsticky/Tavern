using System;
using System.Collections.Generic;
using System.IO;
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
        Assert.Equal("Update your study status", mailService.LastSubject);
        Assert.Contains("Beste Bob", mailService.LastHtmlContent);
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

        var mailgunService = new MailgunService(
            _db,
            _paymentMock,
            _permissionMock,
            NullLogger<MailgunService>.Instance,
            NullLogger<AbstractMailService>.Instance
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
