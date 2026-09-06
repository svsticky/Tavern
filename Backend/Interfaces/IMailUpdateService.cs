namespace Backend.Interfaces;

/// <summary>
/// Defines the contract for an external service that synchronizes user email changes and account deletions.
/// </summary>
public interface IMailUpdateService
{
    /// <summary>
    /// Synchronizes a user's updated email address with the external service.
    /// </summary>
    /// <param name="oldEmail">The user's previous email address.</param>
    /// <param name="newEmail">The user's new email address.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SyncMailAsync(string oldEmail, string newEmail, CancellationToken ct);

    /// <summary>
    /// Deletes or cleans up a user's record in the external service when their account is deleted.
    /// </summary>
    /// <param name="email">The email address of the deleted user.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteMailAsync(string email, CancellationToken ct);
}
