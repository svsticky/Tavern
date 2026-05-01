namespace Backend.Models.Domain;

/// <summary>
/// Represents a task in the outbox for Keycloak operations. This entity is used to track tasks that need to be processed for Keycloak, such as creating, synchronizing, deleting users, or refreshing user emails. Each task is associated with a specific Keycloak user and includes information about when it was created, how many times it has been retried, and the type of task being performed.
/// </summary>
public enum KeycloakTaskType { Create, Sync, Delete, RefreshEmail }

/// <summary>
/// Represents a task in the outbox for Keycloak operations. This entity is used to track tasks that need to be processed for Keycloak, such as creating, synchronizing, deleting users, or refreshing user emails. Each task is associated with a specific Keycloak user and includes information about when it was created, how many times it has been retried, and the type of task being performed.
/// </summary>
public class KeycloakOutboxTask
{
    /// <summary>
    /// The unique identifier for the outbox task.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// The identifier of the Keycloak user associated with this outbox task. This is a foreign key referencing the Keycloak user.
    /// </summary>
    public Guid KeycloakId { get; set; }

    /// <summary>
    /// The type of the Keycloak task. This is an enumeration that indicates the specific type of task being performed, such as creating a user, synchronizing user data, deleting a user, or refreshing a user's email. This information can be used to determine how the task should be handled and processed.
    /// </summary>
    public KeycloakTaskType TaskType { get; set; }

    /// <summary>
    /// The timestamp indicating when the outbox task was created. This is used to track when the task was generated and can be useful for retry logic or auditing purposes.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// The number of times this outbox task has been retried. This is used to track how many times the task has been attempted, which can be useful for implementing retry logic or for monitoring the success of task processing.
    /// </summary>
    public int RetryCount { get; set; } = 0;
}