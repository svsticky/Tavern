namespace Backend.Models.Domain;

/// <summary>
/// Defines the type of an accounting tool task. This enumeration is used to specify the specific type of task that needs to be processed for the accounting tool
/// </summary>
public enum AccountingToolTaskType { 
    /// <summary>
    /// Indicates that the task is to process an enrollment payment. This task type is used when a payment related to an enrollment needs to be processed in the accounting tool, ensuring that the financial records are updated accordingly for the enrollment transaction.
    /// </summary>
    EnrollmentPayment, 

    /// <summary>
    /// Indicates that the task is to process a membership payment. This task type is used when a payment related to a membership needs to be processed in the accounting tool, ensuring that the financial records are updated accordingly for the membership transaction.
    /// </summary>
    MembershipPayment,

    /// <summary>
    /// Indicates that the task is to process a Payment Service fee payment. This task type is used when a payment related to a fee from the Payment Service payment provider needs to be processed in the accounting tool, ensuring that the financial records are updated accordingly for the fee transaction.
    /// </summary>
    PaymentServiceFeePayment 
}

/// <summary>
/// Represents a task in the outbox for the accounting tool. This entity is used to track tasks that need to be processed for the accounting tool, such as processing payments or updating financial records. Each task is associated with a specific payment and includes information about when it was created, how many times it has been retried, and the type of task being performed.
/// </summary>
public class AccountingToolOutboxTask
{
    /// <summary>
    /// The unique identifier for the outbox task.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// The identifier of the payment associated with this outbox task. This is a foreign key referencing the Payment entity.
    /// </summary>
    public uint PaymentId { get; set; }

    /// <summary>
    /// The payment associated with this outbox task. This is a navigation property that allows access to the related Payment entity.
    /// </summary>
    public Payment Payment { get; set; } = null!;

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
    /// The type of the accounting tool task. This is an enumeration that indicates the specific type of task being performed, such as processing an enrollment payment, a membership payment, or a Payment Service fee payment. This information can be used to determine how the task should be handled and processed.
    /// </summary>
    public AccountingToolTaskType TaskType { get; set; } = AccountingToolTaskType.EnrollmentPayment;
}