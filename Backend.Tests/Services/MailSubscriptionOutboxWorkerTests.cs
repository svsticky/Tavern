using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Backend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Backend.Tests.Services;

public class MailSubscriptionOutboxWorkerTests
{
    private readonly DbContextOptions<PostgresDbContext> _dbOptions;
    private readonly IMailSubscriptionService _mailSubscriptionService;
    private readonly ILogger<MailSubscriptionOutboxWorker> _logger;

    public MailSubscriptionOutboxWorkerTests()
    {
        _dbOptions = new DbContextOptionsBuilder<PostgresDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _mailSubscriptionService = Substitute.For<IMailSubscriptionService>();
        _logger = NullLogger<MailSubscriptionOutboxWorker>.Instance;
    }

    private ServiceProvider CreateServiceProvider(PostgresDbContext db)
    {
        var services = new ServiceCollection();
        services.AddSingleton(db);
        services.AddSingleton(_mailSubscriptionService);
        return services.BuildServiceProvider();
    }

    private class TestableMailSubscriptionOutboxWorker(IServiceProvider serviceProvider, ILogger<MailSubscriptionOutboxWorker> logger)
        : MailSubscriptionOutboxWorker(serviceProvider, logger)
    {
        public Task<bool> PublicTryProcessNextTaskAsync(CancellationToken ct)
        {
            var method = typeof(MailSubscriptionOutboxWorker)
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
        var worker = new MailSubscriptionOutboxWorker(provider, _logger);

        // Act
        worker.EnqueueTask("test@example.com", 3u, db);

        // Assert
        var tasks = await db.MailSubscriptionOutboxTasks.ToListAsync();
        Assert.Single(tasks);
        Assert.Equal("test@example.com", tasks[0].Email);
        Assert.Equal(3u, tasks[0].MailSubscription);
        Assert.Equal(0, tasks[0].RetryCount);
    }

    [Fact]
    public async Task TryProcessNextTaskAsync_NoTasks_ReturnsFalse()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        db.Database.EnsureCreated();
        var provider = CreateServiceProvider(db);
        var worker = new TestableMailSubscriptionOutboxWorker(provider, _logger);

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

        db.MailSubscriptionOutboxTasks.Add(new MailSubscriptionOutboxTask
        {
            Email = "test@example.com",
            MailSubscription = 1u,
            CreatedAt = DateTimeOffset.UtcNow,
            NextAttemptAt = DateTimeOffset.UtcNow.AddMinutes(5)
        });
        await db.SaveChangesAsync();

        var provider = CreateServiceProvider(db);
        var worker = new TestableMailSubscriptionOutboxWorker(provider, _logger);

        // Act
        var result = await worker.PublicTryProcessNextTaskAsync(CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task TryProcessNextTaskAsync_TaskFound_UpdatesSubscriptionAndRemovesTask()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        db.Database.EnsureCreated();

        var task = new MailSubscriptionOutboxTask
        {
            Email = "sync@example.com",
            MailSubscription = 7u,
            CreatedAt = DateTimeOffset.UtcNow,
            NextAttemptAt = DateTimeOffset.UtcNow
        };
        db.MailSubscriptionOutboxTasks.Add(task);
        await db.SaveChangesAsync();

        var provider = CreateServiceProvider(db);
        var worker = new TestableMailSubscriptionOutboxWorker(provider, _logger);

        // Act
        var result = await worker.PublicTryProcessNextTaskAsync(CancellationToken.None);

        // Assert
        Assert.True(result);
        await _mailSubscriptionService.Received(1).UpdateSubscriptionAsync("sync@example.com", 7u, Arg.Any<CancellationToken>());

        var tasks = await db.MailSubscriptionOutboxTasks.ToListAsync();
        Assert.Empty(tasks); // Removed
    }

    [Fact]
    public async Task TryProcessNextTaskAsync_SyncFails_ReschedulesWithBackoff()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        db.Database.EnsureCreated();

        var task = new MailSubscriptionOutboxTask
        {
            Email = "fail@example.com",
            MailSubscription = 2u,
            CreatedAt = DateTimeOffset.UtcNow,
            NextAttemptAt = DateTimeOffset.UtcNow,
            RetryCount = 1
        };
        db.MailSubscriptionOutboxTasks.Add(task);
        await db.SaveChangesAsync();

        _mailSubscriptionService.UpdateSubscriptionAsync(Arg.Any<string>(), Arg.Any<uint>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new Exception("MailChimp API Error")));

        var provider = CreateServiceProvider(db);
        var worker = new TestableMailSubscriptionOutboxWorker(provider, _logger);

        // Act
        var result = await worker.PublicTryProcessNextTaskAsync(CancellationToken.None);

        // Assert
        Assert.True(result);
        
        var updatedTasks = await db.MailSubscriptionOutboxTasks.ToListAsync();
        Assert.Single(updatedTasks);
        Assert.Equal(2, updatedTasks[0].RetryCount); // RetryCount incremented
        Assert.True(updatedTasks[0].NextAttemptAt > DateTimeOffset.UtcNow); // NextAttemptAt rescheduled in the future
    }

    [Fact]
    public async Task ExecuteAsync_Disabled_ReturnsImmediately()
    {
        // Arrange
        Environment.SetEnvironmentVariable("MAIL_SUBSCRIPTION_SERVICE", null);
        using var db = new PostgresDbContext(_dbOptions);
        var provider = CreateServiceProvider(db);
        var worker = new MailSubscriptionOutboxWorker(provider, _logger);

        // Act
        var cts = new CancellationTokenSource();
        await worker.StartAsync(cts.Token);
        await worker.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ExecuteAsync_Enabled_ProcessesTasksAndDelays()
    {
        // Arrange
        Environment.SetEnvironmentVariable("MAIL_SUBSCRIPTION_SERVICE", "True");
        try
        {
            using var db = new PostgresDbContext(_dbOptions);
            db.Database.EnsureCreated();
            var provider = CreateServiceProvider(db);
            var worker = new MailSubscriptionOutboxWorker(provider, _logger);

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
            Environment.SetEnvironmentVariable("MAIL_SUBSCRIPTION_SERVICE", null);
        }
    }
}
