using Backend.Controllers.DTOs;
using Backend.Validators;

namespace Backend.Tests.Validators;

public class StudyEnrollmentValidatorTests
{
    private readonly string[] _configuredStartDates = new[] { "09-01", "02-01" };
    private readonly DateTime _today = new DateTime(2026, 7, 24);

    [Theory]
    [InlineData(2026, 2, 1, true)]    // Past date in current year (Feb 1, 2026 <= July 24, 2026)
    [InlineData(2025, 9, 1, true)]    // Past date in previous year
    [InlineData(2026, 9, 1, true)]    // Next future start date (Sep 1, 2026)
    [InlineData(2027, 2, 1, false)]   // Future start date beyond next immediate start date
    [InlineData(2026, 7, 1, false)]   // Arbitrary date not matching month/day
    public void IsValidStudyEnrollmentDate_ValidatesCorrectly(int year, int month, int day, bool expected)
    {
        var testDate = new DateTime(year, month, day);
        var result = StudyEnrollmentValidator.IsValidStudyEnrollmentDate(testDate, _configuredStartDates, _today);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ValidateEnrollmentDates_InvalidDate_ThrowsArgumentException()
    {
        var enrollments = new[]
        {
            new PostStudyEnrollmentDTO
            {
                StudyId = 1,
                MemberId = Guid.NewGuid(),
                EnrollmentDate = new DateTime(2026, 7, 1)
            }
        };

        Assert.Throws<ArgumentException>(() =>
            StudyEnrollmentValidator.ValidateEnrollmentDates(enrollments, "09-01,02-01"));
    }

    [Fact]
    public void ValidateEnrollmentDates_ValidDate_DoesNotThrow()
    {
        var enrollments = new[]
        {
            new PostStudyEnrollmentDTO
            {
                StudyId = 1,
                MemberId = Guid.NewGuid(),
                EnrollmentDate = new DateTime(2026, 2, 1)
            }
        };

        StudyEnrollmentValidator.ValidateEnrollmentDates(enrollments, "09-01,02-01");
    }
}
