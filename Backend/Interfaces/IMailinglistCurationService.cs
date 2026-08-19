using Backend.Models;

namespace Backend.Interfaces;

/// <summary>
/// Represents a curated mailing list together with its live-resolved display name and whether the
/// underlying provider list still exists.
/// </summary>
/// <param name="Id">The curation record's own identifier.</param>
/// <param name="ProviderListId">The mail subscription provider's identifier for the list.</param>
/// <param name="Name">The list's current display name at the provider, or <c>null</c> if it's orphaned.</param>
/// <param name="Visibility">Where this list is shown to members.</param>
/// <param name="Orphaned">Whether the curated provider list no longer exists at the provider.</param>
public record CuratedMailinglistDto(int Id, string ProviderListId, string? Name, MailinglistVisibility Visibility, bool Orphaned);

/// <summary>
/// Defines the contract for curating which mail subscription provider lists Tavern exposes to
/// members, and in which context. This sits above <see cref="IMailSubscriptionService"/> - it knows
/// nothing provider-specific, it only decides which of the provider's lists are shown, and where.
/// </summary>
public interface IMailinglistCurationService
{
    /// <summary>
    /// Retrieves every curated mailing list for the admin management view, with live-resolved names
    /// and an <c>Orphaned</c> flag for entries whose provider list no longer exists. Board members only.
    /// </summary>
    /// <param name="userId">The ID of the user performing the action.</param>
    /// <param name="ct">The cancellation token.</param>
    Task<IEnumerable<CuratedMailinglistDto>> GetCuratedMailinglists(Guid userId, CancellationToken ct);

    /// <summary>
    /// Retrieves the provider's available lists that have not yet been curated, for the admin "add
    /// mailing list" picker. Board members only.
    /// </summary>
    /// <param name="userId">The ID of the user performing the action.</param>
    /// <param name="ct">The cancellation token.</param>
    Task<IEnumerable<MailinglistDto>> GetAddableProviderMailinglists(Guid userId, CancellationToken ct);

    /// <summary>
    /// Curates a provider list, exposing it to members with the given visibility.
    /// </summary>
    /// <param name="providerListId">The mail subscription provider's identifier for the list.</param>
    /// <param name="visibility">Where this list should be shown to members.</param>
    /// <param name="userId">The ID of the user performing the action.</param>
    /// <param name="ct">The cancellation token.</param>
    Task<CuratedMailinglistDto> AddMailinglist(string providerListId, MailinglistVisibility visibility, Guid userId, CancellationToken ct);

    /// <summary>
    /// Changes the visibility of an existing curated mailing list.
    /// </summary>
    /// <param name="id">The curation record's identifier.</param>
    /// <param name="visibility">The new visibility.</param>
    /// <param name="userId">The ID of the user performing the action.</param>
    /// <param name="ct">The cancellation token.</param>
    Task UpdateMailinglistVisibility(int id, MailinglistVisibility visibility, Guid userId, CancellationToken ct);

    /// <summary>
    /// Un-curates a mailing list. This never touches the actual list at the provider - members
    /// already subscribed to it stay subscribed, it simply stops being offered/shown in Tavern.
    /// </summary>
    /// <param name="id">The curation record's identifier.</param>
    /// <param name="userId">The ID of the user performing the action.</param>
    /// <param name="ct">The cancellation token.</param>
    Task DeleteMailinglist(int id, Guid userId, CancellationToken ct);

    /// <summary>
    /// Retrieves the provider list IDs that should be visible for the given context: General lists
    /// are always included, YearlyRenewalOnly lists are included only when requested.
    /// </summary>
    /// <param name="includeYearlyRenewalOnly">Whether to also include YearlyRenewalOnly lists.</param>
    /// <param name="ct">The cancellation token.</param>
    Task<IReadOnlySet<string>> GetVisibleProviderListIds(bool includeYearlyRenewalOnly, CancellationToken ct);
}
