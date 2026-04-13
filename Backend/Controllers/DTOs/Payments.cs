namespace Backend.Controllers.DTOs;

public class PostMembershipPaymentDTO
{
    public Guid MemberId { get; set; }
}

public class PostActivityPaymentDTO
{
    public Guid MemberId { get; set; }
    public List<uint> ActivityIds { get; set; } = new();
    public bool ManuallyMarkedAsPaid { get; set; } = false;
}

public class PostPaymentResponse
{
    public required string CheckoutUrl { get; set; }
}