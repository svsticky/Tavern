using Backend.Database;
using Backend.Models.Domain;
using Backend.Services;
using Backend.Services.PaymentServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace Backend.Tests.Services;

public class PaymentWebhookServiceTests : IDisposable
{
    private readonly PostgresDbContext _db;
    private readonly AbstractPaymentService _paymentServiceMock;
    private readonly ILogger<PaymentWebhookService> _loggerMock;

    public PaymentWebhookServiceTests()
    {
        var options = new DbContextOptionsBuilder<PostgresDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new PostgresDbContext(options);
        _db.Database.EnsureCreated();

        _paymentServiceMock = Substitute.For<AbstractPaymentService>(_db, Substitute.For<ILogger<AbstractPaymentService>>());
        _loggerMock = Substitute.For<ILogger<PaymentWebhookService>>();
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
        Environment.SetEnvironmentVariable("ACCOUNTING_SERVICE", null);
    }

    private PaymentWebhookService CreateService()
    {
        return new PaymentWebhookService(_db, _paymentServiceMock, _loggerMock);
    }

    private Member CreateMember(Guid authSystemUserId)
    {
        return new Member
        {
            Id = Guid.NewGuid(),
            AuthSystemUserId = authSystemUserId,
            FirstName = "Jane",
            LastName = "Doe",
            Email = "jane@example.com",
            StudentNumber = "s1234567",
            PhoneNumber = "+31612345678",
            Street = "Main Street",
            HouseNumber = "12",
            PostalCode = "7500AA",
            City = "Enschede"
        };
    }

    [Fact]
    public async Task HandleWebhookAsync_WhenPaymentNotFound_ThrowsException()
    {
        // Arrange
        var id = "pay_12345";
        var paidAt = DateTimeOffset.UtcNow;
        _paymentServiceMock.GetPaymentAsync(id).Returns(new GetPaymentResponse(id, PaymentStatus.Paid, paidAt));

        var service = CreateService();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(async () => await service.HandleWebhookAsync(id));
        Assert.Equal("Payment not found", exception.Message);
    }

    [Fact]
    public async Task HandleWebhookAsync_WithPaidStatus_ProcessesMembershipPayment_SyncsAuthAndAccounting()
    {
        // Arrange
        _db.Settings.Add(new Setting { Name = "AccountingService", Value = "EXACT" });
        await _db.SaveChangesAsync();
        var id = "pay_12345";
        var paidAt = DateTimeOffset.UtcNow;
        _paymentServiceMock.GetPaymentAsync(id).Returns(new GetPaymentResponse(id, PaymentStatus.Paid, paidAt));

        var memberId = Guid.NewGuid();
        var authUserId = Guid.NewGuid();
        var member = CreateMember(authUserId);
        _db.Members.Add(member);

        var payment = new MembershipPayment
        {
            Id = 1,
            PaymentServiceId = id,
            PaymentIntentUrl = "https://mollie.com/pay/123",
            Price = 15.00m,
            MemberId = member.Id,
            Member = member
        };
        _db.MembershipPayments.Add(payment);
        await _db.SaveChangesAsync();

        var service = CreateService();

        // Act
        await service.HandleWebhookAsync(id);

        // Assert
        // Verify payment marked paid
        var dbPayment = await _db.MembershipPayments.FindAsync(1u);
        Assert.NotNull(dbPayment);
        Assert.Equal(paidAt, dbPayment.PaidAt);

        // Verify auth outbox task queued
        var authTasks = await _db.AuthOutboxTasks.ToListAsync();
        Assert.Single(authTasks);
        Assert.Equal(AuthTaskType.Sync, authTasks[0].TaskType);
        Assert.Equal(authUserId, authTasks[0].AuthSystemUserId);

        // Verify accounting task queued
        var accountingTasks = await _db.AccountingToolOutboxTasks.ToListAsync();
        Assert.Single(accountingTasks);
        Assert.Equal(payment.Id, accountingTasks[0].PaymentId);
        Assert.Equal(AccountingToolTaskType.MembershipPayment, accountingTasks[0].TaskType);
    }

    [Fact]
    public async Task HandleWebhookAsync_WithPaidStatus_ProcessesMembershipPayment_NoAccounting()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ACCOUNTING_SERVICE", null);
        var id = "pay_12345";
        var paidAt = DateTimeOffset.UtcNow;
        _paymentServiceMock.GetPaymentAsync(id).Returns(new GetPaymentResponse(id, PaymentStatus.Paid, paidAt));

        var member = CreateMember(Guid.NewGuid());
        _db.Members.Add(member);

        var payment = new MembershipPayment
        {
            Id = 1,
            PaymentServiceId = id,
            PaymentIntentUrl = "https://mollie.com/pay/123",
            Price = 15.00m,
            MemberId = member.Id,
            Member = member
        };
        _db.MembershipPayments.Add(payment);
        await _db.SaveChangesAsync();

        var service = CreateService();

        // Act
        await service.HandleWebhookAsync(id);

        // Assert
        // Verify payment marked paid
        var dbPayment = await _db.MembershipPayments.FindAsync(1u);
        Assert.NotNull(dbPayment);
        Assert.Equal(paidAt, dbPayment.PaidAt);

        // Verify accounting task NOT queued
        var accountingTasks = await _db.AccountingToolOutboxTasks.ToListAsync();
        Assert.Empty(accountingTasks);
    }

    [Fact]
    public async Task HandleWebhookAsync_WithPaidStatus_ProcessesEnrollmentPayment_AndFeePayment_WithAccounting()
    {
        // Arrange
        _db.Settings.Add(new Setting { Name = "AccountingService", Value = "EXACT" });
        await _db.SaveChangesAsync();
        var id = "pay_12345";
        var paidAt = DateTimeOffset.UtcNow;
        _paymentServiceMock.GetPaymentAsync(id).Returns(new GetPaymentResponse(id, PaymentStatus.Paid, paidAt));

        var payment1 = new EnrollmentPayment
        {
            Id = 10,
            PaymentServiceId = id,
            PaymentIntentUrl = "https://mollie.com/pay/123",
            Price = 5.00m,
            ActivityId = 1
        };

        var payment2 = new PaymentServiceFeePayment
        {
            Id = 11,
            PaymentServiceId = id,
            PaymentIntentUrl = "https://mollie.com/pay/123",
            Price = 0.50m
        };

        _db.EnrollmentPayments.Add(payment1);
        _db.PaymentServiceFeePayments.Add(payment2);
        await _db.SaveChangesAsync();

        var service = CreateService();

        // Act
        await service.HandleWebhookAsync(id);

        // Assert
        var dbPayment1 = await _db.EnrollmentPayments.FindAsync(10u);
        var dbPayment2 = await _db.PaymentServiceFeePayments.FindAsync(11u);
        Assert.Equal(paidAt, dbPayment1!.PaidAt);
        Assert.Equal(paidAt, dbPayment2!.PaidAt);

        // Verify accounting tasks queued
        var accountingTasks = await _db.AccountingToolOutboxTasks.OrderBy(t => t.PaymentId).ToListAsync();
        Assert.Equal(2, accountingTasks.Count);
        Assert.Equal(payment1.Id, accountingTasks[0].PaymentId);
        Assert.Equal(AccountingToolTaskType.EnrollmentPayment, accountingTasks[0].TaskType);
        Assert.Equal(payment2.Id, accountingTasks[1].PaymentId);
        Assert.Equal(AccountingToolTaskType.EnrollmentPayment, accountingTasks[1].TaskType);

        // Enrollment payments do not queue auth sync tasks
        var authTasks = await _db.AuthOutboxTasks.ToListAsync();
        Assert.Empty(authTasks);
    }

    [Fact]
    public async Task HandleWebhookAsync_WithPendingStatus_DoesNothing()
    {
        // Arrange
        var id = "pay_12345";
        _paymentServiceMock.GetPaymentAsync(id).Returns(new GetPaymentResponse(id, PaymentStatus.Pending, null));

        var payment = new MembershipPayment
        {
            Id = 1,
            PaymentServiceId = id,
            PaymentIntentUrl = "https://mollie.com/pay/123",
            Price = 15.00m
        };
        _db.MembershipPayments.Add(payment);
        await _db.SaveChangesAsync();

        var service = CreateService();

        // Act
        await service.HandleWebhookAsync(id);

        // Assert
        var dbPayment = await _db.MembershipPayments.FindAsync(1u);
        Assert.Null(dbPayment!.PaidAt);
        Assert.Empty(await _db.AuthOutboxTasks.ToListAsync());
    }

    [Fact]
    public async Task HandleWebhookAsync_WhenExceptionOccurs_RollsBackTransactionAndThrows()
    {
        // Arrange
        var id = "pay_12345";
        var paidAt = DateTimeOffset.UtcNow;
        _paymentServiceMock.GetPaymentAsync(id).Returns(new GetPaymentResponse(id, PaymentStatus.Paid, paidAt));

        // Add a membership payment but WITHOUT a member. This will cause
        // QueueAuthenticationSystemSyncIfNeeded to throw an exception because
        // payment.Member is null, triggering rollback.
        var payment = new MembershipPayment
        {
            Id = 1,
            PaymentServiceId = id,
            PaymentIntentUrl = "https://mollie.com/pay/123",
            Price = 15.00m,
            Member = null
        };
        _db.MembershipPayments.Add(payment);
        await _db.SaveChangesAsync();

        var service = CreateService();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(async () => await service.HandleWebhookAsync(id));
        Assert.Contains("Member does not have a authentication system ID", exception.Message);

        // Verify rollback: payment was NOT marked paid in DB (using AsNoTracking to bypass change tracker cache)
        var dbPayment = await _db.MembershipPayments.AsNoTracking().FirstOrDefaultAsync(p => p.Id == 1u);
        Assert.Null(dbPayment!.PaidAt);
    }
}
