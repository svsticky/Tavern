using Backend.Database;

namespace Backend.Interfaces;

/// <summary>
/// Defines the contract for an outbox worker that enqueues email updates and deletions for external services.
/// </summary>
public interface IMailSyncOutboxWorker
{
    /// <summary>
    /// Enqueues an email migration task from an old email to a new email.
    /// </summary>
    /// <param name="oldEmail">The old email address.</param>
    /// <param name="newEmail">The new email address.</param>
    /// <param name="db">The database context used to persist the task.</param>
    void EnqueueSyncMail(string oldEmail, string newEmail, PostgresDbContext db);

    /// <summary>
    /// Enqueues an account deletion task for a user email.
    /// </summary>
    /// <param name="email">The email address of the user being deleted.</param>
    /// <param name="db">The database context used to persist the task.</param>
    void EnqueueDeleteMail(string email, PostgresDbContext db);
}
