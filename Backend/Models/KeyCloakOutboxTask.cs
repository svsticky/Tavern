public enum KeycloakTaskType { Create, Sync, Delete }

public class KeyCloakOutboxTask
{
    public long Id { get; set; }
    public Guid KeycoakId { get; set; }
    public KeycloakTaskType TaskType { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int RetryCount { get; set; } = 0;
}