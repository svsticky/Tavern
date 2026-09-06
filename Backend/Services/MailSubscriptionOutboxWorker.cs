using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Backend.Services;

/// <summary>
/// Background worker that processes queued mail subscription tasks.
/// </summary>
public class MailSubscriptionOutboxWorker(
    IServiceProvider serviceProvider,
    ILogger<MailSubscriptionOutboxWorker> logger) : BackgroundService, IMailSyncOutboxWorker
{

    /// <summary>
    /// Enqueues a task that replaces a member's mailing list subscriptions.
    /// </summary>
    /// <param name="email">The email address to process.</param>
    /// <param name="subscribedListIds">The IDs of the mailing lists the member should be subscribed to.</param>
    /// <param name="db">The database context used to persist the task.</param>
    public virtual void EnqueueUpdateSubscriptionsTask(string email, IEnumerable<string> subscribedListIds, PostgresDbContext db)
    {
        var task = new MailSubscriptionOutboxTask
        {
            TaskType = MailSubscriptionOutboxTaskType.UpdateSubscriptions,
            Email = email,
            SubscribedListIdsJson = JsonSerializer.Serialize(subscribedListIds.ToList()),
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
    /// Enqueues a task that moves a member's mail subscriptions from an old email address to a new one.
    /// </summary>
    /// <param name="oldEmail">The member's previous email address.</param>
    /// <param name="newEmail">The member's new email address.</param>
    /// <param name="db">The database context used to persist the task.</param>
    public virtual void EnqueueMigrateEmailTask(string oldEmail, string newEmail, PostgresDbContext db)
    {
        var task = new MailSubscriptionOutboxTask
        {
            TaskType = MailSubscriptionOutboxTaskType.MigrateEmail,
            Email = newEmail,
            OldEmail = oldEmail,
            CreatedAt = DateTimeOffset.UtcNow,
            NextAttemptAt = DateTimeOffset.UtcNow,
            RetryCount = 0
        };

        db.MailSubscriptionOutboxTasks.Add(task);
        db.SaveChanges();
        logger.LogInformation("Enqueued mail subscription email migration task from {OldEmail} to {NewEmail}.", oldEmail, newEmail);
    }

    /// <inheritdoc />
    public virtual void EnqueueSyncMail(string oldEmail, string newEmail, PostgresDbContext db)
    {
        EnqueueMigrateEmailTask(oldEmail, newEmail, db);
    }

    /// <inheritdoc />
    public virtual void EnqueueDeleteMail(string email, PostgresDbContext db)
    {
        EnqueueDeleteTask(email, db);
    }

    /// <summary>
    /// Executes the background worker, continuously processing mail subscription outbox tasks until the service is stopped. The worker retrieves tasks from the database, processes them using the mail subscription service, and handles any failures with retry logic and exponential backoff. The worker also includes logging for monitoring task processing and any errors that occur during execution.
    /// </summary>
    /// <param name="stoppingToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
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

    /// <summary>
    /// Tries to process the next mail subscription outbox task from the database. If a task is found, it is processed using the mail subscription service, and any failures are handled with retry logic and exponential backoff. The method returns true if a task was processed, or false if no tasks were available to process.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
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

    /// <summary>
    /// Handles the processing of a mail subscription outbox task by calling the matching mail subscription service method based on the task's type. If the processing fails, an exception is thrown, which is caught and handled in the calling method to implement retry logic and exponential backoff for failed tasks.
    /// </summary>
    /// <param name="service">The mail subscription service.</param>
    /// <param name="task">The mail subscription outbox task.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task HandleTaskAsync(IMailSubscriptionService service, MailSubscriptionOutboxTask task, CancellationToken ct)
    {
        switch (task.TaskType)
        {
            case MailSubscriptionOutboxTaskType.UpdateSubscriptions:
                var subscribedListIds = string.IsNullOrEmpty(task.SubscribedListIdsJson)
                    ? []
                    : JsonSerializer.Deserialize<List<string>>(task.SubscribedListIdsJson) ?? [];
                await service.UpdateMemberSubscriptionsAsync(task.Email, subscribedListIds, ct);
                break;
            case MailSubscriptionOutboxTaskType.Delete:
                await service.DeleteMemberAsync(task.Email, ct);
                break;
            case MailSubscriptionOutboxTaskType.MigrateEmail:
                await service.MigrateEmailAsync(task.OldEmail ?? throw new InvalidOperationException("MigrateEmail task is missing OldEmail."), task.Email, ct);
                break;
            default:
                throw new NotSupportedException($"Unsupported mail subscription outbox task type '{task.TaskType}'.");
        }
    }

    /// <summary>
    /// Handles the failure of processing a mail subscription outbox task by logging the error, incrementing the retry count, and rescheduling the task with exponential backoff. The next scheduled time for the task is calculated based on the number of retries, with a maximum delay of 1 hour to prevent excessively long delays between retries. This method ensures that failed tasks are retried in a controlled manner while providing visibility into the failures through logging.
    /// </summary>
    /// <param name="task">The mail subscription outbox task.</param>
    /// <param name="ex">The exception that occurred.</param>
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
