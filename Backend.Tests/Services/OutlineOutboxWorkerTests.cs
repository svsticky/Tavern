using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Backend.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace Backend.Tests.Services;

public class OutlineOutboxWorkerTests
{
    private readonly DbContextOptions<PostgresDbContext> _dbOptions;
    private readonly IOutlineService _outlineService;
    private readonly ILogger<OutlineOutboxWorker> _logger;

    public OutlineOutboxWorkerTests()
    {
        _dbOptions = new DbContextOptionsBuilder<PostgresDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _outlineService = Substitute.For<IOutlineService>();
        _logger = NullLogger<OutlineOutboxWorker>.Instance;
    }

    private ServiceProvider CreateServiceProvider(PostgresDbContext db)
    {
        var services = new ServiceCollection();
        services.AddSingleton(db);
        services.AddSingleton(_outlineService);
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task EnqueueSyncMail_SavesTaskToDatabase()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        var provider = CreateServiceProvider(db);
        var worker = new OutlineOutboxWorker(provider, _logger);

        // Act
        worker.EnqueueSyncMail("old@example.com", "new@example.com", db);

        // Assert
        var tasks = await db.OutlineOutboxTasks.ToListAsync();
        Assert.Single(tasks);
        Assert.Equal(OutlineTaskType.SyncMail, tasks[0].TaskType);
        Assert.Equal("old@example.com", tasks[0].Email);
        Assert.Equal("new@example.com", tasks[0].NewEmail);
        Assert.Equal(0, tasks[0].RetryCount);
    }

    [Fact]
    public async Task EnqueueDeleteMail_SavesTaskToDatabase()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        var provider = CreateServiceProvider(db);
        var worker = new OutlineOutboxWorker(provider, _logger);

        // Act
        worker.EnqueueDeleteMail("delete@example.com", db);

        // Assert
        var tasks = await db.OutlineOutboxTasks.ToListAsync();
        Assert.Single(tasks);
        Assert.Equal(OutlineTaskType.DeleteMail, tasks[0].TaskType);
        Assert.Equal("delete@example.com", tasks[0].Email);
        Assert.Equal(0, tasks[0].RetryCount);
    }

    [Fact]
    public async Task EnqueueAdminStatusUpdate_Promote_SavesPromoteTask()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        var provider = CreateServiceProvider(db);
        var worker = new OutlineOutboxWorker(provider, _logger);

        // Act
        worker.EnqueueAdminStatusUpdate("admin@example.com", true, db);

        // Assert
        var tasks = await db.OutlineOutboxTasks.ToListAsync();
        Assert.Single(tasks);
        Assert.Equal(OutlineTaskType.PromoteAdmin, tasks[0].TaskType);
        Assert.Equal("admin@example.com", tasks[0].Email);
        Assert.Equal(0, tasks[0].RetryCount);
    }

    [Fact]
    public async Task EnqueueAdminStatusUpdate_Demote_SavesDemoteTask()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        var provider = CreateServiceProvider(db);
        var worker = new OutlineOutboxWorker(provider, _logger);

        // Act
        worker.EnqueueAdminStatusUpdate("member@example.com", false, db);

        // Assert
        var tasks = await db.OutlineOutboxTasks.ToListAsync();
        Assert.Single(tasks);
        Assert.Equal(OutlineTaskType.DemoteAdmin, tasks[0].TaskType);
        Assert.Equal("member@example.com", tasks[0].Email);
        Assert.Equal(0, tasks[0].RetryCount);
    }

    [Fact]
    public async Task TryProcessNextTaskAsync_WhenNoTasks_ReturnsFalse()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        var provider = CreateServiceProvider(db);
        var worker = new OutlineOutboxWorker(provider, _logger);

        // Act
        var result = await worker.TryProcessNextTaskAsync(CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task TryProcessNextTaskAsync_WhenTaskInFuture_ReturnsFalse()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        db.OutlineOutboxTasks.Add(new OutlineOutboxTask
        {
            TaskType = OutlineTaskType.DeleteMail,
            Email = "future@example.com",
            NextAttemptAt = DateTimeOffset.UtcNow.AddMinutes(10),
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();

        var provider = CreateServiceProvider(db);
        var worker = new OutlineOutboxWorker(provider, _logger);

        // Act
        var result = await worker.TryProcessNextTaskAsync(CancellationToken.None);

        // Assert
        Assert.False(result);
        await _outlineService.DidNotReceive().DeleteMailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task TryProcessNextTaskAsync_ProcessesSyncMailSuccessfully()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        db.OutlineOutboxTasks.Add(new OutlineOutboxTask
        {
            TaskType = OutlineTaskType.SyncMail,
            Email = "old@example.com",
            NewEmail = "new@example.com",
            NextAttemptAt = DateTimeOffset.UtcNow.AddMinutes(-1),
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-1)
        });
        await db.SaveChangesAsync();

        var provider = CreateServiceProvider(db);
        var worker = new OutlineOutboxWorker(provider, _logger);

        // Act
        var result = await worker.TryProcessNextTaskAsync(CancellationToken.None);

        // Assert
        Assert.True(result);
        await _outlineService.Received(1).SyncMailAsync("old@example.com", "new@example.com", Arg.Any<CancellationToken>());
        Assert.Empty(await db.OutlineOutboxTasks.ToListAsync());
    }

    [Fact]
    public async Task TryProcessNextTaskAsync_ProcessesDeleteMailSuccessfully()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        db.OutlineOutboxTasks.Add(new OutlineOutboxTask
        {
            TaskType = OutlineTaskType.DeleteMail,
            Email = "delete@example.com",
            NextAttemptAt = DateTimeOffset.UtcNow.AddMinutes(-1),
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-1)
        });
        await db.SaveChangesAsync();

        var provider = CreateServiceProvider(db);
        var worker = new OutlineOutboxWorker(provider, _logger);

        // Act
        var result = await worker.TryProcessNextTaskAsync(CancellationToken.None);

        // Assert
        Assert.True(result);
        await _outlineService.Received(1).DeleteMailAsync("delete@example.com", Arg.Any<CancellationToken>());
        Assert.Empty(await db.OutlineOutboxTasks.ToListAsync());
    }

    [Fact]
    public async Task TryProcessNextTaskAsync_ProcessesPromoteAdminSuccessfully()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        db.OutlineOutboxTasks.Add(new OutlineOutboxTask
        {
            TaskType = OutlineTaskType.PromoteAdmin,
            Email = "admin@example.com",
            NextAttemptAt = DateTimeOffset.UtcNow.AddMinutes(-1),
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-1)
        });
        await db.SaveChangesAsync();

        var provider = CreateServiceProvider(db);
        var worker = new OutlineOutboxWorker(provider, _logger);

        // Act
        var result = await worker.TryProcessNextTaskAsync(CancellationToken.None);

        // Assert
        Assert.True(result);
        await _outlineService.Received(1).UpdateAdminStatusAsync("admin@example.com", true, Arg.Any<CancellationToken>());
        Assert.Empty(await db.OutlineOutboxTasks.ToListAsync());
    }

    [Fact]
    public async Task TryProcessNextTaskAsync_ProcessesDemoteAdminSuccessfully()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        db.OutlineOutboxTasks.Add(new OutlineOutboxTask
        {
            TaskType = OutlineTaskType.DemoteAdmin,
            Email = "demote@example.com",
            NextAttemptAt = DateTimeOffset.UtcNow.AddMinutes(-1),
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-1)
        });
        await db.SaveChangesAsync();

        var provider = CreateServiceProvider(db);
        var worker = new OutlineOutboxWorker(provider, _logger);

        // Act
        var result = await worker.TryProcessNextTaskAsync(CancellationToken.None);

        // Assert
        Assert.True(result);
        await _outlineService.Received(1).UpdateAdminStatusAsync("demote@example.com", false, Arg.Any<CancellationToken>());
        Assert.Empty(await db.OutlineOutboxTasks.ToListAsync());
    }

    [Fact]
    public async Task TryProcessNextTaskAsync_WhenThrowsException_IncrementsRetryCountAndSchedulesNextAttempt()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        db.OutlineOutboxTasks.Add(new OutlineOutboxTask
        {
            TaskType = OutlineTaskType.DeleteMail,
            Email = "fail@example.com",
            RetryCount = 1,
            NextAttemptAt = DateTimeOffset.UtcNow.AddMinutes(-1),
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-10)
        });
        await db.SaveChangesAsync();

        _outlineService.DeleteMailAsync("fail@example.com", Arg.Any<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network failure"));

        var provider = CreateServiceProvider(db);
        var worker = new OutlineOutboxWorker(provider, _logger);

        var beforeRun = DateTimeOffset.UtcNow;

        // Act
        var result = await worker.TryProcessNextTaskAsync(CancellationToken.None);

        // Assert
        Assert.True(result);
        var tasks = await db.OutlineOutboxTasks.ToListAsync();
        Assert.Single(tasks);
        Assert.Equal(2, tasks[0].RetryCount);
        // RetryCount was incremented from 1 to 2, 2^2 = 4 minutes added
        Assert.True(tasks[0].NextAttemptAt >= beforeRun.AddMinutes(3.9));
    }
}
