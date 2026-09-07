using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Backend.Database;
using Backend.Models.Domain;
using Backend.Services;
using Backend.Services.AccountingToolServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Backend.Tests.Services;

[Collection("NonParallelEnvironment")]
public class AccountingToolOutboxWorkerTests
{
    private readonly DbContextOptions<PostgresDbContext> _dbOptions;
    private readonly AbstractAccountingToolService _accountingToolService;
    private readonly ILogger<AccountingToolOutboxWorker> _logger;

    public AccountingToolOutboxWorkerTests()
    {
        _dbOptions = new DbContextOptionsBuilder<PostgresDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _accountingToolService = Substitute.For<AbstractAccountingToolService>(
            Substitute.For<PostgresDbContext>(_dbOptions),
            NullLogger<AbstractAccountingToolService>.Instance
        );
        _logger = NullLogger<AccountingToolOutboxWorker>.Instance;
    }

    private ServiceProvider CreateServiceProvider(PostgresDbContext db)
    {
        var services = new ServiceCollection();
        services.AddSingleton(db);
        services.AddSingleton(_accountingToolService);
        return services.BuildServiceProvider();
    }

    private class TestableAccountingToolOutboxWorker(IServiceProvider serviceProvider, ILogger<AccountingToolOutboxWorker> logger)
        : AccountingToolOutboxWorker(serviceProvider, logger)
    {
        public Task<bool> PublicTryProcessNextTaskAsync(CancellationToken ct)
        {
            var method = typeof(AccountingToolOutboxWorker)
                .GetMethod("TryProcessNextTaskAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method == null)
            {
                throw new InvalidOperationException("TryProcessNextTaskAsync method not found");
            }
            return (Task<bool>)method.Invoke(this, [ct])!;
        }
    }

    [Fact]
    public async Task EnqueueTask_SavesTaskToDatabase()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        db.Database.EnsureCreated();
        var provider = CreateServiceProvider(db);
        var worker = new AccountingToolOutboxWorker(provider, _logger);

        // Act
        worker.EnqueueTask(AccountingToolTaskType.MembershipPayment, 123u, db);

        // Assert
        var tasks = await db.AccountingToolOutboxTasks.ToListAsync();
        Assert.Single(tasks);
        Assert.Equal(AccountingToolTaskType.MembershipPayment, tasks[0].TaskType);
        Assert.Equal(123u, tasks[0].PaymentId);
        Assert.Equal(0, tasks[0].RetryCount);
    }

    [Fact]
    public async Task TryProcessNextTaskAsync_NoTasks_ReturnsFalse()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        db.Database.EnsureCreated();
        var provider = CreateServiceProvider(db);
        var worker = new TestableAccountingToolOutboxWorker(provider, _logger);

        // Act
        var result = await worker.PublicTryProcessNextTaskAsync(CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task TryProcessNextTaskAsync_TaskInFuture_ReturnsFalse()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        db.Database.EnsureCreated();

        db.AccountingToolOutboxTasks.Add(new AccountingToolOutboxTask
        {
            TaskType = AccountingToolTaskType.MembershipPayment,
            PaymentId = 1u,
            CreatedAt = DateTimeOffset.UtcNow,
            NextAttemptAt = DateTimeOffset.UtcNow.AddMinutes(5)
        });
        await db.SaveChangesAsync();

        var provider = CreateServiceProvider(db);
        var worker = new TestableAccountingToolOutboxWorker(provider, _logger);

        // Act
        var result = await worker.PublicTryProcessNextTaskAsync(CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task TryProcessNextTaskAsync_EnrollmentPaymentTask_PaymentFound_SyncsAndUpdatesId()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        db.Database.EnsureCreated();

        var payment = new EnrollmentPayment
        {
            Id = 1,
            Price = 15.0m,
            PaymentServiceId = "tr_enroll",
            PaymentIntentUrl = "https://example.com/pay"
        };
        db.EnrollmentPayments.Add(payment);

        var task = new AccountingToolOutboxTask
        {
            TaskType = AccountingToolTaskType.EnrollmentPayment,
            PaymentId = 1,
            CreatedAt = DateTimeOffset.UtcNow,
            NextAttemptAt = DateTimeOffset.UtcNow
        };
        db.AccountingToolOutboxTasks.Add(task);
        await db.SaveChangesAsync();

        var expectedEntryId = Guid.NewGuid();
        _accountingToolService.SyncPaymentAsync(Arg.Any<Payment>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expectedEntryId));

        var provider = CreateServiceProvider(db);
        var worker = new TestableAccountingToolOutboxWorker(provider, _logger);

        // Act
        var result = await worker.PublicTryProcessNextTaskAsync(CancellationToken.None);

        // Assert
        Assert.True(result);
        var updatedPayment = await db.EnrollmentPayments.FindAsync(1u);
        Assert.NotNull(updatedPayment);
        Assert.Equal(expectedEntryId, updatedPayment.AccountingToolEntryId);

        var tasks = await db.AccountingToolOutboxTasks.ToListAsync();
        Assert.Empty(tasks); // Removed
    }

    [Fact]
    public async Task TryProcessNextTaskAsync_MembershipPaymentTask_PaymentFound_SyncsAndUpdatesId()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        db.Database.EnsureCreated();

        var payment = new MembershipPayment
        {
            Id = 2,
            Price = 10.0m,
            PaymentServiceId = "tr_member",
            PaymentIntentUrl = "https://example.com/pay"
        };
        db.MembershipPayments.Add(payment);

        var task = new AccountingToolOutboxTask
        {
            TaskType = AccountingToolTaskType.MembershipPayment,
            PaymentId = 2,
            CreatedAt = DateTimeOffset.UtcNow,
            NextAttemptAt = DateTimeOffset.UtcNow
        };
        db.AccountingToolOutboxTasks.Add(task);
        await db.SaveChangesAsync();

        var expectedEntryId = Guid.NewGuid();
        _accountingToolService.SyncPaymentAsync(Arg.Any<Payment>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expectedEntryId));

        var provider = CreateServiceProvider(db);
        var worker = new TestableAccountingToolOutboxWorker(provider, _logger);

        // Act
        var result = await worker.PublicTryProcessNextTaskAsync(CancellationToken.None);

        // Assert
        Assert.True(result);
        var updatedPayment = await db.MembershipPayments.FindAsync(2u);
        Assert.NotNull(updatedPayment);
        Assert.Equal(expectedEntryId, updatedPayment.AccountingToolEntryId);

        var tasks = await db.AccountingToolOutboxTasks.ToListAsync();
        Assert.Empty(tasks); // Removed
    }

    [Fact]
    public async Task TryProcessNextTaskAsync_PaymentServiceFeePaymentTask_PaymentFound_SyncsAndUpdatesId()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        db.Database.EnsureCreated();

        var payment = new PaymentServiceFeePayment
        {
            Id = 4,
            Price = 0.39m,
            PaymentServiceId = "tr_fee",
            PaymentIntentUrl = "https://example.com/pay"
        };
        db.PaymentServiceFeePayments.Add(payment);

        var task = new AccountingToolOutboxTask
        {
            TaskType = AccountingToolTaskType.PaymentServiceFeePayment,
            PaymentId = 4,
            CreatedAt = DateTimeOffset.UtcNow,
            NextAttemptAt = DateTimeOffset.UtcNow
        };
        db.AccountingToolOutboxTasks.Add(task);
        await db.SaveChangesAsync();

        var expectedEntryId = Guid.NewGuid();
        _accountingToolService.SyncPaymentAsync(Arg.Any<Payment>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expectedEntryId));

        var provider = CreateServiceProvider(db);
        var worker = new TestableAccountingToolOutboxWorker(provider, _logger);

        // Act
        var result = await worker.PublicTryProcessNextTaskAsync(CancellationToken.None);

        // Assert
        Assert.True(result);
        var updatedPayment = await db.PaymentServiceFeePayments.FindAsync(4u);
        Assert.NotNull(updatedPayment);
        Assert.Equal(expectedEntryId, updatedPayment.AccountingToolEntryId);

        var tasks = await db.AccountingToolOutboxTasks.ToListAsync();
        Assert.Empty(tasks); // Removed
    }

    [Fact]
    public async Task TryProcessNextTaskAsync_BegunstigerPaymentTask_PaymentFound_SyncsAndUpdatesId()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        db.Database.EnsureCreated();

        var payment = new BegunstigerPayment
        {
            Id = 5,
            Price = 10.0m,
            PaymentServiceId = "tr_begunstiger",
            PaymentIntentUrl = "https://example.com/pay"
        };
        db.BegunstigerPayments.Add(payment);

        var task = new AccountingToolOutboxTask
        {
            TaskType = AccountingToolTaskType.BegunstigerPayment,
            PaymentId = 5,
            CreatedAt = DateTimeOffset.UtcNow,
            NextAttemptAt = DateTimeOffset.UtcNow
        };
        db.AccountingToolOutboxTasks.Add(task);
        await db.SaveChangesAsync();

        var expectedEntryId = Guid.NewGuid();
        _accountingToolService.SyncPaymentAsync(Arg.Any<Payment>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(expectedEntryId));

        var provider = CreateServiceProvider(db);
        var worker = new TestableAccountingToolOutboxWorker(provider, _logger);

        // Act
        var result = await worker.PublicTryProcessNextTaskAsync(CancellationToken.None);

        // Assert
        Assert.True(result);
        var updatedPayment = await db.BegunstigerPayments.FindAsync(5u);
        Assert.NotNull(updatedPayment);
        Assert.Equal(expectedEntryId, updatedPayment.AccountingToolEntryId);

        var tasks = await db.AccountingToolOutboxTasks.ToListAsync();
        Assert.Empty(tasks); // Removed
    }

    [Fact]
    public async Task TryProcessNextTaskAsync_PaymentNotFound_LogsWarningAndRemovesTask()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        db.Database.EnsureCreated();

        var task = new AccountingToolOutboxTask
        {
            TaskType = AccountingToolTaskType.MembershipPayment,
            PaymentId = 999,
            CreatedAt = DateTimeOffset.UtcNow,
            NextAttemptAt = DateTimeOffset.UtcNow
        };
        db.AccountingToolOutboxTasks.Add(task);
        await db.SaveChangesAsync();

        var provider = CreateServiceProvider(db);
        var worker = new TestableAccountingToolOutboxWorker(provider, _logger);

        // Act
        var result = await worker.PublicTryProcessNextTaskAsync(CancellationToken.None);

        // Assert
        Assert.True(result);
        await _accountingToolService.DidNotReceiveWithAnyArgs().SyncPaymentAsync(default!, default);

        var tasks = await db.AccountingToolOutboxTasks.ToListAsync();
        Assert.Empty(tasks); // Removed
    }

    [Fact]
    public async Task TryProcessNextTaskAsync_SyncFails_ReschedulesWithBackoff()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        db.Database.EnsureCreated();

        var payment = new MembershipPayment
        {
            Id = 3,
            Price = 10.0m,
            PaymentServiceId = "tr_fail",
            PaymentIntentUrl = "https://example.com/pay"
        };
        db.MembershipPayments.Add(payment);

        var task = new AccountingToolOutboxTask
        {
            TaskType = AccountingToolTaskType.MembershipPayment,
            PaymentId = 3,
            CreatedAt = DateTimeOffset.UtcNow,
            NextAttemptAt = DateTimeOffset.UtcNow,
            RetryCount = 1
        };
        db.AccountingToolOutboxTasks.Add(task);
        await db.SaveChangesAsync();

        _accountingToolService.SyncPaymentAsync(Arg.Any<Payment>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<Guid>(new Exception("API Error")));

        var provider = CreateServiceProvider(db);
        var worker = new TestableAccountingToolOutboxWorker(provider, _logger);

        // Act
        var result = await worker.PublicTryProcessNextTaskAsync(CancellationToken.None);

        // Assert
        Assert.True(result);

        var updatedTasks = await db.AccountingToolOutboxTasks.ToListAsync();
        Assert.Single(updatedTasks);
        Assert.Equal(2, updatedTasks[0].RetryCount); // RetryCount incremented
        Assert.True(updatedTasks[0].NextAttemptAt > DateTimeOffset.UtcNow); // NextAttemptAt in the future
    }

    [Fact]
    public async Task ExecuteAsync_Disabled_ReturnsImmediately()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ACCOUNTING_SERVICE", null);
        using var db = new PostgresDbContext(_dbOptions);
        var provider = CreateServiceProvider(db);
        var worker = new AccountingToolOutboxWorker(provider, _logger);

        // Act
        var cts = new CancellationTokenSource();
        await worker.StartAsync(cts.Token);
        await worker.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ExecuteAsync_AccountingDisabledByEnv_SkipsProcessingAndDelays()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ACCOUNTING_ENABLED", "false");
        try
        {
            using var db = new PostgresDbContext(_dbOptions);
            db.Database.EnsureCreated();
            var provider = CreateServiceProvider(db);
            var worker = new AccountingToolOutboxWorker(provider, _logger);

            // Act
            var cts = new CancellationTokenSource();
            var startTask = worker.StartAsync(cts.Token);

            await Task.Delay(100);

            cts.Cancel();
            await worker.StopAsync(CancellationToken.None);
            await startTask;
        }
        finally
        {
            Environment.SetEnvironmentVariable("ACCOUNTING_ENABLED", null);
        }
    }

    [Fact]
    public async Task ExecuteAsync_NoAccountingServiceConfigured_SkipsProcessingAndDelays()
    {
        // Arrange - no "AccountingService" setting row
        using var db = new PostgresDbContext(_dbOptions);
        db.Database.EnsureCreated();
        var provider = CreateServiceProvider(db);
        var worker = new AccountingToolOutboxWorker(provider, _logger);

        // Act
        var cts = new CancellationTokenSource();
        var startTask = worker.StartAsync(cts.Token);

        await Task.Delay(100);

        cts.Cancel();
        await worker.StopAsync(CancellationToken.None);
        await startTask;
    }

    [Fact]
    public async Task ExecuteAsync_Enabled_ProcessesTasksAndDelays()
    {
        // Arrange - "AccountingService" is read from the Settings table, not an env var
        using var db = new PostgresDbContext(_dbOptions);
        db.Database.EnsureCreated();
        db.Settings.Add(new Setting { Name = "AccountingService", Value = "EXACT" });
        db.SaveChanges();
        var provider = CreateServiceProvider(db);
        var worker = new AccountingToolOutboxWorker(provider, _logger);

        // Act
        var cts = new CancellationTokenSource();
        var startTask = worker.StartAsync(cts.Token);

        await Task.Delay(100);

        cts.Cancel();
        await worker.StopAsync(CancellationToken.None);
        await startTask;
    }

    [Fact]
    public async Task ExecuteAsync_EnabledWithEnvVarTrue_ProcessesTasksAndDelays()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ACCOUNTING_SERVICE", "True");
        try
        {
            using var db = new PostgresDbContext(_dbOptions);
            db.Database.EnsureCreated();
            db.Settings.Add(new Setting { Name = "AccountingService", Value = "EXACT" });
            db.SaveChanges();
            var provider = CreateServiceProvider(db);
            var worker = new AccountingToolOutboxWorker(provider, _logger);

            // Act
            var cts = new CancellationTokenSource();
            var startTask = worker.StartAsync(cts.Token);

            await Task.Delay(100);

            cts.Cancel();
            await worker.StopAsync(CancellationToken.None);
            await startTask;
        }
        finally
        {
            Environment.SetEnvironmentVariable("ACCOUNTING_SERVICE", null);
        }
    }
}
