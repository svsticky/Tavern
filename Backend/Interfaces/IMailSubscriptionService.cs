namespace Backend.Interfaces;

/// <summary>
/// Represents a single mailing list as known by the mail subscription provider.
/// </summary>
/// <param name="Id">The provider's own identifier for the list (e.g. a Mailchimp interest ID).</param>
/// <param name="Name">The human-readable name of the list.</param>
public record MailinglistDto(string Id, string Name);

/// <summary>
/// Represents a mailing list together with whether a specific member is currently subscribed to it.
/// </summary>
/// <param name="Id">The provider's own identifier for the list (e.g. a Mailchimp interest ID).</param>
/// <param name="Name">The human-readable name of the list.</param>
/// <param name="Subscribed">Whether the member is currently subscribed to this list.</param>
public record MemberMailinglistDto(string Id, string Name, bool Subscribed);

/// <summary>
/// Defines the contract for a mail subscription service that manages mailing lists and member subscriptions against an external provider (such as Mailchimp). Implementations are the sole source of truth for which lists exist and which members are subscribed to them - no subscription state is mirrored locally.
/// </summary>
public interface IMailSubscriptionService
{
    /// <summary>
    /// Retrieves every mailing list currently available at the provider.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The available mailing lists.</returns>
    Task<IEnumerable<MailinglistDto>> GetAvailableMailinglistsAsync(CancellationToken ct);

    /// <summary>
    /// Retrieves every mailing list together with whether the given member is currently subscribed to it.
    /// </summary>
    /// <param name="email">The email address of the member.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The mailing lists with the member's subscription status.</returns>
    Task<IEnumerable<MemberMailinglistDto>> GetMemberMailinglistsAsync(string email, CancellationToken ct);

    /// <summary>
    /// Replaces a member's mailing list subscriptions with the given set of list IDs.
    /// </summary>
    /// <param name="email">The email address of the member.</param>
    /// <param name="subscribedListIds">The IDs of the lists the member should be subscribed to.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task UpdateMemberSubscriptionsAsync(string email, IEnumerable<string> subscribedListIds, CancellationToken ct);

    /// <summary>
    /// Removes a member from the mail subscription provider entirely.
    /// </summary>
    /// <param name="email">The email address of the member.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DeleteMemberAsync(string email, CancellationToken ct);

    /// <summary>
    /// Moves a member's subscriptions from an old email address to a new one, archiving the old record.
    /// </summary>
    /// <param name="oldEmail">The member's previous email address.</param>
    /// <param name="newEmail">The member's new email address.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task MigrateEmailAsync(string oldEmail, string newEmail, CancellationToken ct);
}
