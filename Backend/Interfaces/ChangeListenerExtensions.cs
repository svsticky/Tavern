using Backend.Database;
using Backend.Models.Domain;

namespace Backend.Interfaces;

/// <summary>
/// Shared dispatch helpers for <see cref="INameChangedListener"/> and <see cref="IMailChangedListener"/>,
/// so every call site notifies listeners the same way instead of repeating the loop.
/// </summary>
internal static class ChangeListenerExtensions
{
    /// <summary>
    /// Notifies every enabled listener that a member's name changed.
    /// </summary>
    public static void NotifyNameChanged(this IEnumerable<INameChangedListener> listeners, Member member, PostgresDbContext db)
    {
        foreach (var listener in listeners.Where(l => l.IsEnabled))
        {
            listener.OnNameChanged(member, db);
        }
    }

    /// <summary>
    /// Notifies every enabled listener that a member's email changed.
    /// </summary>
    public static void NotifyMailChanged(this IEnumerable<IMailChangedListener> listeners, Guid memberId, string oldEmail, string newEmail, PostgresDbContext db)
    {
        foreach (var listener in listeners.Where(l => l.IsEnabled))
        {
            listener.OnMailChanged(memberId, oldEmail, newEmail, db);
        }
    }
}
