using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Backend.Database;
using Backend.Models.Domain;
using Backend.Services.PaymentServices;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Mollie.Api.Client.Abstract;
using Mollie.Api.Models;
using Mollie.Api.Models.Payment.Request;
using Mollie.Api.Models.Payment.Response;
using Mollie.Api.Models.Url;
using NSubstitute;
using Xunit;

namespace Backend.Tests.Services.PaymentServices;

[Collection("NonParallelEnvironment")]
public class PaymentServicesTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly PostgresDbContext _db;
    private readonly IPaymentClient _mollieClientMock;
    private readonly MollieService _service;
    private readonly string? _originalAccountingEnabled;

    public PaymentServicesTests()
    {
        _originalAccountingEnabled = Environment.GetEnvironmentVariable("ACCOUNTING_ENABLED");
        Environment.SetEnvironmentVariable("ACCOUNTING_ENABLED", "true");

        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        var dbOptions = new DbContextOptionsBuilder<PostgresDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new PostgresDbContext(dbOptions);
        _db.Database.EnsureCreated();

        _mollieClientMock = Substitute.For<IPaymentClient>();
        _service = new MollieService(_db, NullLogger<MollieService>.Instance, () => _mollieClientMock);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
        Environment.SetEnvironmentVariable("ACCOUNTING_ENABLED", _originalAccountingEnabled);
        Environment.SetEnvironmentVariable("ACCOUNTING_SERVICE", null);
    }

    private PaymentResponse CreateMockPaymentResponse(string id, string status, DateTimeOffset? paidAt = null)
    {
        return new PaymentResponse
        {
            Id = id,
            Status = status,
            PaidAt = paidAt?.UtcDateTime,
            Resource = "payment",
            Mode = Mode.Test,
            CreatedAt = DateTime.UtcNow,
            Amount = new Amount(Currency.EUR, 7.50m),
            ProfileId = "pfl_123",
            SequenceType = "oneoff",
            Links = new PaymentResponseLinks
            {
                Self = new UrlObjectLink<PaymentResponse> { Href = "http://self", Type = "application/hal+json" },
                Dashboard = new UrlObjectLink<PaymentResponse> { Href = "http://dashboard", Type = "application/hal+json" },
                Documentation = new UrlObjectLink<PaymentResponse> { Href = "http://docs", Type = "application/hal+json" }
            }
        };
    }

    [Theory]
    [InlineData("paid", PaymentStatus.Paid)]
    [InlineData("open", PaymentStatus.Pending)]
    [InlineData("pending", PaymentStatus.Pending)]
    [InlineData("canceled", PaymentStatus.Failed)]
    [InlineData("failed", PaymentStatus.Failed)]
    [InlineData("expired", PaymentStatus.Failed)]
    public async Task GetPaymentAsync_MapsStatusCorrectly(string mollieStatus, PaymentStatus expectedStatus)
    {
        // Arrange
        var mockResponse = CreateMockPaymentResponse("tr_123", mollieStatus, DateTime.UtcNow);
        _mollieClientMock.GetPaymentAsync("tr_123").Returns(mockResponse);

        // Act
        var result = await _service.GetPaymentAsync("tr_123");

        // Assert
        Assert.Equal("tr_123", result.PaymentId);
        Assert.Equal(expectedStatus, result.Status);
    }

    [Fact]
    public async Task GetPaymentAsync_UnknownStatus_ThrowsException()
    {
        // Arrange
        var mockResponse = CreateMockPaymentResponse("tr_123", "unknown_status");
        _mollieClientMock.GetPaymentAsync("tr_123").Returns(mockResponse);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _service.GetPaymentAsync("tr_123"));
    }

    [Fact]
    public async Task CancelPaymentAsync_CallsMollieClient()
    {
        // Act
        await _service.CancelPaymentAsync("tr_123");

        // Assert
        await _mollieClientMock.Received(1).CancelPaymentAsync("tr_123");
    }

    [Fact]
    public async Task CreatePaymentAsync_Success_ReturnsCheckoutResponse()
    {
        // Arrange
        var response = CreateMockPaymentResponse("tr_abc", "open");
        response.Links.Checkout = new UrlLink { Href = "https://checkout.mollie.com/pay/abc", Type = "text/html" };
        _mollieClientMock.CreatePaymentAsync(Arg.Any<PaymentRequest>()).Returns(response);

        // Act
        var result = await _service.CreatePaymentAsync(7.50m, "Description", "http://redirect", "http://webhook");

        // Assert
        Assert.Equal("tr_abc", result.PaymentId);
        Assert.Equal("https://checkout.mollie.com/pay/abc", result.PaymentUrl);
    }

    [Fact]
    public async Task CreatePaymentAsync_NullCheckoutLink_ThrowsInvalidOperationException()
    {
        // Arrange
        var response = CreateMockPaymentResponse("tr_abc", "open");
        response.Links.Checkout = null; // Set checkout link to null explicitly
        _mollieClientMock.CreatePaymentAsync(Arg.Any<PaymentRequest>()).Returns(response);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreatePaymentAsync(7.50m, "Description"));
    }

    [Fact]
    public async Task HandleWebhookAsync_PaymentNotFound_ThrowsException()
    {
        // Arrange
        var response = CreateMockPaymentResponse("tr_missing", "paid");
        _mollieClientMock.GetPaymentAsync("tr_missing").Returns(response);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() => _service.HandleWebhookAsync("tr_missing"));
        Assert.Equal("Payment not found", ex.Message);
    }

    [Fact]
    public async Task HandleWebhookAsync_PaidStatus_ProcessesPaymentsSuccessfully()
    {
        // Arrange
        _db.Settings.Add(new Setting { Name = "AccountingService", Value = "EXACT" });
        await _db.SaveChangesAsync();

        var member = new Member
        {
            Id = Guid.NewGuid(),
            FirstName = "Dave",
            LastName = "Miller",
            Email = "dave@example.com",
            StudentNumber = "s9",
            PhoneNumber = "9",
            Street = "St",
            HouseNumber = "9",
            PostalCode = "9",
            City = "Enschede",
            AuthSystemUserId = Guid.NewGuid()
        };

        var membershipPayment = new MembershipPayment
        {
            Id = 1,
            PaymentServiceId = "tr_paid_1",
            PaymentIntentUrl = "url",
            Price = 10,
            Member = member
        };

        var enrollmentPayment = new EnrollmentPayment
        {
            Id = 2,
            PaymentServiceId = "tr_paid_1",
            PaymentIntentUrl = "url",
            Price = 5,
            Member = member
        };

        _db.Members.Add(member);
        _db.MembershipPayments.Add(membershipPayment);
        _db.EnrollmentPayments.Add(enrollmentPayment);
        await _db.SaveChangesAsync();

        var paidTime = DateTimeOffset.UtcNow;
        var mockPaymentResponse = CreateMockPaymentResponse("tr_paid_1", "paid", paidTime);
        _mollieClientMock.GetPaymentAsync("tr_paid_1").Returns(mockPaymentResponse);

        // Create a new instance of MollieService so that it picks up the Environment variable at creation time
        var serviceWithAccounting = new MollieService(_db, NullLogger<MollieService>.Instance, () => _mollieClientMock);

        // Act
        await serviceWithAccounting.HandleWebhookAsync("tr_paid_1");

        // Assert
        var updatedMembership = await _db.MembershipPayments.FirstAsync(p => p.Id == 1);
        var updatedEnrollment = await _db.EnrollmentPayments.FirstAsync(p => p.Id == 2);

        Assert.NotNull(updatedMembership.PaidAt);
        Assert.NotNull(updatedEnrollment.PaidAt);

        // Verify Auth System Sync Outbox task was added for MembershipPayment
        var authTask = await _db.AuthOutboxTasks.SingleAsync(t => t.TaskType == AuthTaskType.Sync);
        Assert.Equal(member.Id, authTask.AuthSystemUserId);

        // Verify the initial activation email was queued, since this member hadn't been sent one yet
        var activationTask = await _db.AuthOutboxTasks.SingleAsync(t => t.TaskType == AuthTaskType.SendActivationEmail);
        Assert.Equal(member.Id, activationTask.AuthSystemUserId);
        // ExecuteUpdateAsync writes straight to the database, bypassing the change tracker, so the
        // already-tracked `member` instance needs an explicit reload to see the new value.
        await _db.Entry(member).ReloadAsync();
        Assert.NotNull(member.ActivationEmailSentAt);

        // Verify Accounting Tool Outbox tasks were added
        var accountingTasks = await _db.AccountingToolOutboxTasks.ToListAsync();
        Assert.Equal(2, accountingTasks.Count);
        Assert.Contains(accountingTasks, t => t.PaymentId == 1 && t.TaskType == AccountingToolTaskType.MembershipPayment);
        Assert.Contains(accountingTasks, t => t.PaymentId == 2 && t.TaskType == AccountingToolTaskType.EnrollmentPayment);
    }

    [Fact]
    public async Task HandleWebhookAsync_BegunstigerPayment_ProcessesSuccessfullyWithAuthAndAccountingSync()
    {
        // Arrange
        _db.Settings.Add(new Setting { Name = "AccountingService", Value = "EXACT" });
        await _db.SaveChangesAsync();

        var member = new Member
        {
            Id = Guid.NewGuid(),
            FirstName = "Bea",
            LastName = "Vermeer",
            Email = "bea@example.com",
            StudentNumber = "s10",
            PhoneNumber = "10",
            Street = "St",
            HouseNumber = "10",
            PostalCode = "10",
            City = "Enschede",
            Begunstiger = true,
            AuthSystemUserId = Guid.NewGuid()
        };

        var begunstigerPayment = new BegunstigerPayment
        {
            Id = 3,
            PaymentServiceId = "tr_paid_begunstiger",
            PaymentIntentUrl = "url",
            Price = 10,
            Member = member
        };

        _db.Members.Add(member);
        _db.BegunstigerPayments.Add(begunstigerPayment);
        await _db.SaveChangesAsync();

        var paidTime = DateTimeOffset.UtcNow;
        var mockPaymentResponse = CreateMockPaymentResponse("tr_paid_begunstiger", "paid", paidTime);
        _mollieClientMock.GetPaymentAsync("tr_paid_begunstiger").Returns(mockPaymentResponse);

        var serviceWithAccounting = new MollieService(_db, NullLogger<MollieService>.Instance, () => _mollieClientMock);

        // Act
        await serviceWithAccounting.HandleWebhookAsync("tr_paid_begunstiger");

        // Assert
        var updatedBegunstigerPayment = await _db.BegunstigerPayments.FirstAsync(p => p.Id == 3);
        Assert.NotNull(updatedBegunstigerPayment.PaidAt);

        var authTask = await _db.AuthOutboxTasks.SingleAsync();
        Assert.Equal(AuthTaskType.Sync, authTask.TaskType);
        Assert.Equal(member.Id, authTask.AuthSystemUserId);

        var accountingTask = await _db.AccountingToolOutboxTasks.SingleAsync();
        Assert.Equal(3u, accountingTask.PaymentId);
        Assert.Equal(AccountingToolTaskType.BegunstigerPayment, accountingTask.TaskType);
    }

    [Fact]
    public async Task HandleWebhookAsync_PendingStatus_DoesNothing()
    {
        // Arrange
        var membershipPayment = new MembershipPayment
        {
            Id = 4,
            PaymentServiceId = "tr_pending",
            PaymentIntentUrl = "url",
            Price = 10
        };
        _db.MembershipPayments.Add(membershipPayment);
        await _db.SaveChangesAsync();

        var mockPaymentResponse = CreateMockPaymentResponse("tr_pending", "pending");
        _mollieClientMock.GetPaymentAsync("tr_pending").Returns(mockPaymentResponse);

        // Act
        await _service.HandleWebhookAsync("tr_pending");

        // Assert
        var payment = await _db.MembershipPayments.FirstAsync(p => p.Id == 4);
        Assert.Null(payment.PaidAt);
        Assert.Empty(await _db.AuthOutboxTasks.ToListAsync());
        Assert.Empty(await _db.AccountingToolOutboxTasks.ToListAsync());
    }

    [Fact]
    public async Task HandleWebhookAsync_NoAccountingConfigured_DoesNotQueueAccountingTask()
    {
        // Arrange - no "AccountingService" setting row means IsUsingAccountingTool is false
        var member = new Member
        {
            Id = Guid.NewGuid(),
            FirstName = "Sam",
            LastName = "Klein",
            Email = "sam@example.com",
            StudentNumber = "s11",
            PhoneNumber = "11",
            Street = "St",
            HouseNumber = "11",
            PostalCode = "11",
            City = "Enschede",
            AuthSystemUserId = Guid.NewGuid()
        };

        var membershipPayment = new MembershipPayment
        {
            Id = 5,
            PaymentServiceId = "tr_no_accounting",
            PaymentIntentUrl = "url",
            Price = 10,
            Member = member
        };
        _db.Members.Add(member);
        _db.MembershipPayments.Add(membershipPayment);
        await _db.SaveChangesAsync();

        var paidTime = DateTimeOffset.UtcNow;
        var mockPaymentResponse = CreateMockPaymentResponse("tr_no_accounting", "paid", paidTime);
        _mollieClientMock.GetPaymentAsync("tr_no_accounting").Returns(mockPaymentResponse);

        // Act
        await _service.HandleWebhookAsync("tr_no_accounting");

        // Assert
        var payment = await _db.MembershipPayments.FirstAsync(p => p.Id == 5);
        Assert.NotNull(payment.PaidAt);
        Assert.Empty(await _db.AccountingToolOutboxTasks.ToListAsync());
    }

    [Fact]
    public async Task HandleWebhookAsync_MemberWithoutAuthSystemUserId_StillMarksPaidAndQueuesSync()
    {
        // Arrange - a member whose AuthOutboxTask.Create task hasn't completed yet shouldn't block the
        // payment from being marked as paid; AuthOutboxWorker resolves the member and creates the auth
        // user (then a follow-up Sync) once it processes the queued Sync task.
        var member = new Member
        {
            Id = Guid.NewGuid(),
            FirstName = "Robin",
            LastName = "de Boer",
            Email = "robin@example.com",
            StudentNumber = "s12",
            PhoneNumber = "12",
            Street = "St",
            HouseNumber = "12",
            PostalCode = "12",
            City = "Enschede",
            AuthSystemUserId = null
        };

        var membershipPayment = new MembershipPayment
        {
            Id = 6,
            PaymentServiceId = "tr_unlinked_member",
            PaymentIntentUrl = "url",
            Price = 10,
            Member = member
        };
        _db.Members.Add(member);
        _db.MembershipPayments.Add(membershipPayment);
        await _db.SaveChangesAsync();

        var paidTime = DateTimeOffset.UtcNow;
        var mockPaymentResponse = CreateMockPaymentResponse("tr_unlinked_member", "paid", paidTime);
        _mollieClientMock.GetPaymentAsync("tr_unlinked_member").Returns(mockPaymentResponse);

        // Act
        await _service.HandleWebhookAsync("tr_unlinked_member");

        // Assert
        var payment = await _db.MembershipPayments.FirstAsync(p => p.Id == 6);
        Assert.NotNull(payment.PaidAt);
        var authTask = await _db.AuthOutboxTasks.SingleAsync(t => t.TaskType == AuthTaskType.Sync);
        Assert.Equal(member.Id, authTask.AuthSystemUserId);
    }

    [Fact]
    public async Task HandleWebhookAsync_MembershipPaymentPaid_QueuesActivationEmailWhenNotSentYet()
    {
        // Arrange - the member hasn't been sent their initial activation email yet (e.g. the
        // confirm-mail page never sent it because the payment hadn't settled at that point).
        var member = new Member
        {
            Id = Guid.NewGuid(),
            FirstName = "Nora",
            LastName = "Jansen",
            Email = "nora@example.com",
            StudentNumber = "s13",
            PhoneNumber = "13",
            Street = "St",
            HouseNumber = "13",
            PostalCode = "13",
            City = "Enschede",
            AuthSystemUserId = Guid.NewGuid()
        };

        var membershipPayment = new MembershipPayment
        {
            Id = 7,
            PaymentServiceId = "tr_activation_email",
            PaymentIntentUrl = "url",
            Price = 10,
            Member = member
        };
        _db.Members.Add(member);
        _db.MembershipPayments.Add(membershipPayment);
        await _db.SaveChangesAsync();

        var mockPaymentResponse = CreateMockPaymentResponse("tr_activation_email", "paid", DateTimeOffset.UtcNow);
        _mollieClientMock.GetPaymentAsync("tr_activation_email").Returns(mockPaymentResponse);

        // Act
        await _service.HandleWebhookAsync("tr_activation_email");

        // Assert
        var activationTask = await _db.AuthOutboxTasks.SingleAsync(t => t.TaskType == AuthTaskType.SendActivationEmail);
        Assert.Equal(member.Id, activationTask.AuthSystemUserId);

        // ExecuteUpdateAsync writes straight to the database, bypassing the change tracker, so the
        // already-tracked `member` instance needs an explicit reload to see the new value.
        await _db.Entry(member).ReloadAsync();
        Assert.NotNull(member.ActivationEmailSentAt);
    }

    [Fact]
    public async Task HandleWebhookAsync_MembershipPaymentPaid_DoesNotQueueActivationEmailWhenAlreadySent()
    {
        // Arrange - e.g. the confirm-mail page already sent it right after the Mollie redirect.
        var member = new Member
        {
            Id = Guid.NewGuid(),
            FirstName = "Otto",
            LastName = "Bakker",
            Email = "otto@example.com",
            StudentNumber = "s14",
            PhoneNumber = "14",
            Street = "St",
            HouseNumber = "14",
            PostalCode = "14",
            City = "Enschede",
            AuthSystemUserId = Guid.NewGuid(),
            ActivationEmailSentAt = DateTimeOffset.UtcNow.AddMinutes(-5)
        };

        var membershipPayment = new MembershipPayment
        {
            Id = 8,
            PaymentServiceId = "tr_already_activated",
            PaymentIntentUrl = "url",
            Price = 10,
            Member = member
        };
        _db.Members.Add(member);
        _db.MembershipPayments.Add(membershipPayment);
        await _db.SaveChangesAsync();

        var mockPaymentResponse = CreateMockPaymentResponse("tr_already_activated", "paid", DateTimeOffset.UtcNow);
        _mollieClientMock.GetPaymentAsync("tr_already_activated").Returns(mockPaymentResponse);

        // Act
        await _service.HandleWebhookAsync("tr_already_activated");

        // Assert
        Assert.False(await _db.AuthOutboxTasks.AnyAsync(t => t.TaskType == AuthTaskType.SendActivationEmail));
    }
}
