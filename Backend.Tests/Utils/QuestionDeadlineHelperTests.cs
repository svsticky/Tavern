using Backend.Models.Domain;
using Backend.Utils;

namespace Backend.Tests.Utils;

public class QuestionDeadlineHelperTests
{
    private static Activity CreateActivity(DateTimeOffset end, DateTimeOffset? enrollmentDeadline = null)
    {
        return new Activity
        {
            Name = "Activity",
            DutchDescription = "Beschrijving",
            EnglishDescription = "Description",
            Location = "Enschede",
            DateTimeEnd = end,
            EnrollmentDeadline = enrollmentDeadline,
            PaymentDeadline = DateTimeOffset.UtcNow.AddDays(20)
        };
    }

    private static SpecificationQuestion CreateQuestion(DateTimeOffset? answerDeadline = null)
    {
        return new SpecificationQuestion
        {
            QuestionDutch = "Vraag",
            QuestionEnglish = "Question",
            Type = QuestionType.String,
            AnswerDeadline = answerDeadline
        };
    }

    [Fact]
    public void GetEffectiveDeadline_NoQuestionOrEnrollmentDeadline_FallsBackToActivityEnd()
    {
        var end = DateTimeOffset.UtcNow.AddDays(5);
        var activity = CreateActivity(end);
        var question = CreateQuestion();

        Assert.Equal(end, QuestionDeadlineHelper.GetEffectiveDeadline(question, activity));
    }

    [Fact]
    public void GetEffectiveDeadline_NoQuestionDeadline_FallsBackToEnrollmentDeadline()
    {
        var enrollmentDeadline = DateTimeOffset.UtcNow.AddDays(3);
        var activity = CreateActivity(DateTimeOffset.UtcNow.AddDays(5), enrollmentDeadline);
        var question = CreateQuestion();

        Assert.Equal(enrollmentDeadline, QuestionDeadlineHelper.GetEffectiveDeadline(question, activity));
    }

    [Fact]
    public void GetEffectiveDeadline_QuestionDeadlineSet_OverridesEnrollmentDeadlineAndActivityEnd()
    {
        var questionDeadline = DateTimeOffset.UtcNow.AddDays(1);
        var activity = CreateActivity(DateTimeOffset.UtcNow.AddDays(5), DateTimeOffset.UtcNow.AddDays(3));
        var question = CreateQuestion(questionDeadline);

        Assert.Equal(questionDeadline, QuestionDeadlineHelper.GetEffectiveDeadline(question, activity));
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
