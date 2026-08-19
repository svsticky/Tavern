using Microsoft.EntityFrameworkCore;

namespace Backend.Models.Domain;

/// <summary>
/// Records that an admin has chosen to expose a specific mail subscription provider list (e.g. a
/// Mailchimp interest) in Tavern, and in which context. This is a curation record only - it holds
/// no display name (that's always resolved live against the provider, so it can never drift) and
/// deleting it never touches the actual list at the provider.
/// </summary>
[PrimaryKey(nameof(Id))]
[Index(nameof(ProviderListId), IsUnique = true)]
public class CuratedMailinglist
{
    /// <summary>
    /// The unique identifier for the curation record.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The mail subscription provider's own identifier for the list (e.g. a Mailchimp interest ID).
    /// </summary>
    public required string ProviderListId { get; set; }

    /// <summary>
    /// Where this list is shown to members.
    /// </summary>
    public MailinglistVisibility Visibility { get; set; } = MailinglistVisibility.General;
}
