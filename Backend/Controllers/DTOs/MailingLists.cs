using Backend.Models;

namespace Backend.Controllers.DTOs;

/// <summary>
/// Defines the DTO for curating a new mailing list.
/// </summary>
public class PostCuratedMailinglistDTO
{
    /// <summary>
    /// The mail subscription provider's identifier for the list to curate (e.g. a Mailchimp interest ID).
    /// </summary>
    public required string ProviderListId { get; set; }

    /// <summary>
    /// Where this list should be shown to members.
    /// </summary>
    public required MailinglistVisibility Visibility { get; set; }
}

/// <summary>
/// Defines the DTO for changing a curated mailing list's visibility.
/// </summary>
public class PatchCuratedMailinglistDTO
{
    /// <summary>
    /// The new visibility for the curated mailing list.
    /// </summary>
    public required MailinglistVisibility Visibility { get; set; }
}
