using System.Runtime.InteropServices;

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
        string timezoneId = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) 
            ? "W. Europe Standard Time" 
            : "Europe/Amsterdam";
        
        TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
        System.DateTime nowInNetherlands = TimeZoneInfo.ConvertTimeFromUtc(utcNow, tz);

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
        if (nowInNetherlands.Month > targetMonth)
        {
            isAfterOrEqual = true;
        }
        else if (nowInNetherlands.Month == targetMonth)
        {
            isAfterOrEqual = nowInNetherlands.Day >= targetDay;
        }
        else
        {
            isAfterOrEqual = false;
        }

        return targetMonth <= 6 ? isAfterOrEqual 
            ? (uint)nowInNetherlands.Year 
            : (uint)nowInNetherlands.Year - 1
            : isAfterOrEqual 
            ? (uint)nowInNetherlands.Year + 1 
            : (uint)nowInNetherlands.Year;
    }

    /// <summary>
    /// Gets or sets the start date for the board change. Format is "MM-DD".
    /// </summary>
    public static string BoardChangeDate { get; set; } = "08-01";

    /// <summary>
    /// Determines the current board year based on the current date and time in the Netherlands.
    /// </summary>
    /// <returns>The current board year.</returns>
    public static uint GetCurrentBoardYear()
    {
        return GetCurrentBoardYear(System.DateTime.UtcNow);
    }

    /// <summary>
    /// Determines the board year based on a provided UTC date and time in the Netherlands.
    /// </summary>
    /// <param name="utcNow">The UTC date and time to calculate the board year for.</param>
    /// <returns>The calculated board year.</returns>
    public static uint GetCurrentBoardYear(System.DateTime utcNow)
    {
        string timezoneId = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) 
            ? "W. Europe Standard Time" 
            : "Europe/Amsterdam";
        
        TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
        System.DateTime nowInNetherlands = TimeZoneInfo.ConvertTimeFromUtc(utcNow, tz);

        int targetMonth = 8;
        int targetDay = 1;
        if (!string.IsNullOrEmpty(BoardChangeDate))
        {
            var parts = BoardChangeDate.Split('-');
            if (parts.Length == 2 && 
                int.TryParse(parts[0], out int m) && 
                int.TryParse(parts[1], out int d))
            {
                targetMonth = m;
                targetDay = d;
            }
        }

        bool isAfterOrEqual;
        if (nowInNetherlands.Month > targetMonth)
        {
            isAfterOrEqual = true;
        }
        else if (nowInNetherlands.Month == targetMonth)
        {
            isAfterOrEqual = nowInNetherlands.Day >= targetDay;
        }
        else
        {
            isAfterOrEqual = false;
        }

        return isAfterOrEqual 
            ? (uint)nowInNetherlands.Year + 1 
            : (uint)nowInNetherlands.Year;
    }
}