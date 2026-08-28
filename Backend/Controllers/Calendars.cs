using Backend.Controllers.DTOs;
using Backend.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    /// <summary>
    /// Controller for publishing personal iCalendar feeds of activity enrollments. The CalendarsController exposes the
    /// feed itself, which is fetched anonymously by calendar applications, together with endpoints that let a member
    /// retrieve and regenerate their own subscription URL. Because calendar applications cannot authenticate, the feed
    /// endpoint is deliberately anonymous and is protected solely by the unguessable identifier in its URL, which acts
    /// as a bearer secret. The controller interacts with the ICalendarService to perform the necessary business logic
    /// and data manipulation, ensuring a clean separation of concerns and maintainable code structure for publishing
    /// calendar feeds effectively within the application.
    /// </summary>
    /// <param name="calendarService">The calendar service for managing personal calendar feeds.</param>
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class CalendarsController(ICalendarService calendarService) : ControllerBase
    {
        /// <summary>
        /// Helper method to extract the unique identifier of the currently authenticated user from the request claims.
        /// </summary>
        /// <returns>A Guid representing the authenticated user's ID.</returns>
        private Guid GetUserId()
        {
            return Guid.Parse(User.Claims.First(c => c.Type == "UserId").Value);
        }

        // GET: calendars/00000000-0000-0000-0000-000000000000
        /// <summary>
        /// Retrieves the personal iCalendar feed identified by the given calendar identifier. The GetCalendar endpoint is
        /// what a calendar application such as Google Calendar or Outlook subscribes to, and is therefore anonymous: such
        /// applications poll the URL on a schedule and cannot present credentials. Access is instead controlled by the
        /// calendar identifier itself, which is an unguessable random value that a member can regenerate at any time to
        /// revoke a URL that has been shared or leaked. The endpoint also answers HEAD requests, which several calendar
        /// applications issue to probe a subscription before fetching it in full. An unknown identifier, or one belonging
        /// to a deleted member, yields a 404 without disclosing whether the identifier ever existed.
        /// </summary>
        /// <param name="calendarId">The unguessable identifier of the calendar feed to retrieve.</param>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>The personal iCalendar feed.</returns>
        [HttpGet("{calendarId:guid}")]
        [HttpHead("{calendarId:guid}")]
        [AllowAnonymous]
        [EnableCors("PublicCorsPolicy")]
        [Produces("text/calendar")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult> GetCalendar(Guid calendarId, CancellationToken ct)
        {
            string? calendar = await calendarService.GetPersonalCalendar(calendarId, ct);

            if (calendar == null)
                return NotFound();

            // Served inline rather than as a download: this URL is a live subscription feed that calendar
            // applications poll, not a file that is fetched once.
            return Content(calendar, "text/calendar; charset=utf-8");
        }

        // GET: calendars/me
        /// <summary>
        /// Retrieves the personal iCalendar feed URL of the currently authenticated member. The GetOwnFeedUrl endpoint
        /// allows the frontend to present the member with the URL they can subscribe to from their own calendar
        /// application. The URL contains a secret, so it is only ever returned to the member it belongs to and is
        /// deliberately not exposed through any other member endpoint, keeping the number of places the secret can leak
        /// from as small as possible.
        /// </summary>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>The authenticated member's personal calendar feed URL.</returns>
        [HttpGet("me")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(CalendarFeedUrlDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<CalendarFeedUrlDTO>> GetOwnFeedUrl(CancellationToken ct)
        {
            return Ok(await calendarService.GetOwnFeedUrl(GetUserId(), ct));
        }

        // POST: calendars/me/rotate
        /// <summary>
        /// Regenerates the personal iCalendar feed URL of the currently authenticated member. The RotateOwnFeedUrl
        /// endpoint immediately invalidates the previous URL and returns a freshly generated one, which is the only way
        /// for a member to revoke a feed URL that has been shared or leaked. Any calendar application still subscribed to
        /// the previous URL will start receiving a 404 and must be pointed at the new URL.
        /// </summary>
        /// <param name="ct">The cancellation token.</param>
        /// <returns>The authenticated member's new personal calendar feed URL.</returns>
        [HttpPost("me/rotate")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(CalendarFeedUrlDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorResponseDto), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<CalendarFeedUrlDTO>> RotateOwnFeedUrl(CancellationToken ct)
        {
            return Ok(await calendarService.RotateOwnCalendarId(GetUserId(), ct));
        }
    }
}
