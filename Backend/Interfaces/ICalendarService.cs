using Backend.Controllers.DTOs;

namespace Backend.Interfaces;

/// <summary>
/// Defines the contract for publishing personal iCalendar feeds of activity enrollments.
/// </summary>
public interface ICalendarService
{
    /// <summary>
    /// Builds the personal iCalendar feed belonging to the given calendar identifier. The identifier is an
    /// unguessable secret rather than a member id, because calendar clients cannot authenticate and therefore
    /// fetch the feed anonymously.
    /// </summary>
    /// <param name="calendarId">The unguessable identifier of the feed.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The serialized iCalendar document, or null when no active member owns the identifier.</returns>
    Task<string?> GetPersonalCalendar(Guid calendarId, CancellationToken ct);

    /// <summary>
    /// Retrieves the personal iCalendar feed URL of the given member.
    /// </summary>
    /// <param name="userId">The unique identifier of the member.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The member's current feed URL.</returns>
    Task<CalendarFeedUrlDTO> GetOwnFeedUrl(Guid userId, CancellationToken ct);

    /// <summary>
    /// Generates a new calendar identifier for the given member, immediately invalidating the previous feed URL.
    /// This is the only way to revoke a feed URL that has been shared or leaked.
    /// </summary>
    /// <param name="userId">The unique identifier of the member.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The member's new feed URL.</returns>
    Task<CalendarFeedUrlDTO> RotateOwnCalendarId(Guid userId, CancellationToken ct);
}
