namespace Backend.Models;

public enum KeycloakTaskType { Create, Sync, Delete }

public class KeyCloakOutboxTask
{
    public long Id { get; set; }
    public Guid KeycoakId { get; set; }
    public KeycloakTaskType TaskType { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public int RetryCount { get; set; } = 0;
}