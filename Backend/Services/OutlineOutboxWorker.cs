using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

/// <summary>
/// Background worker that processes queued Outline synchronization tasks.
/// </summary>
public class OutlineOutboxWorker(
    IServiceProvider serviceProvider,
    ILogger<OutlineOutboxWorker> logger) : BackgroundService, IMailSyncOutboxWorker, IAdminStatusUpdateOutboxWorker
{
    /// <inheritdoc />
    public virtual void EnqueueSyncMail(string oldEmail, string newEmail, PostgresDbContext db)
    {
        var task = new OutlineOutboxTask
        {
            TaskType = OutlineTaskType.SyncMail,
            Email = oldEmail,
            NewEmail = newEmail,
            CreatedAt = DateTimeOffset.UtcNow,
            NextAttemptAt = DateTimeOffset.UtcNow,
            RetryCount = 0
        };

        db.OutlineOutboxTasks.Add(task);
        db.SaveChanges();
        logger.LogInformation("Enqueued Outline email migration task from {OldEmail} to {NewEmail}.", oldEmail, newEmail);
    }

    /// <inheritdoc />
    public virtual void EnqueueDeleteMail(string email, PostgresDbContext db)
    {
        var task = new OutlineOutboxTask
        {
            TaskType = OutlineTaskType.DeleteMail,
            Email = email,
            CreatedAt = DateTimeOffset.UtcNow,
            NextAttemptAt = DateTimeOffset.UtcNow,
            RetryCount = 0
        };

        db.OutlineOutboxTasks.Add(task);
        db.SaveChanges();
        logger.LogInformation("Enqueued Outline user deletion task for {Email}.", email);
    }

    /// <inheritdoc />
    public virtual void EnqueueAdminStatusUpdate(string email, bool isAdmin, PostgresDbContext db)
    {
        var task = new OutlineOutboxTask
        {
            TaskType = isAdmin ? OutlineTaskType.PromoteAdmin : OutlineTaskType.DemoteAdmin,
            Email = email,
            CreatedAt = DateTimeOffset.UtcNow,
            NextAttemptAt = DateTimeOffset.UtcNow,
            RetryCount = 0
        };

        db.OutlineOutboxTasks.Add(task);
        db.SaveChanges();
        logger.LogInformation("Enqueued Outline admin status update task for {Email} (isAdmin: {IsAdmin}).", email, isAdmin);
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Outline outbox worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<PostgresDbContext>();
                var isEnabled = db.Settings.FirstOrDefault(s => s.Name == "OutlineEnabled")?.Value?.Trim().Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;

                if (!isEnabled)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                    continue;
                }
            }

            bool processed = await TryProcessNextTaskAsync(stoppingToken);

            if (!processed)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }

        logger.LogInformation("Outline outbox worker stopped.");
    }

    /// <summary>
    /// Attempts to process the next pending Outline outbox task.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>True if a task was processed; otherwise, false.</returns>
    public async Task<bool> TryProcessNextTaskAsync(CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PostgresDbContext>();

        var task = await db.OutlineOutboxTasks
            .OrderBy(t => t.CreatedAt)
            .ThenBy(t => t.Id)
            .FirstOrDefaultAsync(ct);

        if (task == null) return false;
        if (task.NextAttemptAt > DateTimeOffset.UtcNow) return false;

        logger.LogInformation("Processing Outline outbox task {TaskType} for {Email}. Retry {RetryCount}.", task.TaskType, task.Email, task.RetryCount);

        var outlineService = scope.ServiceProvider.GetRequiredService<IOutlineService>();

        try
        {
            await HandleTaskAsync(outlineService, task, ct);
            db.OutlineOutboxTasks.Remove(task);
            logger.LogInformation("Completed Outline outbox task {TaskType} for {Email}.", task.TaskType, task.Email);
        }
        catch (Exception ex)
        {
            HandleFailure(task, ex);
        }

        await db.SaveChangesAsync(ct);
        return true;
    }

    private static async Task HandleTaskAsync(IOutlineService service, OutlineOutboxTask task, CancellationToken ct)
    {
        switch (task.TaskType)
        {
            case OutlineTaskType.SyncMail:
                if (string.IsNullOrWhiteSpace(task.NewEmail))
                {
                    throw new InvalidOperationException("SyncMail task is missing NewEmail.");
                }
                await service.SyncMailAsync(task.Email, task.NewEmail, ct);
                break;

            case OutlineTaskType.DeleteMail:
                await service.DeleteMailAsync(task.Email, ct);
                break;

            case OutlineTaskType.PromoteAdmin:
                await service.UpdateAdminStatusAsync(task.Email, true, ct);
                break;

            case OutlineTaskType.DemoteAdmin:
                await service.UpdateAdminStatusAsync(task.Email, false, ct);
                break;

            default:
                throw new NotSupportedException($"Unsupported Outline outbox task type '\''{task.TaskType}'\''.");
        }
    }

    private void HandleFailure(OutlineOutboxTask task, Exception ex)
    {
        logger.LogError(ex, "Outline sync failed for {Email}. Retry count: {Retry}", task.Email, task.RetryCount);

        task.RetryCount++;
        double extraMinutes = Math.Min(Math.Pow(2, task.RetryCount), 60);
        task.NextAttemptAt = DateTimeOffset.UtcNow.AddMinutes(extraMinutes);
        logger.LogWarning("Rescheduled Outline task for {Email} at {NextRunUtc}.", task.Email, task.NextAttemptAt);
    }
}
