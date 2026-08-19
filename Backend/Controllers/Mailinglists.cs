using Backend.Controllers.DTOs;
using Backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

/// <summary>
/// Controller for retrieving and curating mailing lists. Which lists exist and who's subscribed is
/// managed entirely by the configured mail subscription provider (e.g. Mailchimp); this controller
/// exposes a filtered, read-only view of what's available there, plus board-only endpoints for
/// choosing which provider lists Tavern curates and in which context they're shown.
/// </summary>
/// <param name="mailSubscriptionService">The mail subscription provider service.</param>
/// <param name="curationService">The mailing list curation service.</param>
[Route("[controller]")]
[ApiController]
[Authorize]
public class Mailinglists(IMailSubscriptionService mailSubscriptionService, IMailinglistCurationService curationService) : ControllerBase
{
    /// <summary>
    /// Retrieves the unique identifier of the currently authenticated user from the request claims.
    /// </summary>
    /// <returns>The user ID.</returns>
    private Guid GetUserId() => Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);

    /// <summary>
    /// Retrieves every curated mailing list with General visibility, currently available at the
    /// configured mail subscription provider. This endpoint allows anonymous access so that the
    /// registration form can display the available lists before an account exists.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The list of mailing lists.</returns>
    [HttpGet]
    [AllowAnonymous]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IEnumerable<MailinglistDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<MailinglistDto>>> GetMailinglists(CancellationToken ct)
    {
        var visibleIds = await curationService.GetVisibleProviderListIds(includeYearlyRenewalOnly: false, ct);
        var providerLists = await mailSubscriptionService.GetAvailableMailinglistsAsync(ct);

        var result = providerLists.Where(l => visibleIds.Contains(l.Id));
        return Ok(result);
    }

    /// <summary>
    /// Retrieves the provider's available lists that have not yet been curated, for the admin "add mailing list" picker. Board members only.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The addable mailing lists.</returns>
    [HttpGet("addable")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IEnumerable<MailinglistDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<MailinglistDto>>> GetAddableMailinglists(CancellationToken ct)
    {
        var result = await curationService.GetAddableProviderMailinglists(GetUserId(), ct);
        return Ok(result);
    }

    /// <summary>
    /// Retrieves every curated mailing list for the admin management view, with live-resolved names. Board members only.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The curated mailing lists.</returns>
    [HttpGet("curated")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(IEnumerable<CuratedMailinglistDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IEnumerable<CuratedMailinglistDto>>> GetCuratedMailinglists(CancellationToken ct)
    {
        var result = await curationService.GetCuratedMailinglists(GetUserId(), ct);
        return Ok(result);
    }

    /// <summary>
    /// Curates a provider list, exposing it to members with the given visibility. Board members only.
    /// </summary>
    /// <param name="dto">The DTO containing the provider list to curate and its visibility.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The newly curated mailing list.</returns>
    [HttpPost]
    [Produces("application/json")]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(CuratedMailinglistDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CuratedMailinglistDto>> PostMailinglist([FromBody] PostCuratedMailinglistDTO dto, CancellationToken ct)
    {
        var result = await curationService.AddMailinglist(dto.ProviderListId, dto.Visibility, GetUserId(), ct);
        return CreatedAtAction(nameof(GetCuratedMailinglists), null, result);
    }

    /// <summary>
    /// Changes the visibility of an existing curated mailing list. Board members only.
    /// </summary>
    /// <param name="id">The curation record's identifier.</param>
    /// <param name="dto">The DTO containing the new visibility.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpPatch("{id}")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> PatchMailinglist(int id, [FromBody] PatchCuratedMailinglistDTO dto, CancellationToken ct)
    {
        await curationService.UpdateMailinglistVisibility(id, dto.Visibility, GetUserId(), ct);
        return NoContent();
    }

    /// <summary>
    /// Un-curates a mailing list. This never touches the actual list at the provider. Board members only.
    /// </summary>
    /// <param name="id">The curation record's identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>No content.</returns>
    [HttpDelete("{id}")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> DeleteMailinglist(int id, CancellationToken ct)
    {
        await curationService.DeleteMailinglist(id, GetUserId(), ct);
        return NoContent();
    }
}
