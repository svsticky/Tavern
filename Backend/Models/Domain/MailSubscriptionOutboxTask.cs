namespace Backend.Models.Domain;

/// <summary>
/// Identifies which mail subscription provider operation an outbox task should perform.
/// </summary>
public enum MailSubscriptionOutboxTaskType
{
    /// <summary>
    /// Replace a member's subscriptions with the set of list IDs carried by the task.
    /// </summary>
    UpdateSubscriptions,

    /// <summary>
    /// Remove a member from the mail subscription provider entirely.
    /// </summary>
    Delete,

    /// <summary>
    /// Move a member's subscriptions from an old email address to a new one.
    /// </summary>
    MigrateEmail
}

/// <summary>
/// Represents a task in the outbox for mail subscription operations. This entity is used to track tasks that need to be processed for managing mail subscriptions, such as updating a member's subscribed lists, removing a member, or migrating a member's subscriptions to a new email address. Each task includes information about when it was created, how many times it has been retried, and the type of operation being performed.
/// </summary>
public class MailSubscriptionOutboxTask
{
    /// <summary>
    /// The unique identifier for the outbox task.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// The type of mail subscription operation this task should perform.
    /// </summary>
    public required MailSubscriptionOutboxTaskType TaskType { get; set; }

    /// <summary>
    /// The target email address for this task. For <see cref="MailSubscriptionOutboxTaskType.MigrateEmail"/> this is the new email address.
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// The member's previous email address. Only set for <see cref="MailSubscriptionOutboxTaskType.MigrateEmail"/> tasks.
    /// </summary>
    public string? OldEmail { get; set; }

    /// <summary>
    /// The JSON-serialized list of mailing list IDs the member should be subscribed to. Only set for <see cref="MailSubscriptionOutboxTaskType.UpdateSubscriptions"/> tasks.
    /// </summary>
    public string? SubscribedListIdsJson { get; set; }

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
}
