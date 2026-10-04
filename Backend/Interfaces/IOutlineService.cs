namespace Backend.Interfaces;

/// <summary>
/// Service interface for synchronizing users and administrative roles with Outline.
/// </summary>
public interface IOutlineService : IMailUpdateService, IAdminStatusUpdateService
{
}
