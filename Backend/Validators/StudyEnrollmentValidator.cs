using Backend.Controllers.DTOs;

namespace Backend.Validators;

/// <summary>
/// Provides validation methods for study enrollments.
/// </summary>
public static class StudyEnrollmentValidator
{
    /// <summary>
    /// Validates that all study enrollment dates match one of the valid start dates configured in settings.
    /// </summary>
    /// <param name="studyEnrollments">The study enrollments to validate.</param>
    /// <param name="studyStartDatesSetting">The raw comma-separated study start dates setting string (e.g. "09-01,02-01").</param>
    /// <exception cref="ArgumentException">Thrown when an enrollment date is not a valid start date.</exception>
    public static void ValidateEnrollmentDates(IEnumerable<PostStudyEnrollmentDTO> studyEnrollments, string studyStartDatesSetting)
    {
        var configuredStartDates = studyStartDatesSetting.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var today = DateTime.UtcNow.Date;

        foreach (var se in studyEnrollments)
        {
            if (!IsValidStudyEnrollmentDate(se.EnrollmentDate.Date, configuredStartDates, today))
            {
                throw new ArgumentException($"Enrollment date {se.EnrollmentDate:yyyy-MM-dd} is not a valid study start date.");
            }
        }
    }

    /// <summary>
    /// Determines whether a specific date is a valid study start date given configured MM-DD strings and current date.
    /// </summary>
    public static bool IsValidStudyEnrollmentDate(DateTime date, string[] configuredStartDates, DateTime today)
    {
        foreach (var startDateStr in configuredStartDates)
        {
            var parts = startDateStr.Split('-');
            if (parts.Length != 2 || !int.TryParse(parts[0], out int month) || !int.TryParse(parts[1], out int day))
                continue;

            if (date.Month == month && date.Day == day)
            {
                if (date <= today)
                {
                    return true;
                }

                var nextFutureDate = configuredStartDates
                    .Select(s => {
                        var p = s.Split('-');
                        if (p.Length == 2 && int.TryParse(p[0], out int m) && int.TryParse(p[1], out int d))
                        {
                            var nextD = new DateTime(today.Year, m, d);
                            if (nextD <= today) nextD = nextD.AddYears(1);
                            return nextD;
                        }
                        return (DateTime?)null;
                    })
                    .Where(d => d.HasValue)
                    .Select(d => d!.Value)
                    .OrderBy(d => d)
                    .FirstOrDefault();

                if (nextFutureDate != default && date == nextFutureDate)
                {
                    return true;
                }
            }
        }

        return false;
    }
}
