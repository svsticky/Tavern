namespace Backend.Interfaces;

/// <summary>
/// Defines the contract for an external service that synchronizes administrative and board role status.
/// </summary>
public interface IAdminStatusUpdateService
{
    /// <summary>
    /// Updates the administrative/board status of a user in the external service.
    /// </summary>
    /// <param name="email">The email address of the target user.</param>
    /// <param name="isAdmin">Whether the user currently has administrative or board permissions.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateAdminStatusAsync(string email, bool isAdmin, CancellationToken ct);
}
