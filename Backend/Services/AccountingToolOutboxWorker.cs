using Backend.Database;
using Backend.Models.Domain;
using Backend.Services.AccountingToolServices;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

/// <summary>
/// Background worker that processes queued accounting synchronization tasks.
/// </summary>
public class AccountingToolOutboxWorker(
    IServiceProvider serviceProvider,
    ILogger<AccountingToolOutboxWorker> logger) : BackgroundService
{

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
            CreatedAt = DateTimeOffset.UtcNow,
            NextAttemptAt = DateTimeOffset.UtcNow,
            RetryCount = 0
        };

        db.AccountingToolOutboxTasks.Add(task);
        db.SaveChanges();
        logger.LogInformation("Enqueued accounting outbox task {TaskType} for payment {PaymentId}.", taskType, paymentId);
    }

    /// <summary>
    /// Executes the background worker loop.
    /// </summary>
    /// <param name="stoppingToken">The cancellation token for stopping the worker.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Accounting tool outbox worker started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var envEnabled = Environment.GetEnvironmentVariable("ACCOUNTING_ENABLED");
                if (envEnabled != null && envEnabled.Equals("false", StringComparison.OrdinalIgnoreCase))
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                    continue;
                }

                var db = scope.ServiceProvider.GetRequiredService<PostgresDbContext>();
                var isEnabled = !string.IsNullOrWhiteSpace(db.Settings.FirstOrDefault(s => s.Name == "AccountingService")?.Value);

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

        logger.LogInformation("Accounting tool outbox worker stopped.");
    }

    private async Task<bool> TryProcessNextTaskAsync(CancellationToken ct)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PostgresDbContext>();

        var task = await db.AccountingToolOutboxTasks
            .OrderBy(t => t.CreatedAt)
            .ThenBy(t => t.Id)
            .FirstOrDefaultAsync(ct);

        if (task == null) return false;
        if (task.NextAttemptAt > DateTimeOffset.UtcNow) return false;
        logger.LogInformation("Processing accounting outbox task {TaskType} for payment {PaymentId}. Retry {RetryCount}.",
            task.TaskType, task.PaymentId, task.RetryCount);

        var accountingService = scope.ServiceProvider.GetRequiredService<AbstractAccountingToolService>();

        try
        {
            await HandleTaskAsync(db, accountingService, task, ct);
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

    private async Task HandleTaskAsync(PostgresDbContext db, AbstractAccountingToolService service, AccountingToolOutboxTask task, CancellationToken ct)
    {
        Payment? payment = task.TaskType switch
        {
            AccountingToolTaskType.EnrollmentPayment => await db.EnrollmentPayments.FindAsync([task.PaymentId], ct),
            AccountingToolTaskType.MembershipPayment => await db.MembershipPayments.FindAsync([task.PaymentId], ct),
            AccountingToolTaskType.PaymentServiceFeePayment => await db.PaymentServiceFeePayments.FindAsync([task.PaymentId], ct),
            AccountingToolTaskType.BegunstigerPayment => await db.BegunstigerPayments.FindAsync([task.PaymentId], ct),
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
        task.NextAttemptAt = DateTimeOffset.UtcNow.AddMinutes(extraMinutes);
        logger.LogWarning("Rescheduled accounting task {TaskType} for payment {PaymentId} at {NextRunUtc}.",
            task.TaskType, task.PaymentId, task.NextAttemptAt);
    }
}
