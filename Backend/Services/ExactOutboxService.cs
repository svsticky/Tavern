using Backend.Database;
using Backend.Interfaces;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

public class ExactOutboxWorker(
    IServiceProvider serviceProvider,
    ILogger<ExactOutboxWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            bool hadTask = false;

            using (var scope = serviceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<PostgresDbContext>();
                var exactService = scope.ServiceProvider.GetRequiredService<IExactService>();

                var task = await db.ExactOutboxTasks
                    .Where(t => t.CreatedAt <= DateTime.UtcNow)
                    .OrderBy(t => t.CreatedAt)
                    .FirstOrDefaultAsync(stoppingToken);

                hadTask = task != null;

                if (task != null)
                {
                    try
                    {
                        Payment? payment = task.TaskType switch
                        {
                            ExactTaskType.EnrollmentPayment => await db.EnrollmentPayments.FindAsync(task.PaymentId, stoppingToken),
                            ExactTaskType.MembershipPayment => await db.MembershipPayments.FindAsync(task.PaymentId, stoppingToken),
                            _ => throw new Exception("Unknown task type")
                        };

                        if (payment != null)
                        {
                            var id = await exactService.SyncPaymentAsync(payment, stoppingToken);

                            payment.ExactEntryId = id;
                        }

                        db.ExactOutboxTasks.Remove(task);
                        await db.SaveChangesAsync(stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Exact sync failed for {PaymentId}. Try {Retry}", task.PaymentId, task.RetryCount);

                        task.RetryCount++;

                        double extraMinutes = Math.Min(Math.Pow(2, task.RetryCount), 60);

                        task.CreatedAt = DateTime.UtcNow.AddMinutes(extraMinutes);

                        await db.SaveChangesAsync(stoppingToken);
                    }
                }
            }

            if (!hadTask)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
    }
}