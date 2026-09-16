using Backend.Models.Domain;
using Backend.Utils;

namespace Backend.Tests.Utils;

public class QuestionDeadlineHelperTests
{
    private static Activity CreateActivity(
        DateTimeOffset end,
        DateTimeOffset? enrollmentDeadline = null,
        DateTimeOffset? unenrollmentDeadline = null)
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
            PaymentDeadline = DateTimeOffset.UtcNow.AddDays(20)
        };
    }

    private static SpecificationQuestion CreateQuestion(bool closeOnUnenrollmentDeadline = false)
    {
        return new SpecificationQuestion
        {
            QuestionDutch = "Vraag",
            QuestionEnglish = "Question",
            Type = QuestionType.String,
            CloseOnUnenrollmentDeadline = closeOnUnenrollmentDeadline
        };
    }

    [Fact]
    public void GetEffectiveDeadline_NoCloseFlag_FallsBackToEnrollmentDeadlineEvenWithUnenrollmentDeadlineSet()
    {
        var enrollmentDeadline = DateTimeOffset.UtcNow.AddDays(3);
        var unenrollmentDeadline = DateTimeOffset.UtcNow.AddDays(1);
        var activity = CreateActivity(DateTimeOffset.UtcNow.AddDays(5), enrollmentDeadline, unenrollmentDeadline);
        var question = CreateQuestion(closeOnUnenrollmentDeadline: false);

        Assert.Equal(enrollmentDeadline, QuestionDeadlineHelper.GetEffectiveDeadline(question, activity));
    }

    [Fact]
    public void GetEffectiveDeadline_CloseFlagSetWithUnenrollmentDeadline_UsesUnenrollmentDeadline()
    {
        var enrollmentDeadline = DateTimeOffset.UtcNow.AddDays(3);
        var unenrollmentDeadline = DateTimeOffset.UtcNow.AddDays(1);
        var activity = CreateActivity(DateTimeOffset.UtcNow.AddDays(5), enrollmentDeadline, unenrollmentDeadline);
        var question = CreateQuestion(closeOnUnenrollmentDeadline: true);

        Assert.Equal(unenrollmentDeadline, QuestionDeadlineHelper.GetEffectiveDeadline(question, activity));
    }

    [Fact]
    public void GetEffectiveDeadline_CloseFlagSetButNoUnenrollmentDeadline_FallsBackToEnrollmentDeadline()
    {
        var enrollmentDeadline = DateTimeOffset.UtcNow.AddDays(3);
        var activity = CreateActivity(DateTimeOffset.UtcNow.AddDays(5), enrollmentDeadline, unenrollmentDeadline: null);
        var question = CreateQuestion(closeOnUnenrollmentDeadline: true);

        Assert.Equal(enrollmentDeadline, QuestionDeadlineHelper.GetEffectiveDeadline(question, activity));
    }

    [Fact]
    public void GetEffectiveDeadline_NoDeadlinesAtAll_FallsBackToActivityEnd()
    {
        var end = DateTimeOffset.UtcNow.AddDays(5);
        var activity = CreateActivity(end);
        var question = CreateQuestion();

        Assert.Equal(end, QuestionDeadlineHelper.GetEffectiveDeadline(question, activity));
    }

    [Fact]
    public void IsAnswerable_BeforeEffectiveDeadline_ReturnsTrue()
    {
        var activity = CreateActivity(DateTimeOffset.UtcNow.AddDays(5));
        var question = CreateQuestion();

        Assert.True(QuestionDeadlineHelper.IsAnswerable(question, activity, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void IsAnswerable_AfterEffectiveDeadline_ReturnsFalse()
    {
        var activity = CreateActivity(DateTimeOffset.UtcNow.AddDays(-1));
        var question = CreateQuestion();

        Assert.False(QuestionDeadlineHelper.IsAnswerable(question, activity, DateTimeOffset.UtcNow));
    }
}
