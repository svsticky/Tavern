namespace Backend.Models.Domain;

/// <summary>
/// Represents a task in the outbox for mail subscription operations. This entity is used to track tasks that need to be processed for managing mail subscriptions, such as adding or removing a user from a mail subscription list. Each task is associated with a specific user and includes information about when it was created, how many times it has been retried, and the type of task being performed.
/// </summary>
public class MailSubscriptionOutboxTask
{
    /// <summary>
    /// The unique identifier for the outbox task.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// The email address of the user associated with this outbox task. This is used to identify the target user for the mail subscription operation and is essential for processing the task correctly.
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// The timestamp indicating when the outbox task was created. This is used to track when the task was generated and can be useful for retry logic or auditing purposes.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// The timestamp indicating when this task is eligible for its next processing attempt.
    /// </summary>
    public DateTimeOffset NextAttemptAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// The number of times this outbox task has been retried. This is used to track how many times the task has been attempted, which can be useful for implementing retry logic or for monitoring the success of task processing.
    /// </summary>
    public int RetryCount { get; set; } = 0;

    /// <summary>
    /// The mail subscription associated with this outbox task. This is a required property that references the MailSubscriptions entity, which contains information about the specific mail subscription that this task is related to. This association allows for better organization and retrieval of tasks based on the related mail subscription.
    /// </summary>
    public required uint MailSubscription { get; set; }
}
