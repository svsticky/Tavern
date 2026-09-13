using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Backend.Services.OutboxWorkers;

/// <summary>
/// Background worker that processes queued mail subscription tasks.
/// </summary>
public class MailSubscriptionOutboxWorker(
    IServiceProvider serviceProvider,
    ILogger<MailSubscriptionOutboxWorker> logger) : BackgroundService
{

    /// <summary>
    /// Enqueues a task that replaces a member's mailing list subscriptions, carrying the name along when known.
    /// </summary>
    /// <param name="email">The email address to process.</param>
    /// <param name="subscribedListIds">The IDs of the mailing lists the member should be subscribed to.</param>
    /// <param name="db">The database context used to persist the task.</param>
    /// <param name="firstName">The member's first name, when known.</param>
    /// <param name="lastName">The member's last name, when known.</param>
    public virtual void EnqueueUpdateSubscriptionsTask(string email, IEnumerable<string> subscribedListIds, PostgresDbContext db, string? firstName = null, string? lastName = null)
    {
        var task = new MailSubscriptionOutboxTask
        {
            TaskType = MailSubscriptionOutboxTaskType.UpdateSubscriptions,
            Email = email,
            SubscribedListIdsJson = JsonSerializer.Serialize(subscribedListIds.ToList()),
            FirstName = firstName,
            LastName = lastName,
            CreatedAt = DateTimeOffset.UtcNow,
            NextAttemptAt = DateTimeOffset.UtcNow,
            RetryCount = 0
        };

        db.MailSubscriptionOutboxTasks.Add(task);
        db.SaveChanges();
        logger.LogInformation("Enqueued mail subscription update task for email {Email}.", email);
    }

    /// <summary>
    /// Enqueues a task that removes a member from the mail subscription provider.
    /// </summary>
    /// <param name="email">The email address to remove.</param>
    /// <param name="db">The database context used to persist the task.</param>
    public virtual void EnqueueDeleteTask(string email, PostgresDbContext db)
    {
        var task = new MailSubscriptionOutboxTask
        {
            TaskType = MailSubscriptionOutboxTaskType.Delete,
            Email = email,
            CreatedAt = DateTimeOffset.UtcNow,
            NextAttemptAt = DateTimeOffset.UtcNow,
            RetryCount = 0
        };

        db.MailSubscriptionOutboxTasks.Add(task);
        db.SaveChanges();
        logger.LogInformation("Enqueued mail subscription delete task for email {Email}.", email);
    }

    /// <summary>
    /// Enqueues a task that moves a member's mail subscriptions from an old email address to a new
    /// one, carrying the name along when known.
    /// </summary>
    /// <param name="oldEmail">The member's previous email address.</param>
    /// <param name="newEmail">The member's new email address.</param>
    /// <param name="db">The database context used to persist the task.</param>
    /// <param name="firstName">The member's first name, when known.</param>
    /// <param name="lastName">The member's last name, when known.</param>
    public virtual void EnqueueMigrateEmailTask(string oldEmail, string newEmail, PostgresDbContext db, string? firstName = null, string? lastName = null)
    {
        var task = new MailSubscriptionOutboxTask
        {
            TaskType = MailSubscriptionOutboxTaskType.MigrateEmail,
            Email = newEmail,
            OldEmail = oldEmail,
            FirstName = firstName,
            LastName = lastName,
            CreatedAt = DateTimeOffset.UtcNow,
            NextAttemptAt = DateTimeOffset.UtcNow,
            RetryCount = 0
        };

        db.MailSubscriptionOutboxTasks.Add(task);
        db.SaveChanges();
        logger.LogInformation("Enqueued mail subscription email migration task from {OldEmail} to {NewEmail}.", oldEmail, newEmail);
    }

    /// <summary>
    /// Enqueues a task that updates a member's first/last name merge fields, without touching their subscriptions.
    /// </summary>
    /// <param name="email">The email address to process.</param>
    /// <param name="firstName">The member's first name.</param>
    /// <param name="lastName">The member's last name.</param>
    /// <param name="db">The database context used to persist the task.</param>
    public virtual void EnqueueUpdateNameTask(string email, string firstName, string lastName, PostgresDbContext db)
    {
        var task = new MailSubscriptionOutboxTask
        {
            TaskType = MailSubscriptionOutboxTaskType.UpdateName,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            CreatedAt = DateTimeOffset.UtcNow,
            NextAttemptAt = DateTimeOffset.UtcNow,
            RetryCount = 0
        };

        db.MailSubscriptionOutboxTasks.Add(task);
        db.SaveChanges();
        logger.LogInformation("Enqueued mail subscription name update task for email {Email}.", email);
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Mailsubscription outbox worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<PostgresDbContext>();
                var isEnabled = !string.IsNullOrWhiteSpace(db.Settings.FirstOrDefault(s => s.Name == "MailSubscriptionService")?.Value);

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

        logger.LogInformation("Mailsubscription outbox worker stopped.");
    }

    private async Task<bool> TryProcessNextTaskAsync(CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PostgresDbContext>();

        var task = await db.MailSubscriptionOutboxTasks
            .OrderBy(t => t.CreatedAt)
            .ThenBy(t => t.Id)
            .FirstOrDefaultAsync(ct);

        if (task == null) return false;
        if (task.NextAttemptAt > DateTimeOffset.UtcNow) return false;
        logger.LogInformation("Processing mail subscription outbox task {TaskType} for email {Email}. Retry {RetryCount}.", task.TaskType, task.Email, task.RetryCount);

        var mailService = scope.ServiceProvider.GetRequiredService<IMailSubscriptionService>();

        try
        {
            await HandleTaskAsync(mailService, task, ct);
            db.MailSubscriptionOutboxTasks.Remove(task);
            logger.LogInformation("Completed mail subscription outbox task for email {Email}.", task.Email);
        }
        catch (Exception ex)
        {
            HandleFailure(task, ex);
        }

        await db.SaveChangesAsync(ct);
        return true;
    }

    private async Task HandleTaskAsync(IMailSubscriptionService service, MailSubscriptionOutboxTask task, CancellationToken ct)
    {
        switch (task.TaskType)
        {
            case MailSubscriptionOutboxTaskType.UpdateSubscriptions:
                var subscribedListIds = string.IsNullOrEmpty(task.SubscribedListIdsJson)
                    ? []
                    : JsonSerializer.Deserialize<List<string>>(task.SubscribedListIdsJson) ?? [];
                await service.UpdateMemberSubscriptionsAsync(task.Email, subscribedListIds, ct, task.FirstName, task.LastName);
                break;
            case MailSubscriptionOutboxTaskType.Delete:
                await service.DeleteMemberAsync(task.Email, ct);
                break;
            case MailSubscriptionOutboxTaskType.MigrateEmail:
                if (task.OldEmail == null)
                {
                    // Always set by EnqueueMigrateEmailTask, so a missing value means a corrupted row - discard rather than retry forever.
                    logger.LogError("MigrateEmail task {TaskId} for {Email} is missing OldEmail. Discarding.", task.Id, task.Email);
                    return;
                }
                await service.MigrateEmailAsync(task.OldEmail, task.Email, ct, task.FirstName, task.LastName);
                break;
            case MailSubscriptionOutboxTaskType.UpdateName:
                if (task.FirstName == null || task.LastName == null)
                {
                    // Same reasoning as MigrateEmail above.
                    logger.LogError("UpdateName task {TaskId} for {Email} is missing FirstName/LastName. Discarding.", task.Id, task.Email);
                    return;
                }
                await service.UpdateMemberNameAsync(task.Email, task.FirstName, task.LastName, ct);
                break;
            default:
                throw new NotSupportedException($"Unsupported mail subscription outbox task type '{task.TaskType}'.");
        }
    }

    private void HandleFailure(MailSubscriptionOutboxTask task, Exception ex)
    {
        logger.LogError(ex, "Sync failed for {Email}. Retry count: {Retry}", task.Email, task.RetryCount);

        task.RetryCount++;
        // Exponential backoff with a max delay of 1 hour
        double extraMinutes = Math.Min(Math.Pow(2, task.RetryCount), 60);
        task.NextAttemptAt = DateTimeOffset.UtcNow.AddMinutes(extraMinutes);
        logger.LogWarning("Rescheduled mail subscription task for email {Email} at {NextRunUtc}.", task.Email, task.NextAttemptAt);
    }
}
