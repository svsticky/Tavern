using System.Runtime.InteropServices;

namespace Backend.Utils;

public static class YearUtils
{
    public static uint GetCurrentFinancialYear()
    {
        string timezoneId = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) 
            ? "W. Europe Standard Time" 
            : "Europe/Amsterdam";
        
        TimeZoneInfo tz = TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
        DateTime nowInNetherlands = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);

        return nowInNetherlands.Month >= 8 
            ? (uint)nowInNetherlands.Year + 1 
            : (uint)nowInNetherlands.Year;
    }
}