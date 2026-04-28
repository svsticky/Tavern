using Backend.Models;

namespace Backend.Interfaces;

/// <summary>
/// Interface for the mail subscription service, which manages mail subscriptions and processes mail subscription outbox tasks. This service provides methods for enqueuing mail subscription tasks and handling the processing of these tasks in a background worker. The service is designed to work with a database context to persist tasks and includes logging for monitoring the enqueuing and processing of tasks.
/// </summary>
public interface IMailSubscriptionService
{
    /// <summary>
    /// Updates the mail subscription for a given email address. This method is responsible for enqueuing a mail subscription task that will be processed by the background worker. The task includes the email address, the mail subscription details, and a cancellation token for handling task cancellation. The method ensures that the mail subscription update is properly logged and persisted in the database for later processing.
    /// </summary>
    /// <param name="email">The email address for which to update the subscription.</param>
    /// <param name="mailSubscription">The mail subscription details.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task UpdateSubscriptionAsync(string email, uint mailSubscription, CancellationToken ct);
}