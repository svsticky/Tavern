using Backend.Models.Domain;
using Backend.Utils;

namespace Backend.Tests.Utils;

public class AnswerDeadlineHelperTests
{
    private static Activity CreateActivity(
        DateTimeOffset end,
        DateTimeOffset? enrollmentDeadline = null,
        DateTimeOffset? unenrollmentDeadline = null,
        bool closeOnUnenrollmentDeadline = false)
    {
        return new Activity
        {
            Name = "Activity",
            DutchDescription = "Beschrijving",
            EnglishDescription = "Description",
            Location = "Enschede",
            DateTimeEnd = end,
            EnrollmentDeadline = enrollmentDeadline,
            UnenrollmentDeadline = unenrollmentDeadline,
            CloseAnswersOnUnenrollmentDeadline = closeOnUnenrollmentDeadline,
            PaymentDeadline = DateTimeOffset.UtcNow.AddDays(20)
        };
    }

    [Fact]
    public void GetEffectiveDeadline_NoDeadlinesAtAll_FallsBackToActivityEnd()
    {
        var end = DateTimeOffset.UtcNow.AddDays(5);
        var activity = CreateActivity(end);

        Assert.Equal(end, AnswerDeadlineHelper.GetEffectiveDeadline(activity));
    }

    [Fact]
    public void GetEffectiveDeadline_NoCloseFlag_UsesEnrollmentDeadlineEvenWithUnenrollmentDeadlineSet()
    {
        var enrollmentDeadline = DateTimeOffset.UtcNow.AddDays(3);
        var unenrollmentDeadline = DateTimeOffset.UtcNow.AddDays(1);
        var activity = CreateActivity(
            DateTimeOffset.UtcNow.AddDays(5),
            enrollmentDeadline,
            unenrollmentDeadline,
            closeOnUnenrollmentDeadline: false);

        Assert.Equal(enrollmentDeadline, AnswerDeadlineHelper.GetEffectiveDeadline(activity));
    }

    [Fact]
    public void GetEffectiveDeadline_CloseFlagSetWithUnenrollmentDeadline_UsesUnenrollmentDeadline()
    {
        var enrollmentDeadline = DateTimeOffset.UtcNow.AddDays(3);
        var unenrollmentDeadline = DateTimeOffset.UtcNow.AddDays(1);
        var activity = CreateActivity(
            DateTimeOffset.UtcNow.AddDays(5),
            enrollmentDeadline,
            unenrollmentDeadline,
            closeOnUnenrollmentDeadline: true);

        Assert.Equal(unenrollmentDeadline, AnswerDeadlineHelper.GetEffectiveDeadline(activity));
    }

    [Fact]
    public void GetEffectiveDeadline_CloseFlagSetButNoUnenrollmentDeadline_FallsBackToEnrollmentDeadline()
    {
        var enrollmentDeadline = DateTimeOffset.UtcNow.AddDays(3);
        var activity = CreateActivity(
            DateTimeOffset.UtcNow.AddDays(5),
            enrollmentDeadline,
            unenrollmentDeadline: null,
            closeOnUnenrollmentDeadline: true);

        Assert.Equal(enrollmentDeadline, AnswerDeadlineHelper.GetEffectiveDeadline(activity));
    }

    [Fact]
    public void GetEffectiveDeadline_CloseFlagSetButNoUnenrollmentOrEnrollmentDeadline_FallsBackToActivityEnd()
    {
        var end = DateTimeOffset.UtcNow.AddDays(5);
        var activity = CreateActivity(end, closeOnUnenrollmentDeadline: true);

        Assert.Equal(end, AnswerDeadlineHelper.GetEffectiveDeadline(activity));
    }

    [Fact]
    public void AreAnswersOpen_BeforeEffectiveDeadline_ReturnsTrue()
    {
        var activity = CreateActivity(DateTimeOffset.UtcNow.AddDays(5));

        Assert.True(AnswerDeadlineHelper.AreAnswersOpen(activity, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void AreAnswersOpen_AfterEffectiveDeadline_ReturnsFalse()
    {
        var activity = CreateActivity(DateTimeOffset.UtcNow.AddDays(-1));

        Assert.False(AnswerDeadlineHelper.AreAnswersOpen(activity, DateTimeOffset.UtcNow));
    }
}
