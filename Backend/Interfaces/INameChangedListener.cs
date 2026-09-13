using Backend.Database;
using Backend.Models.Domain;

namespace Backend.Interfaces;

/// <summary>
/// Implemented by systems that react whenever a member's first and/or last name changes.
/// </summary>
public interface INameChangedListener
{
    /// <summary>
    /// Whether this listener should currently be notified.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Called with the member's current (already-updated) name. Implementations enqueue any
    /// outbox work on <paramref name="db"/>, so it persists with the caller's own transaction.
    /// </summary>
    /// <param name="member">The member whose name changed, with the new name already applied.</param>
    /// <param name="db">The database context used to persist any resulting outbox task.</param>
    void OnNameChanged(Member member, PostgresDbContext db);
}
