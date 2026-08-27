using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Backend.Services;
using Backend.Services.PaymentServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Backend.Tests.Services;

public class PaymentSyncServiceTests
{
    private readonly DbContextOptions<PostgresDbContext> _dbOptions;
    private readonly AbstractPaymentService _paymentService;
    private readonly IPaymentValidationService _paymentValidationService;
    private readonly AuthOutboxWorker _authOutboxWorker;
    private readonly ILogger<PaymentSyncService> _logger;

    public PaymentSyncServiceTests()
    {
        _dbOptions = new DbContextOptionsBuilder<PostgresDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _paymentService = Substitute.For<AbstractPaymentService>(null, null);
        _paymentValidationService = Substitute.For<IPaymentValidationService>();
        _authOutboxWorker = Substitute.For<AuthOutboxWorker>(null, null);
        _logger = NullLogger<PaymentSyncService>.Instance;
    }

    private ServiceProvider CreateServiceProvider(PostgresDbContext db)
    {
        var services = new ServiceCollection();
        services.AddSingleton(db);
        services.AddSingleton(_paymentService);
        services.AddSingleton(_paymentValidationService);
        services.AddSingleton(_authOutboxWorker);
        return services.BuildServiceProvider();
    }

    private class TestablePaymentSyncService(IServiceProvider serviceProvider, ILogger<PaymentSyncService> logger)
        : PaymentSyncService(serviceProvider, logger)
    {
        public Task PublicSyncPayments()
        {
            var method = typeof(PaymentSyncService)
                .GetMethod("SyncPayments", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method == null)
            {
                throw new InvalidOperationException("SyncPayments method not found");
            }
            return (Task)method.Invoke(this, null)!;
        }
    }

    private Member CreateTestMember(Guid? authSystemUserId = null, bool setAuthSystemUserId = true)
    {
        return new Member
        {
            Id = Guid.NewGuid(),
            AuthSystemUserId = setAuthSystemUserId ? (authSystemUserId ?? Guid.NewGuid()) : null,
            StudentNumber = "s1234567",
            FirstName = "John",
            LastName = "Doe",
            Email = $"{Guid.NewGuid()}@example.com",
            PhoneNumber = "0612345678",
            Street = "Main Street",
            HouseNumber = "12",
            PostalCode = "1234AB",
            City = "Enschede",
            DateOfBirth = DateTimeOffset.UtcNow.AddYears(-25),
            PreferredLanguage = Language.EN
        };
    }

    [Fact]
    public async Task SyncPayments_NoPendingPayments_DoesNothing()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        db.Database.EnsureCreated();
        var provider = CreateServiceProvider(db);
        var service = new TestablePaymentSyncService(provider, _logger);

        // Act
        await service.PublicSyncPayments();

        // Assert
        await _paymentService.DidNotReceiveWithAnyArgs().GetPaymentAsync(default!);
    }

    [Fact]
    public async Task SyncPayments_MembershipPaymentPaid_UpdatesDatabase()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        db.Database.EnsureCreated();

        var member = CreateTestMember();
        db.Members.Add(member);

        var payment = new MembershipPayment
        {
            Id = 1,
            Price = 10.0m,
            PaymentServiceId = "tr_abc123",
            PaymentIntentUrl = "https://example.com/pay",
            Member = member,
            MemberId = member.Id
        };
        db.MembershipPayments.Add(payment);
        await db.SaveChangesAsync();

        var provider = CreateServiceProvider(db);
        var service = new TestablePaymentSyncService(provider, _logger);

        var now = DateTimeOffset.UtcNow;
        _paymentService.GetPaymentAsync("tr_abc123").Returns(new GetPaymentResponse("tr_abc123", PaymentStatus.Paid, now));

        // Act
        await service.PublicSyncPayments();

        // Assert
        var updatedPayment = await db.MembershipPayments.FindAsync(1u);
        Assert.NotNull(updatedPayment);
        Assert.Equal(now, updatedPayment.PaidAt);

        var outboxTasks = await db.AuthOutboxTasks.ToListAsync();
        Assert.Single(outboxTasks);
        Assert.Equal(member.AuthSystemUserId, outboxTasks[0].AuthSystemUserId);
        Assert.Equal(AuthTaskType.Sync, outboxTasks[0].TaskType);

        var accountingTasks = await db.AccountingToolOutboxTasks.ToListAsync();
        Assert.Single(accountingTasks);
        Assert.Equal(payment.Id, accountingTasks[0].PaymentId);
        Assert.Equal(AccountingToolTaskType.MembershipPayment, accountingTasks[0].TaskType);
    }

    [Fact]
    public async Task SyncPayments_PaymentServiceFeePaymentPaid_UpdatesDatabase()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        db.Database.EnsureCreated();

        var member = CreateTestMember();
        db.Members.Add(member);

        var payment = new PaymentServiceFeePayment
        {
            Id = 2,
            Price = 1.5m,
            PaymentServiceId = "tr_fee123",
            PaymentIntentUrl = "https://example.com/pay",
            Member = member,
            MemberId = member.Id
        };
        db.PaymentServiceFeePayments.Add(payment);
        await db.SaveChangesAsync();

        var provider = CreateServiceProvider(db);
        var service = new TestablePaymentSyncService(provider, _logger);

        var now = DateTimeOffset.UtcNow;
        _paymentService.GetPaymentAsync("tr_fee123").Returns(new GetPaymentResponse("tr_fee123", PaymentStatus.Paid, now));

        // Act
        await service.PublicSyncPayments();

        // Assert
        var updatedPayment = await db.PaymentServiceFeePayments.FindAsync(2u);
        Assert.NotNull(updatedPayment);
        Assert.Equal(now, updatedPayment.PaidAt);

        var accountingTasks = await db.AccountingToolOutboxTasks.ToListAsync();
        Assert.Single(accountingTasks);
        Assert.Equal(payment.Id, accountingTasks[0].PaymentId);
        Assert.Equal(AccountingToolTaskType.PaymentServiceFeePayment, accountingTasks[0].TaskType);
    }

    [Fact]
    public async Task SyncPayments_BegunstigerPaymentPaid_UpdatesDatabase()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        db.Database.EnsureCreated();

        var member = CreateTestMember();
        member.Begunstiger = true;
        db.Members.Add(member);

        var payment = new BegunstigerPayment
        {
            Id = 6,
            Price = 10.0m,
            PaymentServiceId = "tr_begunstiger123",
            PaymentIntentUrl = "https://example.com/pay",
            Member = member,
            MemberId = member.Id
        };
        db.BegunstigerPayments.Add(payment);
        await db.SaveChangesAsync();

        var provider = CreateServiceProvider(db);
        var service = new TestablePaymentSyncService(provider, _logger);

        var now = DateTimeOffset.UtcNow;
        _paymentService.GetPaymentAsync("tr_begunstiger123").Returns(new GetPaymentResponse("tr_begunstiger123", PaymentStatus.Paid, now));

        // Act
        await service.PublicSyncPayments();

        // Assert
        var updatedPayment = await db.BegunstigerPayments.FindAsync(6u);
        Assert.NotNull(updatedPayment);
        Assert.Equal(now, updatedPayment.PaidAt);

        var outboxTasks = await db.AuthOutboxTasks.ToListAsync();
        Assert.Single(outboxTasks);
        Assert.Equal(member.AuthSystemUserId, outboxTasks[0].AuthSystemUserId);
        Assert.Equal(AuthTaskType.Sync, outboxTasks[0].TaskType);

        var accountingTasks = await db.AccountingToolOutboxTasks.ToListAsync();
        Assert.Single(accountingTasks);
        Assert.Equal(payment.Id, accountingTasks[0].PaymentId);
        Assert.Equal(AccountingToolTaskType.BegunstigerPayment, accountingTasks[0].TaskType);
    }

    [Fact]
    public async Task SyncPayments_EnrollmentPaymentPaid_UpdatesDatabase()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        db.Database.EnsureCreated();

        var member = CreateTestMember();
        db.Members.Add(member);

        var payment = new EnrollmentPayment
        {
            Id = 3,
            Price = 5.0m,
            PaymentServiceId = "tr_enroll123",
            PaymentIntentUrl = "https://example.com/pay",
            Member = member,
            MemberId = member.Id
        };
        db.EnrollmentPayments.Add(payment);
        await db.SaveChangesAsync();

        var provider = CreateServiceProvider(db);
        var service = new TestablePaymentSyncService(provider, _logger);

        var now = DateTimeOffset.UtcNow;
        _paymentService.GetPaymentAsync("tr_enroll123").Returns(new GetPaymentResponse("tr_enroll123", PaymentStatus.Paid, now));

        // Act
        await service.PublicSyncPayments();

        // Assert
        var updatedPayment = await db.EnrollmentPayments.FindAsync(3u);
        Assert.NotNull(updatedPayment);
        Assert.Equal(now, updatedPayment.PaidAt);

        var accountingTasks = await db.AccountingToolOutboxTasks.ToListAsync();
        Assert.Single(accountingTasks);
        Assert.Equal(payment.Id, accountingTasks[0].PaymentId);
        Assert.Equal(AccountingToolTaskType.EnrollmentPayment, accountingTasks[0].TaskType);
    }

    [Fact]
    public async Task SyncPayments_MembershipPaymentPaidButNoAuthSystemUserId_StillMarksPaidWithoutAuthSync()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        db.Database.EnsureCreated();

        var member = CreateTestMember(setAuthSystemUserId: false);
        db.Members.Add(member);

        var payment = new MembershipPayment
        {
            Id = 4,
            Price = 10.0m,
            PaymentServiceId = "tr_noauth",
            PaymentIntentUrl = "https://example.com/pay",
            Member = member,
            MemberId = member.Id
        };
        db.MembershipPayments.Add(payment);
        await db.SaveChangesAsync();

        var provider = CreateServiceProvider(db);
        var service = new TestablePaymentSyncService(provider, _logger);

        var now = DateTimeOffset.UtcNow;
        _paymentService.GetPaymentAsync("tr_noauth").Returns(new GetPaymentResponse("tr_noauth", PaymentStatus.Paid, now));

        // Act
        await service.PublicSyncPayments();

        // Assert
        var updatedPayment = await db.MembershipPayments.FindAsync(4u);
        Assert.NotNull(updatedPayment);
        Assert.Equal(now, updatedPayment.PaidAt); // Payment status must not be blocked by a missing auth link

        var outboxTasks = await db.AuthOutboxTasks.ToListAsync();
        Assert.Empty(outboxTasks); // No auth sync queued since there's no AuthSystemUserId yet

        var accountingTasks = await db.AccountingToolOutboxTasks.ToListAsync();
        Assert.Single(accountingTasks);
        Assert.Equal(payment.Id, accountingTasks[0].PaymentId);
    }

    [Fact]
    public async Task SyncPayments_MembershipPaymentFailed_StaleRecordRemoved_NeverPaid_RemovesMember()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        db.Database.EnsureCreated();

        var member = CreateTestMember();
        db.Members.Add(member);

        var payment = new MembershipPayment
        {
            Id = 5,
            Price = 10.0m,
            PaymentServiceId = "tr_fail1",
            PaymentIntentUrl = "https://example.com/pay",
            Member = member,
            MemberId = member.Id
        };
        db.MembershipPayments.Add(payment);
        await db.SaveChangesAsync();

        _paymentValidationService.HasEverPaidMembershipPayment(member.Id).Returns(false);

        var provider = CreateServiceProvider(db);
        var service = new TestablePaymentSyncService(provider, _logger);

        _paymentService.GetPaymentAsync("tr_fail1").Returns(new GetPaymentResponse("tr_fail1", PaymentStatus.Failed, null));

        // Act
        await service.PublicSyncPayments();

        // Assert
        var updatedPayment = await db.MembershipPayments.FindAsync(5u);
        Assert.Null(updatedPayment); // Removed

        var updatedMember = await db.Members.FindAsync(member.Id);
        Assert.Null(updatedMember); // Removed

        _authOutboxWorker.Received(1).EnqueueTask(AuthTaskType.Delete, member.AuthSystemUserId!.Value, Arg.Any<PostgresDbContext>());
    }

    [Fact]
    public async Task SyncPayments_MembershipPaymentFailed_StaleRecordRemoved_HasPaidBefore_KeepsMember()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        db.Database.EnsureCreated();

        var member = CreateTestMember();
        db.Members.Add(member);

        var payment = new MembershipPayment
        {
            Id = 6,
            Price = 10.0m,
            PaymentServiceId = "tr_fail2",
            PaymentIntentUrl = "https://example.com/pay",
            Member = member,
            MemberId = member.Id
        };
        db.MembershipPayments.Add(payment);
        await db.SaveChangesAsync();

        _paymentValidationService.HasEverPaidMembershipPayment(member.Id).Returns(true);

        var provider = CreateServiceProvider(db);
        var service = new TestablePaymentSyncService(provider, _logger);

        _paymentService.GetPaymentAsync("tr_fail2").Returns(new GetPaymentResponse("tr_fail2", PaymentStatus.Failed, null));

        // Act
        await service.PublicSyncPayments();

        // Assert
        var updatedPayment = await db.MembershipPayments.FindAsync(6u);
        Assert.Null(updatedPayment); // Removed

        var updatedMember = await db.Members.FindAsync(member.Id);
        Assert.NotNull(updatedMember); // Kept!

        _authOutboxWorker.DidNotReceive().EnqueueTask(Arg.Any<AuthTaskType>(), Arg.Any<Guid>(), Arg.Any<PostgresDbContext>());
    }

    [Fact]
    public async Task SyncPayments_BegunstigerPaymentFailed_NeverPaid_RemovesMember()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        db.Database.EnsureCreated();

        var member = CreateTestMember();
        member.Begunstiger = true;
        db.Members.Add(member);

        var payment = new BegunstigerPayment
        {
            Id = 8,
            Price = 10.0m,
            PaymentServiceId = "tr_begunstiger_fail1",
            PaymentIntentUrl = "https://example.com/pay",
            Member = member,
            MemberId = member.Id
        };
        db.BegunstigerPayments.Add(payment);
        await db.SaveChangesAsync();

        _paymentValidationService.HasEverPaidMembershipPayment(member.Id).Returns(false);
        _paymentValidationService.HasEverPaidBegunstigerFee(member.Id).Returns(false);

        var provider = CreateServiceProvider(db);
        var service = new TestablePaymentSyncService(provider, _logger);

        _paymentService.GetPaymentAsync("tr_begunstiger_fail1").Returns(new GetPaymentResponse("tr_begunstiger_fail1", PaymentStatus.Failed, null));

        // Act
        await service.PublicSyncPayments();

        // Assert
        var updatedPayment = await db.BegunstigerPayments.FindAsync(8u);
        Assert.Null(updatedPayment); // Removed

        var updatedMember = await db.Members.FindAsync(member.Id);
        Assert.Null(updatedMember); // Removed

        _authOutboxWorker.Received(1).EnqueueTask(AuthTaskType.Delete, member.AuthSystemUserId!.Value, Arg.Any<PostgresDbContext>());
    }

    [Fact]
    public async Task SyncPayments_MembershipPaymentFailed_HasOtherPendingBegunstigerPayment_KeepsMember()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        db.Database.EnsureCreated();

        var member = CreateTestMember();
        member.Begunstiger = true;
        db.Members.Add(member);

        var failedPayment = new MembershipPayment
        {
            Id = 9,
            Price = 10.0m,
            PaymentServiceId = "tr_fail_other_pending",
            PaymentIntentUrl = "https://example.com/pay",
            Member = member,
            MemberId = member.Id
        };
        db.MembershipPayments.Add(failedPayment);

        var otherPendingPayment = new BegunstigerPayment
        {
            Id = 10,
            Price = 10.0m,
            PaymentServiceId = "tr_still_pending",
            PaymentIntentUrl = "https://example.com/pay",
            Member = member,
            MemberId = member.Id
        };
        db.BegunstigerPayments.Add(otherPendingPayment);
        await db.SaveChangesAsync();

        _paymentValidationService.HasEverPaidMembershipPayment(member.Id).Returns(false);
        _paymentValidationService.HasEverPaidBegunstigerFee(member.Id).Returns(false);

        var provider = CreateServiceProvider(db);
        var service = new TestablePaymentSyncService(provider, _logger);

        _paymentService.GetPaymentAsync("tr_fail_other_pending").Returns(new GetPaymentResponse("tr_fail_other_pending", PaymentStatus.Failed, null));
        _paymentService.GetPaymentAsync("tr_still_pending").Returns(new GetPaymentResponse("tr_still_pending", PaymentStatus.Pending, null));

        // Act
        await service.PublicSyncPayments();

        // Assert
        var updatedPayment = await db.MembershipPayments.FindAsync(9u);
        Assert.Null(updatedPayment); // Removed

        var updatedMember = await db.Members.FindAsync(member.Id);
        Assert.NotNull(updatedMember); // Kept, since the begunstiger payment is still pending

        _authOutboxWorker.DidNotReceive().EnqueueTask(Arg.Any<AuthTaskType>(), Arg.Any<Guid>(), Arg.Any<PostgresDbContext>());
    }

    [Fact]
    public async Task SyncPayments_ExceptionInLoop_LogsErrorAndContinues()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        db.Database.EnsureCreated();

        var payment = new MembershipPayment
        {
            Id = 7,
            Price = 10.0m,
            PaymentServiceId = "tr_exception",
            PaymentIntentUrl = "https://example.com/pay"
        };
        db.MembershipPayments.Add(payment);
        await db.SaveChangesAsync();

        _paymentService.GetPaymentAsync("tr_exception").Returns(Task.FromException<GetPaymentResponse>(new Exception("API Error")));

        var provider = CreateServiceProvider(db);
        var service = new TestablePaymentSyncService(provider, _logger);

        // Act & Assert (Should not throw exception)
        await service.PublicSyncPayments();
    }

    [Fact]
    public async Task ExecuteAsync_CanceledImmediately_ReturnsImmediately()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        var provider = CreateServiceProvider(db);
        var service = new PaymentSyncService(provider, _logger);

        // Act
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately so Task.Delay(5000, token) throws TaskCanceledException and exits instantly

        var startTask = service.StartAsync(cts.Token);
        await service.StopAsync(CancellationToken.None);
        await startTask;
    }
}
