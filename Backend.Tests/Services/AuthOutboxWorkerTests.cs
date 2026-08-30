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

public class AuthOutboxWorkerTests
{
    private readonly DbContextOptions<PostgresDbContext> _dbOptions;
    private readonly IAuthService _authService;
    private readonly ILogger<AuthOutboxWorker> _logger;

    public AuthOutboxWorkerTests()
    {
        _dbOptions = new DbContextOptionsBuilder<PostgresDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _authService = Substitute.For<IAuthService>();
        _logger = NullLogger<AuthOutboxWorker>.Instance;
    }

    private ServiceProvider CreateServiceProvider(PostgresDbContext db)
    {
        var services = new ServiceCollection();
        services.AddSingleton(db);
        services.AddSingleton(_authService);
        return services.BuildServiceProvider();
    }

    private class TestableAuthOutboxWorker(IServiceProvider serviceProvider, ILogger<AuthOutboxWorker> logger)
        : AuthOutboxWorker(serviceProvider, logger)
    {
        public Task<bool> PublicTryProcessNextTaskAsync(CancellationToken ct)
        {
            var method = typeof(AuthOutboxWorker)
                .GetMethod("TryProcessNextTaskAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (method == null)
            {
                throw new InvalidOperationException("TryProcessNextTaskAsync method not found");
            }
            return (Task<bool>)method.Invoke(this, [ct])!;
        }
    }

    private Member CreateTestMember(Guid id, Guid? authSystemUserId = null)
    {
        return new Member
        {
            Id = id,
            AuthSystemUserId = authSystemUserId,
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
    public async Task EnqueueTask_SavesTaskToDatabase()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        db.Database.EnsureCreated();
        var provider = CreateServiceProvider(db);
        var worker = new AuthOutboxWorker(provider, _logger);

        var userId = Guid.NewGuid();

        // Act
        worker.EnqueueTask(AuthTaskType.Create, userId, db);

        // Assert
        var tasks = await db.AuthOutboxTasks.ToListAsync();
        Assert.Single(tasks);
        Assert.Equal(AuthTaskType.Create, tasks[0].TaskType);
        Assert.Equal(userId, tasks[0].AuthSystemUserId);
        Assert.Equal(0, tasks[0].RetryCount);
    }

    [Fact]
    public async Task TryProcessNextTaskAsync_NoTasks_ReturnsFalse()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        db.Database.EnsureCreated();
        var provider = CreateServiceProvider(db);
        var worker = new TestableAuthOutboxWorker(provider, _logger);

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

        db.AuthOutboxTasks.Add(new AuthOutboxTask
        {
            TaskType = AuthTaskType.Sync,
            AuthSystemUserId = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow,
            NextAttemptAt = DateTimeOffset.UtcNow.AddMinutes(5)
        });
        await db.SaveChangesAsync();

        var provider = CreateServiceProvider(db);
        var worker = new TestableAuthOutboxWorker(provider, _logger);

        // Act
        var result = await worker.PublicTryProcessNextTaskAsync(CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task TryProcessNextTaskAsync_CreateTask_MemberFound_InvokesServiceAndUpdatesMember()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        db.Database.EnsureCreated();

        var memberId = Guid.NewGuid();
        var member = CreateTestMember(memberId);
        db.Members.Add(member);

        var task = new AuthOutboxTask
        {
            TaskType = AuthTaskType.Create,
            AuthSystemUserId = memberId,
            CreatedAt = DateTimeOffset.UtcNow,
            NextAttemptAt = DateTimeOffset.UtcNow
        };
        db.AuthOutboxTasks.Add(task);
        await db.SaveChangesAsync();

        var newAuthId = Guid.NewGuid();
        _authService.CreateUser(Arg.Any<Member>()).Returns(Task.FromResult<Guid?>(newAuthId));

        var provider = CreateServiceProvider(db);
        var worker = new TestableAuthOutboxWorker(provider, _logger);

        // Act
        var result = await worker.PublicTryProcessNextTaskAsync(CancellationToken.None);

        // Assert
        Assert.True(result);
        await _authService.Received(1).CreateUser(Arg.Is<Member>(m => m.Id == memberId));

        var updatedMember = await db.Members.FindAsync(memberId);
        Assert.NotNull(updatedMember);
        Assert.Equal(newAuthId, updatedMember.AuthSystemUserId);

        var tasks = await db.AuthOutboxTasks.ToListAsync();
        var queuedTask = Assert.Single(tasks); // Create task removed, catch-up Sync task queued
        Assert.Equal(AuthTaskType.Sync, queuedTask.TaskType);
        Assert.Equal(memberId, queuedTask.AuthSystemUserId); // Sync tasks carry the member's local ID
    }

    [Fact]
    public async Task TryProcessNextTaskAsync_CreateTask_MemberNotFound_RetriesInsteadOfDroppingTask()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        db.Database.EnsureCreated();

        var memberId = Guid.NewGuid();
        var task = new AuthOutboxTask
        {
            TaskType = AuthTaskType.Create,
            AuthSystemUserId = memberId,
            CreatedAt = DateTimeOffset.UtcNow,
            NextAttemptAt = DateTimeOffset.UtcNow
        };
        db.AuthOutboxTasks.Add(task);
        await db.SaveChangesAsync();

        var provider = CreateServiceProvider(db);
        var worker = new TestableAuthOutboxWorker(provider, _logger);

        // Act
        var result = await worker.PublicTryProcessNextTaskAsync(CancellationToken.None);

        // Assert
        Assert.True(result);
        await _authService.DidNotReceiveWithAnyArgs().CreateUser(default!);

        var tasks = await db.AuthOutboxTasks.ToListAsync();
        var remainingTask = Assert.Single(tasks); // Not dropped - rescheduled for retry
        Assert.Equal(1, remainingTask.RetryCount);
        Assert.True(remainingTask.NextAttemptAt > DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task TryProcessNextTaskAsync_SyncTask_InvokesService()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        db.Database.EnsureCreated();

        var memberId = Guid.NewGuid();
        var authUserId = Guid.NewGuid();
        var member = CreateTestMember(memberId, authUserId);
        db.Members.Add(member);

        var task = new AuthOutboxTask
        {
            TaskType = AuthTaskType.Sync,
            AuthSystemUserId = memberId,
            CreatedAt = DateTimeOffset.UtcNow,
            NextAttemptAt = DateTimeOffset.UtcNow
        };
        db.AuthOutboxTasks.Add(task);
        await db.SaveChangesAsync();

        var provider = CreateServiceProvider(db);
        var worker = new TestableAuthOutboxWorker(provider, _logger);

        // Act
        var result = await worker.PublicTryProcessNextTaskAsync(CancellationToken.None);

        // Assert
        Assert.True(result);
        await _authService.Received(1).SyncMember(authUserId);

        var tasks = await db.AuthOutboxTasks.ToListAsync();
        Assert.Empty(tasks); // Removed
    }

    [Fact]
    public async Task TryProcessNextTaskAsync_SyncTask_MemberNotLinkedYet_CreatesInsteadOfFailing()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        db.Database.EnsureCreated();

        var memberId = Guid.NewGuid();
        var member = CreateTestMember(memberId); // no AuthSystemUserId yet
        db.Members.Add(member);

        var task = new AuthOutboxTask
        {
            TaskType = AuthTaskType.Sync,
            AuthSystemUserId = memberId,
            CreatedAt = DateTimeOffset.UtcNow,
            NextAttemptAt = DateTimeOffset.UtcNow
        };
        db.AuthOutboxTasks.Add(task);
        await db.SaveChangesAsync();

        var newAuthId = Guid.NewGuid();
        _authService.CreateUser(Arg.Any<Member>()).Returns(Task.FromResult<Guid?>(newAuthId));

        var provider = CreateServiceProvider(db);
        var worker = new TestableAuthOutboxWorker(provider, _logger);

        // Act
        var result = await worker.PublicTryProcessNextTaskAsync(CancellationToken.None);

        // Assert
        Assert.True(result);
        await _authService.Received(1).CreateUser(Arg.Is<Member>(m => m.Id == memberId));
        await _authService.DidNotReceiveWithAnyArgs().SyncMember(default);

        var updatedMember = await db.Members.FindAsync(memberId);
        Assert.NotNull(updatedMember);
        Assert.Equal(newAuthId, updatedMember.AuthSystemUserId);

        var tasks = await db.AuthOutboxTasks.ToListAsync();
        var queuedTask = Assert.Single(tasks); // Sync task removed, catch-up Sync task queued
        Assert.Equal(AuthTaskType.Sync, queuedTask.TaskType);
        Assert.Equal(memberId, queuedTask.AuthSystemUserId);
    }

    [Fact]
    public async Task TryProcessNextTaskAsync_DeleteTask_MemberFound_InvokesService()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        db.Database.EnsureCreated();

        var memberId = Guid.NewGuid();
        var authUserId = Guid.NewGuid();
        var member = CreateTestMember(memberId, authUserId);
        db.Members.Add(member);

        var task = new AuthOutboxTask
        {
            TaskType = AuthTaskType.Delete,
            AuthSystemUserId = authUserId,
            CreatedAt = DateTimeOffset.UtcNow,
            NextAttemptAt = DateTimeOffset.UtcNow
        };
        db.AuthOutboxTasks.Add(task);
        await db.SaveChangesAsync();

        var provider = CreateServiceProvider(db);
        var worker = new TestableAuthOutboxWorker(provider, _logger);

        // Act
        var result = await worker.PublicTryProcessNextTaskAsync(CancellationToken.None);

        // Assert
        Assert.True(result);
        await _authService.Received(1).DeleteUser(authUserId);

        var tasks = await db.AuthOutboxTasks.ToListAsync();
        Assert.Empty(tasks); // Removed
    }

    [Fact]
    public async Task TryProcessNextTaskAsync_DeleteTask_MemberAlreadySoftDeletedLocally_StillDeletesFromAuthSystem()
    {
        // Arrange: the member enqueuing this Delete task has already been soft-deleted (IsDeleted = true)
        // by the same transaction that queued it, so it's invisible to Member's global query filter. The
        // task must not depend on looking the member back up - task.AuthSystemUserId is already correct.
        using var db = new PostgresDbContext(_dbOptions);
        db.Database.EnsureCreated();

        var authUserId = Guid.NewGuid();
        var task = new AuthOutboxTask
        {
            TaskType = AuthTaskType.Delete,
            AuthSystemUserId = authUserId,
            CreatedAt = DateTimeOffset.UtcNow,
            NextAttemptAt = DateTimeOffset.UtcNow
        };
        db.AuthOutboxTasks.Add(task);
        await db.SaveChangesAsync();

        var provider = CreateServiceProvider(db);
        var worker = new TestableAuthOutboxWorker(provider, _logger);

        // Act
        var result = await worker.PublicTryProcessNextTaskAsync(CancellationToken.None);

        // Assert
        Assert.True(result);
        await _authService.Received(1).DeleteUser(authUserId);

        var tasks = await db.AuthOutboxTasks.ToListAsync();
        Assert.Empty(tasks); // Removed
    }

    [Fact]
    public async Task TryProcessNextTaskAsync_RefreshEmailTask_InvokesService()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        db.Database.EnsureCreated();

        var memberId = Guid.NewGuid();
        var authUserId = Guid.NewGuid();
        var member = CreateTestMember(memberId, authUserId);
        db.Members.Add(member);

        var task = new AuthOutboxTask
        {
            TaskType = AuthTaskType.RefreshEmail,
            AuthSystemUserId = memberId,
            CreatedAt = DateTimeOffset.UtcNow,
            NextAttemptAt = DateTimeOffset.UtcNow
        };
        db.AuthOutboxTasks.Add(task);
        await db.SaveChangesAsync();

        var provider = CreateServiceProvider(db);
        var worker = new TestableAuthOutboxWorker(provider, _logger);

        // Act
        var result = await worker.PublicTryProcessNextTaskAsync(CancellationToken.None);

        // Assert
        Assert.True(result);
        await _authService.Received(1).RefreshEmail(authUserId);

        var tasks = await db.AuthOutboxTasks.ToListAsync();
        Assert.Empty(tasks); // Removed
    }

    [Fact]
    public async Task TryProcessNextTaskAsync_SendActivationEmailTask_InvokesService()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        db.Database.EnsureCreated();

        var memberId = Guid.NewGuid();
        var authUserId = Guid.NewGuid();
        var member = CreateTestMember(memberId, authUserId);
        db.Members.Add(member);

        var task = new AuthOutboxTask
        {
            TaskType = AuthTaskType.SendActivationEmail,
            AuthSystemUserId = memberId,
            CreatedAt = DateTimeOffset.UtcNow,
            NextAttemptAt = DateTimeOffset.UtcNow
        };
        db.AuthOutboxTasks.Add(task);
        await db.SaveChangesAsync();

        var provider = CreateServiceProvider(db);
        var worker = new TestableAuthOutboxWorker(provider, _logger);

        // Act
        var result = await worker.PublicTryProcessNextTaskAsync(CancellationToken.None);

        // Assert
        Assert.True(result);
        await _authService.Received(1).SendActivationEmail(authUserId);

        var tasks = await db.AuthOutboxTasks.ToListAsync();
        Assert.Empty(tasks); // Removed
    }

    [Fact]
    public async Task TryProcessNextTaskAsync_UnknownTaskType_ReschedulesWithBackoff()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        db.Database.EnsureCreated();

        var authUserId = Guid.NewGuid();
        var task = new AuthOutboxTask
        {
            TaskType = (AuthTaskType)99,
            AuthSystemUserId = authUserId,
            CreatedAt = DateTimeOffset.UtcNow,
            NextAttemptAt = DateTimeOffset.UtcNow,
            RetryCount = 0
        };
        db.AuthOutboxTasks.Add(task);
        await db.SaveChangesAsync();

        var provider = CreateServiceProvider(db);
        var worker = new TestableAuthOutboxWorker(provider, _logger);

        // Act
        var result = await worker.PublicTryProcessNextTaskAsync(CancellationToken.None);

        // Assert
        Assert.True(result);
        var updatedTasks = await db.AuthOutboxTasks.ToListAsync();
        Assert.Single(updatedTasks);
        Assert.Equal(1, updatedTasks[0].RetryCount);
    }

    [Fact]
    public async Task TryProcessNextTaskAsync_TaskFails_ReschedulesWithBackoff()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        db.Database.EnsureCreated();

        var authUserId = Guid.NewGuid();
        var task = new AuthOutboxTask
        {
            TaskType = AuthTaskType.Sync,
            AuthSystemUserId = authUserId,
            CreatedAt = DateTimeOffset.UtcNow,
            NextAttemptAt = DateTimeOffset.UtcNow,
            RetryCount = 1
        };
        db.AuthOutboxTasks.Add(task);
        await db.SaveChangesAsync();

        _authService.SyncMember(authUserId).Returns(Task.FromException(new Exception("Sync failed")));

        var provider = CreateServiceProvider(db);
        var worker = new TestableAuthOutboxWorker(provider, _logger);

        // Act
        var result = await worker.PublicTryProcessNextTaskAsync(CancellationToken.None);

        // Assert
        Assert.True(result); // Processed a task
        
        var updatedTasks = await db.AuthOutboxTasks.ToListAsync();
        Assert.Single(updatedTasks);
        Assert.Equal(2, updatedTasks[0].RetryCount); // Retry count incremented
        Assert.True(updatedTasks[0].NextAttemptAt > DateTimeOffset.UtcNow); // Rescheduled in the future
    }

    [Fact]
    public async Task ExecuteAsync_ProcessesTasksAndDelays()
    {
        // Arrange
        using var db = new PostgresDbContext(_dbOptions);
        db.Database.EnsureCreated();
        var provider = CreateServiceProvider(db);
        var worker = new AuthOutboxWorker(provider, _logger);

        // Act
        var cts = new CancellationTokenSource();
        var startTask = worker.StartAsync(cts.Token);
        
        await Task.Delay(100);
        
        cts.Cancel();
        await worker.StopAsync(CancellationToken.None);
        await startTask;
    }
}
