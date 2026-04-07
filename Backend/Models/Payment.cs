namespace Backend.Models;

public abstract class Payment 
{
    public uint Id { get; set; }

    public decimal Price { get; set; }
    
    public required string MollieId { get; set; }

    public required string PaymentIntentUrl { get; set; }

    public string? PaidAt { get; set; }

    public Guid? MemberId { get; set; }
   
    public Member? Member { get; set; }

    public Guid? ExactEntryId { get; set; }
}

public class MembershipPayment : Payment
{

}

public class EnrollmentPayment : Payment
{
    public uint? ActivityId { get; set; }

    public Activity? Activity { get; set; } = null!;
}

public class EnrollmentBalance
{
    public required Enrollment Enrollment { get; set; }
    public required decimal Balance { get; set; }
}