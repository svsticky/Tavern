using System.Runtime.InteropServices;

namespace Backend.Utils.DateTime;

public static class FinancialYearUtils
{
    public static uint GetCurrentFinancialYear()
    {
        string timezoneId = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) 
            ? "W. Europe Standard Time" 
            : "Europe/Amsterdam";
        
        TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
        System.DateTime nowInNetherlands = TimeZoneInfo.ConvertTimeFromUtc(System.DateTime.UtcNow, tz);

        return nowInNetherlands.Month >= 8 
            ? (uint)nowInNetherlands.Year + 1 
            : (uint)nowInNetherlands.Year;
    }
}