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
                var syncService = scope.ServiceProvider.GetRequiredService<KeycloakAPIService>();

                var task = await db.KeyCloakOutboxTasks
                    .OrderBy(t => t.CreatedAt)
                    .FirstOrDefaultAsync(stoppingToken);

                if (task != null)
                {
                    try
                    {
                        switch(task.TaskType)
                        {
                            case KeycloakTaskType.Create:
                                var member = await db.Members.FindAsync(task.KeycoakId);
                                if (member != null)
                                {
                                    var keycloakId = await syncService.CreateUserInKeycloak(member);
                                    if (keycloakId != null)
                                    {
                                        member.KeycloakId = keycloakId;
                                        await db.SaveChangesAsync(stoppingToken);
                                    }
                                }
                                break;
                            case KeycloakTaskType.Sync:
                                await syncService.SyncMemberInKeyCloak(task.KeycoakId);
                                break;
                            case KeycloakTaskType.Delete:
                                var memberToDelete = await db.Members.FindAsync(task.KeycoakId);
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
                        task.CreatedAt = DateTime.UtcNow.AddMinutes(Math.Pow(2, task.RetryCount));
                        await db.SaveChangesAsync(stoppingToken);
                    }
                }
            }
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}