namespace Backend.Models.Domain;

/// <summary>
/// Represents a task in the outbox for mail subscription operations. This entity is used to track tasks that need to be processed for managing mail subscriptions, such as adding or removing a user from a mail subscription list. Each task is associated with a specific Keycloak user and includes information about when it was created, how many times it has been retried, and the type of task being performed.
/// </summary>
public class MailSubscriptionOutboxTask
{
    /// <summary>
    /// The unique identifier for the outbox task.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// The identifier of the Keycloak user associated with this outbox task. This is a foreign key referencing the Keycloak user.
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// The timestamp indicating when the outbox task was created. This is used to track when the task was generated and can be useful for retry logic or auditing purposes.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// The number of times this outbox task has been retried. This is used to track how many times the task has been attempted, which can be useful for implementing retry logic or for monitoring the success of task processing.
    /// </summary>
    public int RetryCount { get; set; } = 0;

    /// <summary>
    /// The mail subscription associated with this outbox task. This is a required property that references the MailSubscriptions entity, which contains information about the specific mail subscription that this task is related to. This association allows for better organization and retrieval of tasks based on the related mail subscription.
    /// </summary>
    public required uint MailSubscription { get; set; }
}