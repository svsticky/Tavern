using Backend.Utils.DateTime;

namespace Backend.Tests.Utils;

public class FinancialYearUtilsTests
{
    [Fact]
    public void GetCurrentFinancialYear_BeforeAugust_ReturnsCurrentYear()
    {
        // July 31, 2026 12:00:00 UTC
        var utcNow = new DateTime(2026, 7, 31, 12, 0, 0, DateTimeKind.Utc);
        
        var financialYear = YearUtils.GetCurrentFinancialYear(utcNow);
        
        Assert.Equal(2026u, financialYear);
    }

    [Fact]
    public void GetCurrentFinancialYear_AugustOrLater_ReturnsNextYear()
    {
        // August 1, 2026 12:00:00 UTC
        var utcNow = new DateTime(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc);
        
        var financialYear = YearUtils.GetCurrentFinancialYear(utcNow);
        
        Assert.Equal(2027u, financialYear);
    }

    [Fact]
    public void GetCurrentFinancialYear_Parameterless_ReturnsValidYear()
    {
        var financialYear = YearUtils.GetCurrentFinancialYear();
        
        // Assert it returned something sane (e.g. current year or next year)
        var currentYear = (uint)DateTime.UtcNow.Year;
        Assert.True(financialYear == currentYear || financialYear == currentYear + 1);
    }

    [Fact]
    public void GetCurrentFinancialYear_CustomSettingStartDate_ReturnsCorrectYear()
    {
        // Set setting to "10-15" (October 15th)
        YearUtils.FinancialYearStartDate = "10-15";

        try
        {
            // October 14, 2026 -> should return 2026
            var beforeOct15 = new DateTime(2026, 10, 14, 12, 0, 0, DateTimeKind.Utc);
            var yearBefore = YearUtils.GetCurrentFinancialYear(beforeOct15);
            Assert.Equal(2026u, yearBefore);

            // October 15, 2026 -> should return 2027
            var onOct15 = new DateTime(2026, 10, 15, 12, 0, 0, DateTimeKind.Utc);
            var yearOn = YearUtils.GetCurrentFinancialYear(onOct15);
            Assert.Equal(2027u, yearOn);
        }
        finally
        {
            // Reset to default
            YearUtils.FinancialYearStartDate = "08-01";
        }
    }

    [Fact]
    public void GetCurrentBoardYear_CustomSettingStartDate_ReturnsCorrectYear()
    {
        // Set setting to "09-15" (September 15th)
        YearUtils.BoardChangeDate = "09-15";

        try
        {
            // September 14, 2026 -> should return 2026
            var beforeSept15 = new DateTime(2026, 9, 14, 12, 0, 0, DateTimeKind.Utc);
            var yearBefore = YearUtils.GetCurrentBoardYear(beforeSept15);
            Assert.Equal(2026u, yearBefore);

            // September 15, 2026 -> should return 2027
            var onSept15 = new DateTime(2026, 9, 15, 12, 0, 0, DateTimeKind.Utc);
            var yearOn = YearUtils.GetCurrentBoardYear(onSept15);
            Assert.Equal(2027u, yearOn);
        }
        finally
        {
            // Reset to default
            YearUtils.BoardChangeDate = "08-01";
        }
    }
}
