using Backend.Database;

namespace Backend.Interfaces;

/// <summary>
/// Implemented by systems that react whenever a member's email address changes.
/// </summary>
public interface IMailChangedListener
{
    /// <summary>
    /// Whether this listener should currently be notified.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Called once the member's new email address is known. Implementations enqueue any outbox
    /// work on <paramref name="db"/>, so it persists with the caller's own transaction.
    /// </summary>
    /// <param name="memberId">The local ID of the member whose email changed.</param>
    /// <param name="oldEmail">The member's previous email address.</param>
    /// <param name="newEmail">The member's new email address.</param>
    /// <param name="db">The database context used to persist any resulting outbox task.</param>
    void OnMailChanged(Guid memberId, string oldEmail, string newEmail, PostgresDbContext db);
}
