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
            bool hadTask = false;

            using (var scope = serviceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<PostgresDbContext>();
                var syncService = scope.ServiceProvider.GetRequiredService<KeycloakAPIService>();

                var task = await db.KeyCloakOutboxTasks
                    .Where(t => t.CreatedAt <= DateTime.UtcNow)
                    .OrderBy(t => t.CreatedAt)
                    .FirstOrDefaultAsync(stoppingToken);

                hadTask = task != null;

                if (task != null)
                {
                    try
                    {
                        switch(task.TaskType)
                        {
                            case KeycloakTaskType.Create:
                                var member = await db.Members.FindAsync(task.KeycoakId);
                                if (member != null) {
                                    var kId = await syncService.CreateUserInKeycloak(member);
                                    if (kId != null) {
                                        member.KeycloakId = kId;
                                    }
                                }
                                break;
                            case KeycloakTaskType.Sync:
                                await syncService.SyncMemberInKeyCloak(task.KeycoakId);
                                break;
                            case KeycloakTaskType.Delete:
                                var memberToDelete = await db.Members.FirstOrDefaultAsync(m => m.KeycloakId == task.KeycoakId);
                                if(memberToDelete != null && memberToDelete.KeycloakId != null)
                                {
                                    await syncService.DeleteUserInKeycloak(memberToDelete.KeycloakId.Value);
                                }
                                break;
                        }

                        db.KeyCloakOutboxTasks.Remove(task);
                        await db.SaveChangesAsync(stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Sync failed for {Id}. Try {Count}", task.KeycoakId, task.RetryCount);
    
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