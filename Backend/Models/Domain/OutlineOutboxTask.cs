namespace Backend.Models.Domain;

/// <summary>
/// Identifies which Outline knowledge base operation an outbox task should perform.
/// </summary>
public enum OutlineTaskType
{
    /// <summary>
    /// Update a user'\''s email address in Outline.
    /// </summary>
    SyncMail,

    /// <summary>
    /// Delete a user from Outline.
    /// </summary>
    DeleteMail,

    /// <summary>
    /// Promote a user to admin and add them to the Board group in Outline.
    /// </summary>
    PromoteAdmin,

    /// <summary>
    /// Demote a user from admin and remove them from the Board group in Outline.
    /// </summary>
    DemoteAdmin
}

/// <summary>
/// Represents a queued Outline operation waiting to be processed in the background.
/// </summary>
public class OutlineOutboxTask
{
    /// <summary>
    /// The unique identifier for the outbox task.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// The type of Outline operation this task should perform.
    /// </summary>
    public required OutlineTaskType TaskType { get; set; }

    /// <summary>
    /// The target user'\''s current or previous email address.
    /// </summary>
    public required string Email { get; set; }

    /// <summary>
    /// The new email address for <see cref="OutlineTaskType.SyncMail"/> tasks.
    /// </summary>
    public string? NewEmail { get; set; }

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
