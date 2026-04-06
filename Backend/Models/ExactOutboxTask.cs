namespace Backend.Models;

public enum ExactTaskType { EnrollmentPayment, MembershipPayment }

public class ExactOutboxTask
{
    public long Id { get; set; }
    public uint PaymentId { get; set; }
    public Payment Payment { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public int RetryCount { get; set; } = 0;
    public ExactTaskType TaskType { get; set; } = ExactTaskType.EnrollmentPayment;
}