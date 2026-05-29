using System.Runtime.InteropServices;

namespace Backend.Utils.DateTime;

/// <summary>
/// The FinancialYearUtils class provides a utility method for determining the current financial year based on the current date and time in the Netherlands. The GetCurrentFinancialYear method calculates the financial year by checking if the current month is August or later, in which case it returns the next calendar year as the financial year; otherwise, it returns the current calendar year. This utility is useful for scenarios where financial data needs to be categorized or processed based on the financial year, which may differ from the calendar year depending on the organization's fiscal calendar.
/// </summary>
public static class FinancialYearUtils
{
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

        return nowInNetherlands.Month >= 8 
            ? (uint)nowInNetherlands.Year + 1 
            : (uint)nowInNetherlands.Year;
    }
}