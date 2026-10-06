namespace Backend.Models.Domain;

/// <summary>
/// Represents a task in the outbox for authentication-system operations. This entity is used to track tasks that
/// need to be processed for the configured auth provider, such as creating, synchronizing, deleting users,
/// or refreshing user emails. Each task is associated with a local Member (not directly with an auth-system user -
/// the member may not have one yet, e.g. before their Create task runs) and includes information about when it
/// was created and how many times it has been retried.
/// </summary>
public enum AuthTaskType
{
    /// <summary>
    /// Indicates that the task is to create a new user in the authentication system.
    /// </summary>
    Create,

    /// <summary>
    /// Indicates that the task is to synchronize user data between the system and the authentication provider.
    /// </summary>
    Sync,

    /// <summary>
    /// Indicates that the task is to delete a user from the authentication system.
    /// </summary>
    Delete,

    /// <summary>
    /// Indicates that the task is to refresh a user's email in the authentication system.
    /// </summary>
    RefreshEmail,

    /// <summary>
    /// Indicates that the task is to send the user a combined email-verification and set-password action email.
    /// </summary>
    SendActivationEmail
}

/// <summary>
/// Represents a queued auth-system operation waiting to be processed.
/// </summary>
public class AuthOutboxTask
{
    /// <summary>
    /// The unique identifier for the outbox task.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// For every task type except Delete, this is the target member's local ID - AuthOutboxWorker
    /// resolves the member (and, for Sync, creates their auth-system user first if they don't have
    /// one yet) when the task is processed. For Delete, this is the auth-system user ID to delete
    /// directly: by the time a Delete task runs, the local Member row may already be gone (a
    /// hard-deleted member) or may have been anonymized, so unlike every other task type there's
    /// no live Member row to resolve an ID from later - the caller captures it up front instead.
    /// </summary>
    public Guid AuthSystemUserId { get; set; }

    /// <summary>
    /// The type of the authentication task.
    /// </summary>
    public AuthTaskType TaskType { get; set; }

    /// <summary>
    /// The timestamp indicating when the outbox task was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// The timestamp indicating when this task is eligible for its next processing attempt.
    /// </summary>
    public DateTimeOffset NextAttemptAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// The number of times this outbox task has been retried.
    /// </summary>
    public int RetryCount { get; set; } = 0;
}
