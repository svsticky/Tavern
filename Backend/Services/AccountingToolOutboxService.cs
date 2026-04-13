using Backend.Database;
using Backend.Interfaces;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

public class AccountingToolOutboxWorker(
    IServiceProvider serviceProvider,
    ILogger<AccountingToolOutboxWorker> logger) : BackgroundService
{
    private readonly bool _isEnabled = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ACCOUNTING_SERVICE"));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_isEnabled)
        {
            logger.LogInformation("Accounting tool outbox worker is disabled.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            bool processed = await TryProcessNextTaskAsync(stoppingToken);

            if (!processed)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
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

        var exactService = scope.ServiceProvider.GetRequiredService<IAccountingToolService>();

        try
        {
            await HandleTaskAsync(db, exactService, task, ct);
            db.AccountingToolOutboxTasks.Remove(task);
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
    }

    private void HandleFailure(AccountingToolOutboxTask task, Exception ex)
    {
        logger.LogError(ex, "Sync failed for {PaymentId}. Retry count: {Retry}", task.PaymentId, task.RetryCount);
        
        task.RetryCount++;
        // Exponential backoff with a max delay of 1 hour
        double extraMinutes = Math.Min(Math.Pow(2, task.RetryCount), 60);
        task.CreatedAt = DateTime.UtcNow.AddMinutes(extraMinutes);
    }
}