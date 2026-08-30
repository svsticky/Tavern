namespace Backend.Controllers.DTOs;

/// <summary>
/// Represents the personal iCalendar feed URL of the requesting member. This DTO is returned when a member asks
/// for their own subscription link, and after the member regenerates that link, so the frontend can present the
/// current URL for copying into a calendar application.
/// </summary>
public class CalendarFeedUrlDTO
{
    /// <summary>
    /// The absolute URL of the member's personal iCalendar feed. Anyone in possession of this URL can read the
    /// member's activity enrollments, so it must be treated as a secret and never shared.
    /// </summary>
    public required string Url { get; set; }
}
