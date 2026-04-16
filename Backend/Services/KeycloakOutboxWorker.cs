using Backend.Database;
using Backend.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class KeycloakOutboxWorker(
    IServiceProvider serviceProvider, 
    ILogger<KeycloakOutboxWorker> logger) : BackgroundService
{
    public void EnqueueTask(KeycloakTaskType taskType, Guid keycloakId)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PostgresDbContext>();

        var task = new KeycloakOutboxTask
        {
            TaskType = taskType,
            KeycloakId = keycloakId,
            CreatedAt = DateTime.UtcNow,
            RetryCount = 0
        };

        db.KeycloakOutboxTasks.Add(task);
        db.SaveChanges();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Keycloak outbox worker started.");

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
        
        var task = await db.KeycloakOutboxTasks
            .Where(t => t.CreatedAt <= DateTime.UtcNow)
            .OrderBy(t => t.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (task == null) return false;

        var syncService = scope.ServiceProvider.GetRequiredService<KeycloakAPIService>();

        try
        {
            await HandleTaskAsync(db, syncService, task, ct);
            
            db.KeycloakOutboxTasks.Remove(task);
        }
        catch (Exception ex)
        {
            HandleFailure(task, ex);
        }

        await db.SaveChangesAsync(ct);
        return true;
    }

    private async Task HandleTaskAsync(PostgresDbContext db, KeycloakAPIService syncService, KeycloakOutboxTask task, CancellationToken ct)
    {
        switch (task.TaskType)
        {
            case KeycloakTaskType.Create:
                var member = await db.Members.FindAsync([task.KeycloakId], ct);
                if (member != null)
                {
                    var kId = await syncService.CreateUserInKeycloak(member);
                    if (kId != null)
                    {
                        member.KeycloakId = kId;
                    }
                }
                break;

            case KeycloakTaskType.Sync:
                await syncService.SyncMemberInKeyCloak(task.KeycloakId);
                break;

            case KeycloakTaskType.Delete:
                var memberToDelete = await db.Members.FirstOrDefaultAsync(m => m.KeycloakId == task.KeycloakId, ct);
                if (memberToDelete?.KeycloakId != null)
                {
                    await syncService.DeleteUserInKeycloak(memberToDelete.KeycloakId.Value);
                }
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(task.TaskType), "Unknown Keycloak task type");
        }
    }

    private void HandleFailure(KeycloakOutboxTask task, Exception ex)
    {
        logger.LogError(ex, "Keycloak sync failed for {Id}. Retry count: {Count}", task.KeycloakId, task.RetryCount);

        task.RetryCount++;
        
        // Exponential backoff with a max delay of 1 hour
        double extraMinutes = Math.Min(Math.Pow(2, task.RetryCount), 60);
        task.CreatedAt = DateTime.UtcNow.AddMinutes(extraMinutes);
    }
}