using Backend.Utils.DateTime;

namespace Backend.Tests.Utils;

public class FinancialYearUtilsTests
{
    [Fact]
    public void GetCurrentFinancialYear_BeforeAugust_ReturnsCurrentYear()
    {
        // July 31, 2026 12:00:00 UTC
        var utcNow = new DateTime(2026, 7, 31, 12, 0, 0, DateTimeKind.Utc);
        
        var financialYear = FinancialYearUtils.GetCurrentFinancialYear(utcNow);
        
        Assert.Equal(2026u, financialYear);
    }

    [Fact]
    public void GetCurrentFinancialYear_AugustOrLater_ReturnsNextYear()
    {
        // August 1, 2026 12:00:00 UTC
        var utcNow = new DateTime(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc);
        
        var financialYear = FinancialYearUtils.GetCurrentFinancialYear(utcNow);
        
        Assert.Equal(2027u, financialYear);
    }

    [Fact]
    public void GetCurrentFinancialYear_Parameterless_ReturnsValidYear()
    {
        var financialYear = FinancialYearUtils.GetCurrentFinancialYear();
        
        // Assert it returned something sane (e.g. current year or next year)
        var currentYear = (uint)DateTime.UtcNow.Year;
        Assert.True(financialYear == currentYear || financialYear == currentYear + 1);
    }
}
