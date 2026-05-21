using Backend.Database;
using Backend.Models.Domain;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

/// <summary>
/// Background worker that processes queued Keycloak synchronization tasks.
/// </summary>
public class KeycloakOutboxWorker(
    IServiceProvider serviceProvider, 
    ILogger<KeycloakOutboxWorker> logger) : BackgroundService
{
    /// <summary>
    /// Enqueues a Keycloak outbox task for asynchronous processing.
    /// </summary>
    /// <param name="taskType">The task type to enqueue.</param>
    /// <param name="keycloakId">The target Keycloak user ID.</param>
    public async Task EnqueueTask(KeycloakTaskType taskType, Guid keycloakId)
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
        await db.SaveChangesAsync();
        logger.LogInformation("Enqueued Keycloak outbox task {TaskType} for {KeycloakId}.", taskType, keycloakId);
    }

    /// <summary>
    /// Executes the background worker, continuously processing Keycloak outbox tasks until the service is stopped. The worker retrieves tasks from the database, processes them using the Keycloak API service, and handles any failures with retry logic and exponential backoff. The worker also includes logging for monitoring task processing and any errors that occur during execution.
    /// </summary>
    /// <param name="stoppingToken">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
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

        logger.LogInformation("Keycloak outbox worker stopped.");
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
        logger.LogInformation("Processing Keycloak outbox task {TaskType} for {KeycloakId}. Retry {RetryCount}.",
            task.TaskType, task.KeycloakId, task.RetryCount);

        var syncService = scope.ServiceProvider.GetRequiredService<KeycloakAPIService>();

        try
        {
            await HandleTaskAsync(db, syncService, task, ct);
            
            db.KeycloakOutboxTasks.Remove(task);
            logger.LogInformation("Completed Keycloak outbox task {TaskType} for {KeycloakId}.", task.TaskType, task.KeycloakId);
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
                        logger.LogInformation("Linked local member {MemberId} to Keycloak user {KeycloakId}.", member.Id, kId);
                    }
                }
                else
                {
                    logger.LogWarning("Keycloak create task skipped: member {MemberId} was not found.", task.KeycloakId);
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
                else
                {
                    logger.LogWarning("Keycloak delete task skipped: user {KeycloakId} was not found in local members.", task.KeycloakId);
                }
                break;

            case KeycloakTaskType.RefreshEmail:
                await syncService.RefreshEmail(task.KeycloakId);
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
        logger.LogWarning("Rescheduled Keycloak task {TaskType} for {KeycloakId} at {NextRunUtc}.",
            task.TaskType, task.KeycloakId, task.CreatedAt);
    }
}
