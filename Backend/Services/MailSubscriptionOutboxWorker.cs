using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

/// <summary>
/// Background worker that processes queued mail subscription tasks.
/// </summary>
public class MailSubscriptionOutboxWorker(
    IServiceProvider serviceProvider,
    ILogger<MailSubscriptionOutboxWorker> logger) : BackgroundService
{
    private readonly bool _isEnabled = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MAIL_SUBSCRIPTION_SERVICE"));

    /// <summary>
    /// Enqueues an mail subscription outbox task.
    /// </summary>
    /// <param name="email">The email address to process.</param>
    /// <param name="mailSubscriptions">The mail subscription settings to apply.</param>
    /// <param name="db">The database context used to persist the task.</param>
    public void EnqueueTask(string email, uint mailSubscriptions, PostgresDbContext db)
    {
        var task = new MailSubscriptionOutboxTask
        {
            Email = email,
            MailSubscription = mailSubscriptions,
            CreatedAt = DateTimeOffset.UtcNow,
            NextAttemptAt = DateTimeOffset.UtcNow,
            RetryCount = 0
        };

        db.MailSubscriptionOutboxTasks.Add(task);
        db.SaveChanges();
        logger.LogInformation("Enqueued mail subscription outbox task for email {Email}.", email);
    }

    /// <summary>
    /// Executes the background worker, continuously processing mail subscription outbox tasks until the service is stopped. The worker retrieves tasks from the database, processes them using the mail subscription service, and handles any failures with retry logic and exponential backoff. The worker also includes logging for monitoring task processing and any errors that occur during execution.
    /// </summary>
    /// <param name="stoppingToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_isEnabled)
        {
            logger.LogInformation("Mailsubscription outbox worker is disabled.");
            return;
        }

        logger.LogInformation("Mailsubscription outbox worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
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
        logger.LogInformation("Processing mail subscription outbox task for email {Email}. Retry {RetryCount}.", task.Email, task.RetryCount);

        var mailService = scope.ServiceProvider.GetRequiredService<IMailSubscriptionService>();

        try
        {
            await HandleTaskAsync(db, mailService, task, ct);
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
    /// Handles the processing of a mail subscription outbox task by calling the mail subscription service to update the subscription for the specified email address. If the processing fails, an exception is thrown, which is caught and handled in the calling method to implement retry logic and exponential backoff for failed tasks.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="service">The mail subscription service.</param>
    /// <param name="task">The mail subscription outbox task.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task HandleTaskAsync(PostgresDbContext db, IMailSubscriptionService service, MailSubscriptionOutboxTask task, CancellationToken ct)
    {
        await service.UpdateSubscriptionAsync(task.Email, task.MailSubscription, ct);
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
