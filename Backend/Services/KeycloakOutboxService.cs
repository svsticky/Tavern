using Backend.Database;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class KeycloakOutboxWorker(
    IServiceProvider serviceProvider, 
    ILogger<KeycloakOutboxWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<PostgresDbContext>();
                var syncService = scope.ServiceProvider.GetRequiredService<KeycloakSyncService>();

                var task = await db.KeyCloakOutboxTasks
                    .OrderBy(t => t.CreatedAt)
                    .FirstOrDefaultAsync(stoppingToken);

                if (task != null)
                {
                    try
                    {
                        await syncService.SyncUserMemberships(task.MemberId);
                        db.KeyCloakOutboxTasks.Remove(task);
                        await db.SaveChangesAsync(stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Sync failed for {Id}. Try {Count}", task.MemberId, task.RetryCount);
                        task.RetryCount++;
                        task.CreatedAt = DateTime.UtcNow.AddMinutes(Math.Pow(2, task.RetryCount));
                        await db.SaveChangesAsync(stoppingToken);
                    }
                }
            }
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}