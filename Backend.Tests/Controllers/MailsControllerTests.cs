using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Backend.Controllers;
using Backend.Controllers.DTOs;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models;
using Backend.Models.Domain;
using Backend.Utils.DateTime;
using Backend.Services.MailServices;
using Backend.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Backend.Tests.Controllers;

public class MailsTestPostgresDbContext : PostgresDbContext
{
    public MailsTestPostgresDbContext(DbContextOptions<PostgresDbContext> options) : base(options)
    {
    }
}

public class TestMailService : AbstractMailService
{
    public bool ShouldThrow { get; set; }

    public TestMailService(
        PostgresDbContext db,
        IPaymentValidationService paymentValidationService,
        IPermissionService permissionService,
        ILogger<AbstractMailService> logger) : base(db, paymentValidationService, permissionService, logger)
    {
    }

    protected override Task SendEmailCoreAsync(MailRecipient from, MailRecipient[] to, string subject, string htmlContent, CancellationToken ct)
    {
        if (ShouldThrow)
            throw new Exception("SMTP connection failed");
        return Task.CompletedTask;
    }
}

public class MailsControllerTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly PostgresDbContext _db;
    private readonly IPermissionService _permissionServiceMock;
    private readonly TestMailService _mailService;
    private readonly MailsController _controller;
    private readonly Guid _userId;

    public MailsControllerTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        var dbOptions = new DbContextOptionsBuilder<PostgresDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new MailsTestPostgresDbContext(dbOptions);
        _db.Database.EnsureCreated();

        // Add standard settings before creating _mailService so that constructor parses them
        _db.Settings.Add(new Setting { Name = "BoardGroupId", Value = "1" });
        _db.Settings.Add(new Setting { Name = "CandidateBoardGroupId", Value = "2" });
        _db.Settings.Add(new Setting { Name = "ROLEMAILMAP_3", Value = "board@example.com" });
        _db.SaveChanges();

        _permissionServiceMock = Substitute.For<IPermissionService>();

        _mailService = new TestMailService(
            _db,
            Substitute.For<IPaymentValidationService>(),
            _permissionServiceMock,
            NullLogger<AbstractMailService>.Instance
        );

        _controller = new MailsController(_mailService);
        _userId = Guid.NewGuid();

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("UserId", _userId.ToString())
        }, "mock"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    private async Task SetupDatabaseForSending()
    {

        // 2. Member
        var member = new Member
        {
            Id = _userId,
            FirstName = "Board",
            LastName = "Member",
            Email = "board@example.com",
            StudentNumber = "s1234567",
            PhoneNumber = "+31612345678",
            Street = "Main Street",
            HouseNumber = "12",
            PostalCode = "7500AA",
            City = "Enschede"
        };
        _db.Members.Add(member);

        // 3. Role and RoleAlias
        var role = new Role { Id = 3, Name = "Secretary" };
        _db.Roles.Add(role);

        var alias = new RoleAlias { Id = 9, RoleId = 3, Name = "Sec" };
        _db.RoleAliases.Add(alias);

        // 4. Group and GroupMembership
        var group = new Group { Id = 1, Name = "Board", Type = GroupType.Committee, Active = true };
        _db.Groups.Add(group);

        var currentYear = YearUtils.GetCurrentFinancialYear();
        var membership = new GroupMembership
        {
            MemberId = _userId,
            GroupId = 1,
            MembershipYear = currentYear,
            RoleAliasId = 9
        };
        _db.GroupMemberships.Add(membership);

        await _db.SaveChangesAsync();
    }

    [Fact]
    public async Task PostNormalMail_Success_ReturnsOk()
    {
        // Arrange
        await SetupDatabaseForSending();
        var dto = new PostMailDTO
        {
            Recipients = new[] { new MailRecipient { Mail = "test@example.com", Name = "Test User" } },
            Subject = "Test Subject",
            HtmlContent = "<h1>Test Body</h1>"
        };

        // Act
        var result = await _controller.PostNormalMail(dto, CancellationToken.None);

        // Assert
        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task PostNormalMail_Forbidden_ReturnsForbid()
    {
        // Arrange
        var dto = new PostMailDTO
        {
            Recipients = new[] { new MailRecipient { Mail = "a@b.com", Name = "A" } },
            Subject = "Sub",
            HtmlContent = "Content"
        };
        
        _permissionServiceMock.When(p => p.EnsureBoardOrCandidateBoardMember(_userId))
            .Do(x => throw new UnauthorizedAccessException());

        // Act
        var result = await _controller.PostNormalMail(dto, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task PostNormalMail_Error_ReturnsBadRequest()
    {
        // Arrange
        await SetupDatabaseForSending();
        var dto = new PostMailDTO
        {
            Recipients = new[] { new MailRecipient { Mail = "a@b.com", Name = "A" } },
            Subject = "Sub",
            HtmlContent = "Content"
        };
        _mailService.ShouldThrow = true;

        // Act
        var result = await _controller.PostNormalMail(dto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errorDto = Assert.IsType<ErrorResponseDto>(badRequestResult.Value);
        Assert.Equal("SMTP connection failed", errorDto.Message);
    }

    [Fact]
    public async Task PostActivityMail_Success_ReturnsOk()
    {
        // Arrange
        await SetupDatabaseForSending();

        var activity = new Activity
        {
            Id = 123,
            Name = "Test Activity",
            Price = 0,
            DutchDescription = "NL",
            EnglishDescription = "EN",
            DateTimeStart = DateTime.UtcNow.AddDays(1),
            DateTimeEnd = DateTime.UtcNow.AddDays(2),
            Location = "Enschede",
            AllowedAudience = TargetAudience.All,
            PaymentDeadline = DateTimeOffset.UtcNow.AddDays(5),
            Enrollments = new List<Enrollment>(),
            SpecificationQuestions = new List<SpecificationQuestion>()
        };
        
        var participant = new Member
        {
            Id = Guid.NewGuid(),
            FirstName = "Part",
            LastName = "Icipant",
            Email = "part@example.com",
            StudentNumber = "s7654321",
            PhoneNumber = "+31687654321",
            Street = "Other Street",
            HouseNumber = "34",
            PostalCode = "7500BB",
            City = "Enschede"
        };

        var enrollment = new Enrollment
        {
            ActivityId = 123,
            MemberId = participant.Id,
            Price = 0,
            RegisteredOn = DateTime.UtcNow,
            IsOnWaitingList = false,
            Member = participant
        };
        activity.Enrollments.Add(enrollment);

        _db.Activities.Add(activity);
        _db.Members.Add(participant);
        await _db.SaveChangesAsync();

        var dto = new PostActivityMailDTO
        {
            Subject = "Activity Update",
            HtmlContent = "Update details",
            ActivityId = 123
        };

        // Act
        var result = await _controller.PostActivityMail(dto, CancellationToken.None);

        // Assert
        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task PostActivityMail_Forbidden_ReturnsForbid()
    {
        // Arrange
        await SetupDatabaseForSending();

        var activity = new Activity
        {
            Id = 1,
            Name = "Test Activity",
            Price = 0,
            DutchDescription = "NL",
            EnglishDescription = "EN",
            DateTimeStart = DateTime.UtcNow.AddDays(1),
            DateTimeEnd = DateTime.UtcNow.AddDays(2),
            Location = "Enschede",
            AllowedAudience = TargetAudience.All,
            PaymentDeadline = DateTimeOffset.UtcNow.AddDays(5),
            Enrollments = new List<Enrollment>(),
            SpecificationQuestions = new List<SpecificationQuestion>()
        };
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync();

        var dto = new PostActivityMailDTO
        {
            Subject = "Sub",
            HtmlContent = "Body",
            ActivityId = 1
        };
        
        _permissionServiceMock.When(p => p.EnsureBoardOrCandidateBoardMember(_userId))
            .Do(x => throw new UnauthorizedAccessException());

        // Act
        var result = await _controller.PostActivityMail(dto, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task PostActivityMail_Error_ReturnsBadRequest()
    {
        // Arrange
        var dto = new PostActivityMailDTO
        {
            Subject = "Sub",
            HtmlContent = "Body",
            ActivityId = 999
        };

        // Act
        var result = await _controller.PostActivityMail(dto, CancellationToken.None);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errorDto = Assert.IsType<ErrorResponseDto>(badRequestResult.Value);
        Assert.Equal("Activity not found", errorDto.Message);
    }
}
