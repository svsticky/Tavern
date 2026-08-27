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
    public void GetYearForDate_CustomSettingStartDate_ReturnsCorrectYear()
    {
        // Set setting to "09-15" (September 15th)
        YearUtils.CommitteeCreationDate = "09-15";

        try
        {
            // September 14, 2026 -> should return 2026
            var beforeSept15 = new DateTime(2026, 9, 14, 12, 0, 0, DateTimeKind.Utc);
            var yearBefore = YearUtils.GetYearForDate(beforeSept15, "09-15");
            Assert.Equal(2026u, yearBefore);

            // September 15, 2026 -> should return 2027
            var onSept15 = new DateTime(2026, 9, 15, 12, 0, 0, DateTimeKind.Utc);
            var yearOn = YearUtils.GetYearForDate(onSept15, "09-15");
            Assert.Equal(2027u, yearOn);
        }
        finally
        {
            // Reset to default
            YearUtils.CommitteeCreationDate = "08-01";
        }
    }

    [Fact]
    public void GetCurrentFinancialYear_TargetMonthLessThanOrEqualTo6_ReturnsCorrectYear()
    {
        YearUtils.FinancialYearStartDate = "03-01";
        try
        {
            var beforeMarch1 = new DateTime(2026, 2, 28, 12, 0, 0, DateTimeKind.Utc);
            Assert.Equal(2025u, YearUtils.GetCurrentFinancialYear(beforeMarch1));

            var onMarch1 = new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);
            Assert.Equal(2026u, YearUtils.GetCurrentFinancialYear(onMarch1));
        }
        finally
        {
            YearUtils.FinancialYearStartDate = "08-01";
        }
    }

    [Fact]
    public void GetCurrentFinancialYear_InvalidOrNullStartDate_FallsBackToAugust1()
    {
        YearUtils.FinancialYearStartDate = "invalid-date";
        try
        {
            var July31 = new DateTime(2026, 7, 31, 12, 0, 0, DateTimeKind.Utc);
            Assert.Equal(2026u, YearUtils.GetCurrentFinancialYear(July31));

            var Aug1 = new DateTime(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc);
            Assert.Equal(2027u, YearUtils.GetCurrentFinancialYear(Aug1));
        }
        finally
        {
            YearUtils.FinancialYearStartDate = "08-01";
        }
    }

    [Fact]
    public void GetCurrentFinancialYear_MonthAfterTargetMonth_ReturnsCurrentYear()
    {
        YearUtils.FinancialYearStartDate = "03-01";
        try
        {
            // April is strictly after the March target month (not just on the boundary day)
            var april = new DateTime(2026, 4, 15, 12, 0, 0, DateTimeKind.Utc);
            Assert.Equal(2026u, YearUtils.GetCurrentFinancialYear(april));
        }
        finally
        {
            YearUtils.FinancialYearStartDate = "08-01";
        }
    }

    [Fact]
    public void GetYearForDate_MonthAfterTargetMonth_ReturnsNextYear()
    {
        // October is strictly after the August target month (not just on the boundary day)
        var october = new DateTime(2026, 10, 15, 12, 0, 0, DateTimeKind.Utc);
        Assert.Equal(2027u, YearUtils.GetYearForDate(october, "08-01"));
    }

    [Fact]
    public void GetYearForDate_MonthBeforeTargetMonth_ReturnsCurrentYear()
    {
        // May is strictly before the August target month
        var may = new DateTime(2026, 5, 15, 12, 0, 0, DateTimeKind.Utc);
        Assert.Equal(2026u, YearUtils.GetYearForDate(may, "08-01"));
    }

    [Fact]
    public void GetCommitteeYear_ReturnsValidYear()
    {
        var committeeYear = YearUtils.GetCommitteeYear();
        var currentYear = (uint)DateTime.UtcNow.Year;
        Assert.True(committeeYear == currentYear || committeeYear == currentYear + 1);
    }
}

