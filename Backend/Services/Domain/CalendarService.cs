using Backend.Controllers.DTOs;
using Backend.Database;
using Backend.Interfaces;
using Backend.Models.Domain;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Backend.Services.Domain;

/// <summary>
/// Implements the publication of personal iCalendar feeds containing a member's activity enrollments.
/// </summary>
public class CalendarService : ICalendarService
{
    /// <summary>
    /// The timezone the association operates in, which defines what "a day" means for whole-day activities.
    /// </summary>
    /// <remarks>
    /// Activity timestamps are persisted as UTC instants, so the wall-clock time a board member entered is only
    /// recoverable by converting back to the association's own timezone. Reading the date straight off the UTC
    /// value would place a whole-day activity on the wrong calendar day for half the year.
    /// </remarks>
    private static readonly TimeZoneInfo _associationTimeZone = ResolveAssociationTimeZone();

    /// <summary>
    /// The local end-of-day time that, combined with a local start of midnight, marks an activity as whole-day.
    /// </summary>
    private static readonly TimeSpan _wholeDayEndTime = new(23, 59, 0);

    private const string _productId = "-//Study association Sticky//Tavern//EN";
    private const string _uidDomain = "tavern.svsticky.nl";

    private readonly PostgresDbContext _db;
    private readonly ILogger<CalendarService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CalendarService"/> class with the specified database context
    /// and logger. The database context provides access to the members and their enrollments that make up a feed,
    /// while the logger records feed generation and calendar identifier rotations for troubleshooting.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="logger">The logger.</param>
    public CalendarService(PostgresDbContext db, ILogger<CalendarService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<string?> GetPersonalCalendar(Guid calendarId, CancellationToken ct)
    {
        // An all-zero identifier is trivially guessable, so it must never resolve to a feed even if a row were
        // somehow persisted without a generated identifier.
        if (calendarId == Guid.Empty)
            return null;

        var member = await _db.Members
            .AsNoTracking()
            .Where(m => m.CalendarId == calendarId && !m.IsDeleted)
            .Select(m => new { m.Id, m.PreferredLanguage })
            .FirstOrDefaultAsync(ct);

        if (member == null)
            return null;

        var enrollments = await _db.Enrollments
            .AsNoTracking()
            .Include(e => e.Activity)
            .Where(e => e.MemberId == member.Id)
            .OrderBy(e => e.Activity.DateTimeStart)
            .ToListAsync(ct);

        return BuildCalendar(enrollments, member.PreferredLanguage, DateTime.UtcNow);
    }

    /// <inheritdoc />
    public async Task<CalendarFeedUrlDTO> GetOwnFeedUrl(Guid userId, CancellationToken ct)
    {
        var member = await _db.Members
            .AsNoTracking()
            .Where(m => m.Id == userId && !m.IsDeleted)
            .Select(m => new { m.CalendarId })
            .FirstOrDefaultAsync(ct);

        if (member == null)
            throw new KeyNotFoundException($"Member with ID {userId} not found.");

        return new CalendarFeedUrlDTO { Url = BuildFeedUrl(member.CalendarId) };
    }

    /// <inheritdoc />
    public async Task<CalendarFeedUrlDTO> RotateOwnCalendarId(Guid userId, CancellationToken ct)
    {
        var member = await _db.Members.FirstOrDefaultAsync(m => m.Id == userId && !m.IsDeleted, ct);

        if (member == null)
            throw new KeyNotFoundException($"Member with ID {userId} not found.");

        member.CalendarId = Guid.NewGuid();
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Rotated the calendar feed identifier of member {MemberId}.", userId);

        return new CalendarFeedUrlDTO { Url = BuildFeedUrl(member.CalendarId) };
    }

    /// <summary>
    /// Serializes the given enrollments into an iCalendar document. A member without any enrollments yields an
    /// empty but valid calendar, which calendar clients accept and simply render as an empty subscription.
    /// </summary>
    /// <param name="enrollments">The enrollments to include, each with its activity loaded.</param>
    /// <param name="language">The language to render activity descriptions and labels in.</param>
    /// <param name="generatedAtUtc">The moment the feed was generated, reported to the member in each event.</param>
    /// <returns>The serialized iCalendar document.</returns>
    internal static string BuildCalendar(IEnumerable<Enrollment> enrollments, Language language, DateTime generatedAtUtc)
    {
        var calendar = new Calendar
        {
            Method = "PUBLISH",
            ProductId = _productId
        };
        calendar.AddProperty("X-WR-CALNAME", CalendarName(language));

        foreach (Enrollment enrollment in enrollments)
            calendar.Events.Add(ToCalendarEvent(enrollment, language, generatedAtUtc));

        return new CalendarSerializer().SerializeToString(calendar)
            ?? throw new InvalidOperationException("Failed to serialize the iCalendar document.");
    }

    /// <summary>
    /// Converts a single enrollment into an iCalendar event, marking enrollments that are still on the waiting
    /// list both in the summary and through the standard tentative status.
    /// </summary>
    /// <param name="enrollment">The enrollment to convert, with its activity loaded.</param>
    /// <param name="language">The language to render the description and labels in.</param>
    /// <param name="generatedAtUtc">The moment the feed was generated.</param>
    /// <returns>The resulting iCalendar event.</returns>
    private static CalendarEvent ToCalendarEvent(Enrollment enrollment, Language language, DateTime generatedAtUtc)
    {
        Activity activity = enrollment.Activity;
        (CalDateTime start, CalDateTime end) = ToCalendarRange(activity);

        var calendarEvent = new CalendarEvent
        {
            Uid = $"activity-{activity.Id}@{_uidDomain}",
            DtStamp = new CalDateTime(generatedAtUtc, "UTC", true),
            DtStart = start,
            DtEnd = end,
            Summary = enrollment.IsOnWaitingList
                ? $"[{WaitingListLabel(language)}] {activity.Name}"
                : activity.Name,
            Location = activity.Location,
            Description = BuildDescription(activity, language, generatedAtUtc)
        };

        if (enrollment.IsOnWaitingList)
            calendarEvent.Status = "TENTATIVE";

        return calendarEvent;
    }

    /// <summary>
    /// Determines the start and end of an activity as they should appear in the feed.
    /// </summary>
    /// <param name="activity">The activity to convert.</param>
    /// <returns>The start and end of the activity in iCalendar form.</returns>
    private static (CalDateTime Start, CalDateTime End) ToCalendarRange(Activity activity)
    {
        DateTimeOffset localStart = TimeZoneInfo.ConvertTime(activity.DateTimeStart, _associationTimeZone);
        DateTimeOffset localEnd = TimeZoneInfo.ConvertTime(activity.DateTimeEnd, _associationTimeZone);

        if (IsWholeDay(localStart, localEnd, out DateOnly startDate, out DateOnly exclusiveEndDate))
        {
            // The end of a whole-day event is exclusive, so it points at the day after the final day. Deliberately
            // no conversion to UTC here: a whole-day event is a date, and converting it would shift that date.
            return (new CalDateTime(startDate.Year, startDate.Month, startDate.Day),
                    new CalDateTime(exclusiveEndDate.Year, exclusiveEndDate.Month, exclusiveEndDate.Day));
        }

        return (new CalDateTime(activity.DateTimeStart.UtcDateTime, "UTC", true),
                new CalDateTime(activity.DateTimeEnd.UtcDateTime, "UTC", true));
    }

    /// <summary>
    /// Checks whether the given local start and end times represent a whole-day activity.
    /// </summary>
    private static bool IsWholeDay(DateTimeOffset localStart, DateTimeOffset localEnd, out DateOnly startDate, out DateOnly exclusiveEndDate)
    {
        startDate = default;
        exclusiveEndDate = default;

        if (localStart.TimeOfDay != TimeSpan.Zero)
            return false;

        startDate = DateOnly.FromDateTime(localStart.Date);

        if (localEnd.TimeOfDay.Hours == 23 && localEnd.TimeOfDay.Minutes == 59)
        {
            exclusiveEndDate = DateOnly.FromDateTime(localEnd.Date).AddDays(1);
            return true;
        }

        if (localEnd.TimeOfDay == TimeSpan.Zero && localEnd.Date > localStart.Date)
        {
            exclusiveEndDate = DateOnly.FromDateTime(localEnd.Date);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Composes the event description, combining a link back to the activity in Tavern, the activity description
    /// in the member's own language, and the moment the feed was last generated.
    /// </summary>
    /// <param name="activity">The activity to describe.</param>
    /// <param name="language">The language to render the description in.</param>
    /// <param name="generatedAtUtc">The moment the feed was generated.</param>
    /// <returns>The composed description.</returns>
    private static string BuildDescription(Activity activity, Language language, DateTime generatedAtUtc)
    {
        var builder = new StringBuilder();
        builder.Append($"{TrimTrailingSlash(Environment.GetEnvironmentVariable("HostUrl"))}/activities/{activity.Id}");

        string description = activity.GetDescription(language);
        if (!string.IsNullOrWhiteSpace(description))
            builder.Append($"\n\n{description}");

        builder.Append($"\n\n{LastSyncedLabel(language)}: {generatedAtUtc:yyyy-MM-dd HH:mm} UTC");

        return builder.ToString();
    }

    /// <summary>
    /// Composes the absolute URL under which a feed is published.
    /// </summary>
    /// <param name="calendarId">The unguessable identifier of the feed.</param>
    /// <returns>The absolute feed URL.</returns>
    private static string BuildFeedUrl(Guid calendarId)
    {
        return $"{TrimTrailingSlash(Environment.GetEnvironmentVariable("ApiUrl"))}/calendars/{calendarId}";
    }

    /// <summary>
    /// Removes a trailing slash from a configured base URL so that it can be concatenated with a path safely.
    /// </summary>
    /// <param name="url">The configured base URL, which may be absent.</param>
    /// <returns>The base URL without a trailing slash.</returns>
    private static string TrimTrailingSlash(string? url)
    {
        return (url ?? string.Empty).TrimEnd('/');
    }

    /// <summary>
    /// Resolves the association's timezone from the AssociationTimeZone environment variable, falling back to
    /// Europe/Amsterdam when it is not configured. A configured but unknown timezone identifier deliberately
    /// throws rather than silently falling back, because quietly using the wrong timezone would place whole-day
    /// activities on the wrong calendar day without anyone noticing.
    /// </summary>
    /// <returns>The timezone the association operates in.</returns>
    private static TimeZoneInfo ResolveAssociationTimeZone()
    {
        string timeZoneId = Environment.GetEnvironmentVariable("AssociationTimeZone") ?? "Europe/Amsterdam";
        return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
    }

    /// <summary>Gets the display name of the calendar in the given language.</summary>
    /// <param name="language">The language to use.</param>
    /// <returns>The localized calendar name.</returns>
    private static string CalendarName(Language language) => language switch
    {
        Language.NL => "Sticky Activiteiten",
        Language.EN => "Sticky Activities",
        _ => "Sticky Activities"
    };

    /// <summary>Gets the summary prefix marking a waiting list enrollment in the given language.</summary>
    /// <param name="language">The language to use.</param>
    /// <returns>The localized waiting list label.</returns>
    private static string WaitingListLabel(Language language) => language switch
    {
        Language.NL => "WACHTLIJST",
        Language.EN => "WAITING LIST",
        _ => "WAITING LIST"
    };

    /// <summary>Gets the label introducing the synchronization timestamp in the given language.</summary>
    /// <param name="language">The language to use.</param>
    /// <returns>The localized last-synced label.</returns>
    private static string LastSyncedLabel(Language language) => language switch
    {
        Language.NL => "Laatst gesynchroniseerd op",
        Language.EN => "Last synced on",
        _ => "Last synced on"
    };
}
