using System;
using System.Linq;
using System.Text.Json;
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
    public async Task EnqueueUpdateSubscriptionsTask_SavesTaskToDatabase()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        db.Database.EnsureCreated();
        var provider = CreateServiceProvider(db);
        var worker = new MailSubscriptionOutboxWorker(provider, _logger);

        // Act
        worker.EnqueueUpdateSubscriptionsTask("test@example.com", ["id_news", "id_events"], db);

        // Assert
        var tasks = await db.MailSubscriptionOutboxTasks.ToListAsync();
        Assert.Single(tasks);
        Assert.Equal(MailSubscriptionOutboxTaskType.UpdateSubscriptions, tasks[0].TaskType);
        Assert.Equal("test@example.com", tasks[0].Email);
        Assert.Equal(0, tasks[0].RetryCount);
        var ids = JsonSerializer.Deserialize<string[]>(tasks[0].SubscribedListIdsJson!)!;
        Assert.Equal(["id_news", "id_events"], ids);
    }

    [Fact]
    public async Task EnqueueDeleteTask_SavesTaskToDatabase()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        db.Database.EnsureCreated();
        var provider = CreateServiceProvider(db);
        var worker = new MailSubscriptionOutboxWorker(provider, _logger);

        // Act
        worker.EnqueueDeleteTask("test@example.com", db);

        // Assert
        var tasks = await db.MailSubscriptionOutboxTasks.ToListAsync();
        Assert.Single(tasks);
        Assert.Equal(MailSubscriptionOutboxTaskType.Delete, tasks[0].TaskType);
        Assert.Equal("test@example.com", tasks[0].Email);
    }

    [Fact]
    public async Task EnqueueMigrateEmailTask_SavesTaskToDatabase()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        db.Database.EnsureCreated();
        var provider = CreateServiceProvider(db);
        var worker = new MailSubscriptionOutboxWorker(provider, _logger);

        // Act
        worker.EnqueueMigrateEmailTask("old@example.com", "new@example.com", db);

        // Assert
        var tasks = await db.MailSubscriptionOutboxTasks.ToListAsync();
        Assert.Single(tasks);
        Assert.Equal(MailSubscriptionOutboxTaskType.MigrateEmail, tasks[0].TaskType);
        Assert.Equal("old@example.com", tasks[0].OldEmail);
        Assert.Equal("new@example.com", tasks[0].Email);
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
            TaskType = MailSubscriptionOutboxTaskType.Delete,
            Email = "test@example.com",
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
    public async Task TryProcessNextTaskAsync_UpdateSubscriptionsTask_CallsUpdateAndRemovesTask()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        db.Database.EnsureCreated();

        var task = new MailSubscriptionOutboxTask
        {
            TaskType = MailSubscriptionOutboxTaskType.UpdateSubscriptions,
            Email = "sync@example.com",
            SubscribedListIdsJson = JsonSerializer.Serialize(new[] { "id_news", "id_events" }),
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
        await _mailSubscriptionService.Received(1).UpdateMemberSubscriptionsAsync(
            "sync@example.com",
            Arg.Is<System.Collections.Generic.IEnumerable<string>>(ids => ids.SequenceEqual(new[] { "id_news", "id_events" })),
            Arg.Any<CancellationToken>());

        var tasks = await db.MailSubscriptionOutboxTasks.ToListAsync();
        Assert.Empty(tasks);
    }

    [Fact]
    public async Task TryProcessNextTaskAsync_DeleteTask_CallsDeleteAndRemovesTask()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        db.Database.EnsureCreated();

        var task = new MailSubscriptionOutboxTask
        {
            TaskType = MailSubscriptionOutboxTaskType.Delete,
            Email = "gone@example.com",
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
        await _mailSubscriptionService.Received(1).DeleteMemberAsync("gone@example.com", Arg.Any<CancellationToken>());

        var tasks = await db.MailSubscriptionOutboxTasks.ToListAsync();
        Assert.Empty(tasks);
    }

    [Fact]
    public async Task TryProcessNextTaskAsync_MigrateEmailTask_CallsMigrateAndRemovesTask()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        db.Database.EnsureCreated();

        var task = new MailSubscriptionOutboxTask
        {
            TaskType = MailSubscriptionOutboxTaskType.MigrateEmail,
            Email = "new@example.com",
            OldEmail = "old@example.com",
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
        await _mailSubscriptionService.Received(1).MigrateEmailAsync("old@example.com", "new@example.com", Arg.Any<CancellationToken>());

        var tasks = await db.MailSubscriptionOutboxTasks.ToListAsync();
        Assert.Empty(tasks);
    }

    [Fact]
    public async Task TryProcessNextTaskAsync_SyncFails_ReschedulesWithBackoff()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        db.Database.EnsureCreated();

        var task = new MailSubscriptionOutboxTask
        {
            TaskType = MailSubscriptionOutboxTaskType.Delete,
            Email = "fail@example.com",
            CreatedAt = DateTimeOffset.UtcNow,
            NextAttemptAt = DateTimeOffset.UtcNow,
            RetryCount = 1
        };
        db.MailSubscriptionOutboxTasks.Add(task);
        await db.SaveChangesAsync();

        _mailSubscriptionService.DeleteMemberAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
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
