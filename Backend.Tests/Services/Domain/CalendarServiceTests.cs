using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Backend.Database;
using Backend.Models.Domain;
using Backend.Services.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Backend.Tests.Services.Domain;

public class CalendarServiceTests : IDisposable
{
    private readonly PostgresDbContext _db;
    private readonly CalendarService _service;

    /// <summary>Europe/Amsterdam is UTC+2 in June, so a local midnight is 22:00 UTC the day before.</summary>
    private static readonly TimeSpan SummerOffset = TimeSpan.FromHours(2);

    /// <summary>Europe/Amsterdam is UTC+1 in January.</summary>
    private static readonly TimeSpan WinterOffset = TimeSpan.FromHours(1);

    public CalendarServiceTests()
    {
        Environment.SetEnvironmentVariable("HostUrl", "https://tavern.svsticky.nl/");
        Environment.SetEnvironmentVariable("ApiUrl", "https://api.tavern.svsticky.nl/");

        var options = new DbContextOptionsBuilder<PostgresDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _db = new PostgresDbContext(options);
        _db.Database.EnsureCreated();

        _service = new CalendarService(_db, NullLogger<CalendarService>.Instance);
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
        GC.SuppressFinalize(this);
    }

    private static Activity BuildActivity(uint id, DateTimeOffset start, DateTimeOffset end, string name = "Borrel")
    {
        return new Activity
        {
            Id = id,
            Name = name,
            Location = "Tavern",
            DutchDescription = "Nederlandse omschrijving",
            EnglishDescription = "English description",
            DateTimeStart = start,
            DateTimeEnd = end,
            PaymentDeadline = end
        };
    }

    private static Member BuildMember(Guid calendarId, Language language = Language.EN, bool isDeleted = false)
    {
        return new Member
        {
            Id = Guid.NewGuid(),
            CalendarId = calendarId,
            StudentNumber = "1234567",
            FirstName = "Jan",
            LastName = "Jansen",
            Email = $"jan-{Guid.NewGuid()}@example.com",
            PhoneNumber = "0612345678",
            Street = "Street",
            HouseNumber = "1",
            PostalCode = "1234AB",
            City = "Utrecht",
            PreferredLanguage = language,
            IsDeleted = isDeleted
        };
    }

    private async Task<Member> SeedMemberWithEnrollment(Activity activity, bool isOnWaitingList = false, Language language = Language.EN)
    {
        var member = BuildMember(Guid.NewGuid(), language);
        _db.Members.Add(member);
        _db.Activities.Add(activity);
        _db.Enrollments.Add(new Enrollment
        {
            ActivityId = activity.Id,
            MemberId = member.Id,
            IsOnWaitingList = isOnWaitingList,
            RegisteredOn = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        return member;
    }

    /// <summary>
    /// Reverses RFC 5545 line folding, which breaks long property values across continuation lines. Without
    /// this, asserting on a value such as a description would fail purely because of where the fold landed.
    /// </summary>
    private static string Unfold(string ics)
    {
        return ics.Replace("\r\n ", string.Empty).Replace("\n ", string.Empty);
    }

    private static List<Enrollment> SingleEnrollment(Activity activity, bool isOnWaitingList = false)
    {
        return new List<Enrollment>
        {
            new Enrollment
            {
                ActivityId = activity.Id,
                Activity = activity,
                MemberId = Guid.NewGuid(),
                IsOnWaitingList = isOnWaitingList,
                RegisteredOn = DateTime.UtcNow
            }
        };
    }

    [Fact]
    public async Task GetPersonalCalendar_UnknownCalendarId_ReturnsNull()
    {
        var result = await _service.GetPersonalCalendar(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetPersonalCalendar_EmptyCalendarId_ReturnsNullWithoutQuerying()
    {
        // A member row that somehow never received a generated identifier must not become publicly readable
        // through the trivially guessable all-zero GUID.
        _db.Members.Add(BuildMember(Guid.Empty));
        await _db.SaveChangesAsync();

        var result = await _service.GetPersonalCalendar(Guid.Empty, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetPersonalCalendar_DeletedMember_ReturnsNull()
    {
        var calendarId = Guid.NewGuid();
        _db.Members.Add(BuildMember(calendarId, isDeleted: true));
        await _db.SaveChangesAsync();

        var result = await _service.GetPersonalCalendar(calendarId, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetPersonalCalendar_MemberWithoutEnrollments_ReturnsEmptyButValidCalendar()
    {
        var calendarId = Guid.NewGuid();
        _db.Members.Add(BuildMember(calendarId));
        await _db.SaveChangesAsync();

        var result = await _service.GetPersonalCalendar(calendarId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Contains("BEGIN:VCALENDAR", result);
        Assert.Contains("END:VCALENDAR", result);
        Assert.DoesNotContain("BEGIN:VEVENT", result);
    }

    [Fact]
    public async Task GetPersonalCalendar_EnrolledMember_IncludesTheActivity()
    {
        var activity = BuildActivity(1,
            new DateTimeOffset(2026, 6, 1, 18, 30, 0, SummerOffset),
            new DateTimeOffset(2026, 6, 1, 22, 0, 0, SummerOffset));
        var member = await SeedMemberWithEnrollment(activity);

        var result = await _service.GetPersonalCalendar(member.CalendarId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Contains("SUMMARY:Borrel", result);
        Assert.Contains("UID:activity-1@tavern.svsticky.nl", result);
    }

    [Fact]
    public async Task GetPersonalCalendar_IncludesPastActivities()
    {
        var activity = BuildActivity(2,
            new DateTimeOffset(2020, 1, 5, 10, 0, 0, WinterOffset),
            new DateTimeOffset(2020, 1, 5, 12, 0, 0, WinterOffset),
            "Oude activiteit");
        var member = await SeedMemberWithEnrollment(activity);

        var result = await _service.GetPersonalCalendar(member.CalendarId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Contains("SUMMARY:Oude activiteit", result);
    }

    [Fact]
    public void BuildCalendar_TimedActivity_IsSerializedAsUtcInstant()
    {
        var activity = BuildActivity(1,
            new DateTimeOffset(2026, 6, 1, 18, 30, 0, SummerOffset),
            new DateTimeOffset(2026, 6, 1, 22, 0, 0, SummerOffset));

        string ics = CalendarService.BuildCalendar(SingleEnrollment(activity), Language.EN, DateTime.UtcNow);

        Assert.Contains("DTSTART:20260601T163000Z", ics);
        Assert.Contains("DTEND:20260601T200000Z", ics);
        Assert.DoesNotContain("VALUE=DATE", ics);
    }

    [Fact]
    public void BuildCalendar_WholeDayActivity_IsSerializedAsDateValueWithExclusiveEnd()
    {
        // Europe/Amsterdam is UTC+2 in summer. Local midnight is 22:00 UTC previous day, local 23:59 is 21:59 UTC.
        var activity = BuildActivity(1,
            new DateTimeOffset(2026, 6, 1, 0, 0, 0, SummerOffset),
            new DateTimeOffset(2026, 6, 1, 23, 59, 0, SummerOffset));

        string ics = CalendarService.BuildCalendar(SingleEnrollment(activity), Language.EN, DateTime.UtcNow);

        Assert.Contains("DTSTART;VALUE=DATE:20260601", ics);
        Assert.Contains("DTEND;VALUE=DATE:20260602", ics);
    }

    [Fact]
    public void BuildCalendar_MultiDayWholeDayActivity_IsSerializedAsDateValueWithExclusiveEnd()
    {
        var activity = BuildActivity(1,
            new DateTimeOffset(2026, 6, 1, 0, 0, 0, SummerOffset),
            new DateTimeOffset(2026, 6, 3, 23, 59, 0, SummerOffset));

        string ics = CalendarService.BuildCalendar(SingleEnrollment(activity), Language.EN, DateTime.UtcNow);

        Assert.Contains("DTSTART;VALUE=DATE:20260601", ics);
        Assert.Contains("DTEND;VALUE=DATE:20260604", ics);
    }

    [Fact]
    public void BuildCalendar_WinterWholeDayActivity_IsSerializedAsDateValueInWinterTimezone()
    {
        // Europe/Amsterdam is UTC+1 in winter.
        var activity = BuildActivity(1,
            new DateTimeOffset(2026, 1, 5, 0, 0, 0, WinterOffset),
            new DateTimeOffset(2026, 1, 5, 23, 59, 0, WinterOffset));

        string ics = CalendarService.BuildCalendar(SingleEnrollment(activity), Language.EN, DateTime.UtcNow);

        Assert.Contains("DTSTART;VALUE=DATE:20260105", ics);
        Assert.Contains("DTEND;VALUE=DATE:20260106", ics);
    }

    [Fact]
    public void BuildCalendar_WaitingListEnrollment_IsPrefixedAndTentative()
    {
        var activity = BuildActivity(1,
            new DateTimeOffset(2026, 6, 1, 18, 30, 0, SummerOffset),
            new DateTimeOffset(2026, 6, 1, 22, 0, 0, SummerOffset));

        string ics = CalendarService.BuildCalendar(SingleEnrollment(activity, isOnWaitingList: true), Language.EN, DateTime.UtcNow);

        Assert.Contains("SUMMARY:[WAITING LIST] Borrel", ics);
        Assert.Contains("STATUS:TENTATIVE", ics);
    }

    [Fact]
    public void BuildCalendar_ConfirmedEnrollment_IsNotTentative()
    {
        var activity = BuildActivity(1,
            new DateTimeOffset(2026, 6, 1, 18, 30, 0, SummerOffset),
            new DateTimeOffset(2026, 6, 1, 22, 0, 0, SummerOffset));

        string ics = CalendarService.BuildCalendar(SingleEnrollment(activity), Language.EN, DateTime.UtcNow);

        Assert.DoesNotContain("STATUS:TENTATIVE", ics);
        Assert.Contains("SUMMARY:Borrel", ics);
    }

    [Fact]
    public void BuildCalendar_DutchMember_UsesDutchLabelsAndDescription()
    {
        var activity = BuildActivity(1,
            new DateTimeOffset(2026, 6, 1, 18, 30, 0, SummerOffset),
            new DateTimeOffset(2026, 6, 1, 22, 0, 0, SummerOffset));

        string ics = CalendarService.BuildCalendar(SingleEnrollment(activity, isOnWaitingList: true), Language.NL, DateTime.UtcNow);

        Assert.Contains("X-WR-CALNAME:Sticky Activiteiten", ics);
        Assert.Contains("[WACHTLIJST]", ics);
        Assert.Contains("Nederlandse omschrijving", Unfold(ics));
    }

    [Fact]
    public void BuildCalendar_EnglishMember_UsesEnglishLabelsAndDescription()
    {
        var activity = BuildActivity(1,
            new DateTimeOffset(2026, 6, 1, 18, 30, 0, SummerOffset),
            new DateTimeOffset(2026, 6, 1, 22, 0, 0, SummerOffset));

        string ics = CalendarService.BuildCalendar(SingleEnrollment(activity), Language.EN, DateTime.UtcNow);

        Assert.Contains("X-WR-CALNAME:Sticky Activities", ics);
        Assert.Contains("English description", Unfold(ics));
    }

    [Fact]
    public void BuildCalendar_Always_ContainsPublishMethodAndLocation()
    {
        var activity = BuildActivity(1,
            new DateTimeOffset(2026, 6, 1, 18, 30, 0, SummerOffset),
            new DateTimeOffset(2026, 6, 1, 22, 0, 0, SummerOffset));

        string ics = CalendarService.BuildCalendar(SingleEnrollment(activity), Language.EN, DateTime.UtcNow);

        Assert.Contains("METHOD:PUBLISH", ics);
        Assert.Contains("LOCATION:Tavern", ics);
        Assert.Contains("PRODID:-//Study association Sticky//Tavern//EN", ics);
    }

    [Fact]
    public void BuildCalendar_ActivityWithoutDescription_OmitsTheDescriptionBlock()
    {
        var activity = BuildActivity(1,
            new DateTimeOffset(2026, 6, 1, 18, 30, 0, SummerOffset),
            new DateTimeOffset(2026, 6, 1, 22, 0, 0, SummerOffset));
        activity.EnglishDescription = "   ";

        string ics = CalendarService.BuildCalendar(SingleEnrollment(activity), Language.EN, DateTime.UtcNow);

        Assert.Contains("https://tavern.svsticky.nl/activities/1", Unfold(ics));
        Assert.Contains("Last synced on", Unfold(ics));
    }

    [Fact]
    public void BuildCalendar_PunctuationInName_IsEscapedPerRfc5545()
    {
        var activity = BuildActivity(1,
            new DateTimeOffset(2026, 6, 1, 18, 30, 0, SummerOffset),
            new DateTimeOffset(2026, 6, 1, 22, 0, 0, SummerOffset),
            "Borrel; met, komma");

        string ics = CalendarService.BuildCalendar(SingleEnrollment(activity), Language.EN, DateTime.UtcNow);

        Assert.Contains(@"SUMMARY:Borrel\; met\, komma", ics);
    }

    [Fact]
    public async Task GetOwnFeedUrl_KnownMember_ReturnsExtensionlessUrl()
    {
        var calendarId = Guid.NewGuid();
        var member = BuildMember(calendarId);
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        var result = await _service.GetOwnFeedUrl(member.Id, CancellationToken.None);

        Assert.Equal($"https://api.tavern.svsticky.nl/calendars/{calendarId}", result.Url);
    }

    [Fact]
    public async Task GetOwnFeedUrl_UnknownMember_Throws()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.GetOwnFeedUrl(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task RotateOwnCalendarId_ReplacesTheIdentifierAndInvalidatesTheOldFeed()
    {
        var oldCalendarId = Guid.NewGuid();
        var member = BuildMember(oldCalendarId);
        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        var result = await _service.RotateOwnCalendarId(member.Id, CancellationToken.None);

        Assert.DoesNotContain(oldCalendarId.ToString(), result.Url);
        Assert.NotEqual(oldCalendarId, member.CalendarId);
        Assert.NotEqual(Guid.Empty, member.CalendarId);

        // The previously published URL must stop resolving.
        Assert.Null(await _service.GetPersonalCalendar(oldCalendarId, CancellationToken.None));
        Assert.NotNull(await _service.GetPersonalCalendar(member.CalendarId, CancellationToken.None));
    }

    [Fact]
    public async Task RotateOwnCalendarId_UnknownMember_Throws()
    {
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _service.RotateOwnCalendarId(Guid.NewGuid(), CancellationToken.None));
    }
}
