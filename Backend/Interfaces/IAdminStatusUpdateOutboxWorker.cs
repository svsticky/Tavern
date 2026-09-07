using Backend.Database;

namespace Backend.Interfaces;

/// <summary>
/// Defines the contract for an outbox worker that enqueues administrative status updates for external services.
/// </summary>
public interface IAdminStatusUpdateOutboxWorker
{
    /// <summary>
    /// Enqueues an administrative status update task for a user.
    /// </summary>
    /// <param name="email">The email address of the user.</param>
    /// <param name="isAdmin">Whether the user has administrative or board permissions.</param>
    /// <param name="db">The database context used to persist the task.</param>
    void EnqueueAdminStatusUpdate(string email, bool isAdmin, PostgresDbContext db);
}
