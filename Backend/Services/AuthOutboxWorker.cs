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
    /// Enqueues an auth outbox task on the caller's own database context, so it's only persisted once the
    /// caller's surrounding transaction (if any) commits - instead of on a separate connection that would commit
    /// immediately and could become visible to (and get picked up and discarded by) the background worker before
    /// the entity the task depends on (e.g. a newly created Member) has actually been committed.
    ///
    /// For every task type except Delete: pass the member's local ID, regardless of whether that member has been
    /// linked to the auth system yet - never member.AuthSystemUserId. The worker resolves the actual auth-system
    /// user ID itself when it processes the task (creating one first if needed for a Sync). For Delete: pass
    /// member.AuthSystemUserId directly, since the member row may no longer be resolvable by the time the task runs.
    /// </summary>
    /// <param name="taskType">The task type to enqueue.</param>
    /// <param name="id">The member's local ID for every task type except Delete; the auth-system user ID for Delete.</param>
    /// <param name="db">The database context used to persist the task.</param>
    public virtual void EnqueueTask(AuthTaskType taskType, Guid id, PostgresDbContext db)
    {
        var task = new AuthOutboxTask
        {
            TaskType = taskType,
            AuthSystemUserId = id,
            CreatedAt = DateTimeOffset.UtcNow,
            NextAttemptAt = DateTimeOffset.UtcNow,
            RetryCount = 0
        };

        db.AuthOutboxTasks.Add(task);
        db.SaveChanges();
        logger.LogInformation("Enqueued auth outbox task {TaskType} for {Id}.", taskType, id);
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
        if (task.TaskType == AuthTaskType.Delete)
        {
            // No member lookup here on purpose - see AuthOutboxTask.AuthSystemUserId's doc comment.
            await syncService.DeleteUser(task.AuthSystemUserId);
            return;
        }

        var member = await db.Members.FindAsync([task.AuthSystemUserId], ct)
            // The member row can briefly be invisible to this task's own connection if it was inserted
            // in a transaction that hadn't committed yet when this task became eligible to run. Retry
            // instead of silently dropping the task, rather than permanently losing the auth link.
            ?? throw new InvalidOperationException($"Member {task.AuthSystemUserId} was not found (yet); will retry.");

        switch (task.TaskType)
        {
            case AuthTaskType.Create:
                await CreateAuthSystemUser(db, syncService, member, ct);
                break;

            case AuthTaskType.Sync:
                if (member.AuthSystemUserId == null)
                {
                    // Not linked to the auth system yet (e.g. this member's own Create task hasn't run,
                    // or was never enqueued in the first place). Create it instead of failing outright -
                    // CreateAuthSystemUser queues a follow-up Sync once the user actually exists, which
                    // picks up exactly the state this Sync was trying to push.
                    await CreateAuthSystemUser(db, syncService, member, ct);
                    break;
                }
                await syncService.SyncMember(member.AuthSystemUserId.Value);
                break;

            case AuthTaskType.RefreshEmail:
                await syncService.RefreshEmail(RequireAuthSystemUserId(member));
                break;

            case AuthTaskType.SendActivationEmail:
                await syncService.SendActivationEmail(RequireAuthSystemUserId(member));
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(task.TaskType), "Unknown auth task type");
        }
    }

    private async Task CreateAuthSystemUser(PostgresDbContext db, IAuthService syncService, Member member, CancellationToken ct)
    {
        var kId = await syncService.CreateUser(member);
        if (kId == null) return;

        member.AuthSystemUserId = kId;
        logger.LogInformation("Linked local member {MemberId} to auth user {AuthSystemUserId}.", member.Id, kId);

        // Catch up any auth-system state (e.g. membership/payment status) that couldn't be synced
        // while this member had no AuthSystemUserId yet.
        db.AuthOutboxTasks.Add(new AuthOutboxTask
        {
            TaskType = AuthTaskType.Sync,
            AuthSystemUserId = member.Id,
            CreatedAt = DateTimeOffset.UtcNow,
            NextAttemptAt = DateTimeOffset.UtcNow
        });
    }

    private static Guid RequireAuthSystemUserId(Member member) =>
        member.AuthSystemUserId ?? throw new InvalidOperationException($"Member {member.Id} does not have an authentication system ID.");

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
