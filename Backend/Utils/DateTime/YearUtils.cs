using Backend.Database;
using System.Globalization;

namespace Backend.Utils.DateTime;

/// <summary>
/// The FinancialYearUtils class provides a utility method for determining the current financial year based on the current date and time in the Netherlands. The GetCurrentFinancialYear method calculates the financial year by checking if the current month is August or later, in which case it returns the next calendar year as the financial year; otherwise, it returns the current calendar year. This utility is useful for scenarios where financial data needs to be categorized or processed based on the financial year, which may differ from the calendar year depending on the organization's fiscal calendar.
/// </summary>
public static class YearUtils
{
    /// <summary>
    /// Gets or sets the start date of the financial year. Format is "MM-DD".
    /// </summary>
    public static string FinancialYearStartDate { get; set; } = "08-01";

    /// <summary>
    /// Determines the current financial year based on the current date and time in the Netherlands. The financial year is calculated by checking if the current month is August or later, in which case it returns the next calendar year as the financial year; otherwise, it returns the current calendar year.
    /// </summary>
    /// <returns>The current financial year.</returns>
    public static uint GetCurrentFinancialYear()
    {
        return GetCurrentFinancialYear(System.DateTime.UtcNow);
    }

    /// <summary>
    /// Determines the financial year based on a provided UTC date and time in the Netherlands.
    /// </summary>
    /// <param name="utcNow">The UTC date and time to calculate the financial year for.</param>
    /// <returns>The calculated financial year.</returns>
    public static uint GetCurrentFinancialYear(System.DateTime utcNow)
    {
        string timezoneId = Environment.GetEnvironmentVariable("AssociationTimeZone") ?? "Europe/Amsterdam";

        TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
        System.DateTime nowInTimeZone = TimeZoneInfo.ConvertTimeFromUtc(utcNow, tz);

        int targetMonth = 8;
        int targetDay = 1;
        if (!string.IsNullOrEmpty(FinancialYearStartDate))
        {
            var parts = FinancialYearStartDate.Split('-');
            if (parts.Length == 2 &&
                int.TryParse(parts[0], out int m) &&
                int.TryParse(parts[1], out int d))
            {
                targetMonth = m;
                targetDay = d;
            }
        }

        bool isAfterOrEqual;
        if (nowInTimeZone.Month > targetMonth)
        {
            isAfterOrEqual = true;
        }
        else if (nowInTimeZone.Month == targetMonth)
        {
            isAfterOrEqual = nowInTimeZone.Day >= targetDay;
        }
        else
        {
            isAfterOrEqual = false;
        }

        return targetMonth <= 6 ? isAfterOrEqual
            ? (uint)nowInTimeZone.Year
            : (uint)nowInTimeZone.Year - 1
            : isAfterOrEqual
            ? (uint)nowInTimeZone.Year + 1
            : (uint)nowInTimeZone.Year;
    }

    /// <summary>
    /// Gets or sets the start date for committee creation. Format is "MM-DD".
    /// </summary>
    public static string CommitteeCreationDate { get; set; } = "08-01";

    /// <summary>
    /// Calculates the associated operational year for a given date based on a specified start date ("MM-DD").
    /// </summary>
    public static uint GetYearForDate(System.DateTime utcNow, string startDateStr)
    {
        string timezoneId = Environment.GetEnvironmentVariable("AssociationTimeZone") ?? "Europe/Amsterdam";

        TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
        System.DateTime nowInTimeZone = TimeZoneInfo.ConvertTimeFromUtc(utcNow, tz);

        int targetMonth = 8;
        int targetDay = 1;
        if (!string.IsNullOrEmpty(startDateStr))
        {
            var parts = startDateStr.Split('-');
            if (parts.Length == 2 &&
                int.TryParse(parts[0], out int m) &&
                int.TryParse(parts[1], out int d))
            {
                targetMonth = m;
                targetDay = d;
            }
        }

        bool isAfterOrEqual;
        if (nowInTimeZone.Month > targetMonth)
        {
            isAfterOrEqual = true;
        }
        else if (nowInTimeZone.Month == targetMonth)
        {
            isAfterOrEqual = nowInTimeZone.Day >= targetDay;
        }
        else
        {
            isAfterOrEqual = false;
        }

        return targetMonth <= 6 ? isAfterOrEqual
            ? (uint)nowInTimeZone.Year
            : (uint)nowInTimeZone.Year - 1
            : isAfterOrEqual
            ? (uint)nowInTimeZone.Year + 1
            : (uint)nowInTimeZone.Year;
    }

    /// <summary>
    /// Gets the current board year as the maximum membership year in the board group.
    /// </summary>
    public static uint GetBoardYear(PostgresDbContext db)
    {
        uint boardGroupId = uint.Parse(db.Settings.FirstOrDefault(s => s.Name == "BoardGroupId")?.Value ?? "1", CultureInfo.InvariantCulture);
        return db.GroupMemberships
            .Where(gm => gm.GroupId == boardGroupId)
            .Max(gm => (uint?)gm.MembershipYear) ?? GetYearForDate(System.DateTime.UtcNow, CommitteeCreationDate);
    }

    /// <summary>
    /// Gets the current committee year as the maximum membership year in the committee group.
    /// </summary>
    public static uint GetCommitteeYear()
    {
        return GetYearForDate(System.DateTime.UtcNow, CommitteeCreationDate);
    }
}
