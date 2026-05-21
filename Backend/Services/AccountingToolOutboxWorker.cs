using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

/// <summary>
/// Background worker that processes queued accounting synchronization tasks.
/// </summary>
public class AccountingToolOutboxWorker(
    IServiceProvider serviceProvider,
    ILogger<AccountingToolOutboxWorker> logger) : BackgroundService
{
    private readonly bool _isEnabled = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ACCOUNTING_SERVICE"));

    /// <summary>
    /// Enqueues an accounting outbox task for a payment.
    /// </summary>
    /// <param name="taskType">The accounting task type.</param>
    /// <param name="paymentId">The payment ID to process.</param>
    /// <param name="db">The database context used to persist the task.</param>
    public void EnqueueTask(AccountingToolTaskType taskType, uint paymentId, PostgresDbContext db)
    {
        var task = new AccountingToolOutboxTask
        {
            TaskType = taskType,
            PaymentId = paymentId,
            CreatedAt = DateTime.UtcNow,
            RetryCount = 0
        };

        db.AccountingToolOutboxTasks.Add(task);
        db.SaveChanges();
        logger.LogInformation("Enqueued accounting outbox task {TaskType} for payment {PaymentId}.", taskType, paymentId);
    }

    /// <summary>
    /// Executes the background worker, continuously processing accounting outbox tasks until the service is stopped. The worker retrieves tasks from the database, processes them using the appropriate accounting tool service, and handles any failures with retry logic and exponential backoff. The worker also includes logging for monitoring task processing and any errors that occur during execution.
    /// </summary>
    /// <param name="stoppingToken">The cancellation token for stopping the worker.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_isEnabled)
        {
            logger.LogInformation("Accounting tool outbox worker is disabled.");
            return;
        }

        logger.LogInformation("Accounting tool outbox worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            bool processed = await TryProcessNextTaskAsync(stoppingToken);

            if (!processed)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }

        logger.LogInformation("Accounting tool outbox worker stopped.");
    }

    private async Task<bool> TryProcessNextTaskAsync(CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PostgresDbContext>();
        
        var task = await db.AccountingToolOutboxTasks
            .Where(t => t.CreatedAt <= DateTime.UtcNow)
            .OrderBy(t => t.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (task == null) return false;
        logger.LogInformation("Processing accounting outbox task {TaskType} for payment {PaymentId}. Retry {RetryCount}.",
            task.TaskType, task.PaymentId, task.RetryCount);

        var exactService = scope.ServiceProvider.GetRequiredService<IAccountingToolService>();

        try
        {
            await HandleTaskAsync(db, exactService, task, ct);
            db.AccountingToolOutboxTasks.Remove(task);
            logger.LogInformation("Completed accounting outbox task {TaskType} for payment {PaymentId}.", task.TaskType, task.PaymentId);
        }
        catch (Exception ex)
        {
            HandleFailure(task, ex);
        }

        await db.SaveChangesAsync(ct);
        return true;
    }

    private async Task HandleTaskAsync(PostgresDbContext db, IAccountingToolService service, AccountingToolOutboxTask task, CancellationToken ct)
    {
        Payment? payment = task.TaskType switch
        {
            AccountingToolTaskType.EnrollmentPayment => await db.EnrollmentPayments.FindAsync([task.PaymentId], ct),
            AccountingToolTaskType.MembershipPayment => await db.MembershipPayments.FindAsync([task.PaymentId], ct),
            _ => throw new ArgumentOutOfRangeException(nameof(task.TaskType), "Unknown task type")
        };

        if (payment != null)
        {
            var entryId = await service.SyncPaymentAsync(payment, ct);
            payment.AccountingToolEntryId = entryId;
        }
        else
        {
            logger.LogWarning("Accounting outbox task {TaskType} skipped: payment {PaymentId} not found.", task.TaskType, task.PaymentId);
        }
    }

    private void HandleFailure(AccountingToolOutboxTask task, Exception ex)
    {
        logger.LogError(ex, "Sync failed for {PaymentId}. Retry count: {Retry}", task.PaymentId, task.RetryCount);
        
        task.RetryCount++;
        // Exponential backoff with a max delay of 1 hour
        double extraMinutes = Math.Min(Math.Pow(2, task.RetryCount), 60);
        task.CreatedAt = DateTime.UtcNow.AddMinutes(extraMinutes);
        logger.LogWarning("Rescheduled accounting task {TaskType} for payment {PaymentId} at {NextRunUtc}.",
            task.TaskType, task.PaymentId, task.CreatedAt);
    }
}
