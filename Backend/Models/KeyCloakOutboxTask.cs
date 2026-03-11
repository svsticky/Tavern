public class KeyCloakOutboxTask
{
    public long Id { get; set; }
    public Guid MemberId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int RetryCount { get; set; } = 0;
}