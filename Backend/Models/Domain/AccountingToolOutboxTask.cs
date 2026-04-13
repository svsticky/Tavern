namespace Backend.Models.Domain;

public enum AccountingToolTaskType { EnrollmentPayment, MembershipPayment, MollieFeePayment }

public class AccountingToolOutboxTask
{
    public long Id { get; set; }
    public uint PaymentId { get; set; }
    public Payment Payment { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public int RetryCount { get; set; } = 0;
    public AccountingToolTaskType TaskType { get; set; } = AccountingToolTaskType.EnrollmentPayment;
}