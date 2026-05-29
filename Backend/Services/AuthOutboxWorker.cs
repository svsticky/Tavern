using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

/// <summary>
/// Processes queued auth-system tasks in the background.
/// </summary>
public class AuthOutboxWorker(
    IServiceProvider serviceProvider, 
    ILogger<AuthOutboxWorker> logger) : BackgroundService
{
    /// <summary>
    /// Enqueues a auth outbox task for asynchronous processing.
    /// </summary>
    /// <param name="taskType">The task type to enqueue.</param>
    /// <param name="authSystemUserId">The target auth user ID.</param>
    public virtual async Task EnqueueTask(AuthTaskType taskType, Guid authSystemUserId)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PostgresDbContext>();

        var task = new AuthOutboxTask
        {
            TaskType = taskType,
            AuthSystemUserId = authSystemUserId,
            CreatedAt = DateTimeOffset.UtcNow,
            NextAttemptAt = DateTimeOffset.UtcNow,
            RetryCount = 0
        };

        db.AuthOutboxTasks.Add(task);
        await db.SaveChangesAsync();
        logger.LogInformation("Enqueued auth outbox task {TaskType} for {AuthSystemUserId}.", taskType, authSystemUserId);
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Auth outbox worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            bool processed = await TryProcessNextTaskAsync(stoppingToken);

            if (!processed)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }

        logger.LogInformation("Auth outbox worker stopped.");
    }

    private async Task<bool> TryProcessNextTaskAsync(CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PostgresDbContext>();
        
        var task = await db.AuthOutboxTasks
            .OrderBy(t => t.CreatedAt)
            .ThenBy(t => t.Id)
            .FirstOrDefaultAsync(ct);

        if (task == null) return false;
        if (task.NextAttemptAt > DateTimeOffset.UtcNow) return false;
        logger.LogInformation("Processing auth outbox task {TaskType} for {AuthSystemUserId}. Retry {RetryCount}.",
            task.TaskType, task.AuthSystemUserId, task.RetryCount);

        var syncService = scope.ServiceProvider.GetRequiredService<IAuthService>();

        try
        {
            await HandleTaskAsync(db, syncService, task, ct);
            
            db.AuthOutboxTasks.Remove(task);
            logger.LogInformation("Completed auth outbox task {TaskType} for {AuthSystemUserId}.", task.TaskType, task.AuthSystemUserId);
        }
        catch (Exception ex)
        {
            HandleFailure(task, ex);
        }

        await db.SaveChangesAsync(ct);
        return true;
    }

    private async Task HandleTaskAsync(PostgresDbContext db, IAuthService syncService, AuthOutboxTask task, CancellationToken ct)
    {
        switch (task.TaskType)
        {
            case AuthTaskType.Create:
                var member = await db.Members.FindAsync([task.AuthSystemUserId], ct);
                if (member != null)
                {
                    var kId = await syncService.CreateUser(member);
                    if (kId != null)
                    {
                        member.AuthSystemUserId = kId;
                        logger.LogInformation("Linked local member {MemberId} to auth user {AuthSystemUserId}.", member.Id, kId);
                    }
                }
                else
                {
                    logger.LogWarning("Auth create task skipped: member {MemberId} was not found.", task.AuthSystemUserId);
                }
                break;

            case AuthTaskType.Sync:
                await syncService.SyncMember(task.AuthSystemUserId);
                break;

            case AuthTaskType.Delete:
                var memberToDelete = await db.Members.FirstOrDefaultAsync(m => m.AuthSystemUserId == task.AuthSystemUserId, ct);
                if (memberToDelete?.AuthSystemUserId != null)
                {
                    await syncService.DeleteUser(memberToDelete.AuthSystemUserId.Value);
                }
                else
                {
                    logger.LogWarning("Auth delete task skipped: user {AuthSystemUserId} was not found in local members.", task.AuthSystemUserId);
                }
                break;

            case AuthTaskType.RefreshEmail:
                await syncService.RefreshEmail(task.AuthSystemUserId);
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(task.TaskType), "Unknown auth task type");
        }
    }

    private void HandleFailure(AuthOutboxTask task, Exception ex)
    {
        logger.LogError(ex, "Auth sync failed for {Id}. Retry count: {Count}", task.AuthSystemUserId, task.RetryCount);

        task.RetryCount++;
        
        // Exponential backoff with a max delay of 1 minute
        double extraSeconds = Math.Min(Math.Pow(2, task.RetryCount), 60);
        task.NextAttemptAt = DateTimeOffset.UtcNow.AddSeconds(extraSeconds);
        logger.LogWarning("Rescheduled Auth task {TaskType} for {AuthSystemUserId} at {NextRunUtc}.",
            task.TaskType, task.AuthSystemUserId, task.NextAttemptAt);
    }
}
