namespace Backend.Models.Domain;

/// <summary>
/// Represents a task in the outbox for authentication-system operations. This entity is used to track tasks that
/// need to be processed for the configured auth provider, such as creating, synchronizing, deleting users,
/// or refreshing user emails. Each task is associated with a specific auth-system user and includes information
/// about when it was created and how many times it has been retried.
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
    RefreshEmail
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
    /// The identifier of the authentication-system user associated with this outbox task.
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
