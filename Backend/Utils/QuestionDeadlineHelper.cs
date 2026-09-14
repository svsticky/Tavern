using Backend.Models.Domain;

namespace Backend.Utils;

/// <summary>
/// Computes whether a specification question can still be answered or changed, based on the question's
/// own optional deadline, falling back to the activity's enrollment deadline, and finally to the
/// activity's end date and time.
/// </summary>
public static class QuestionDeadlineHelper
{
    /// <summary>
    /// Determines the effective deadline until which a specification question can be answered or changed.
    /// </summary>
    /// <param name="question">The specification question.</param>
    /// <param name="activity">The activity the question belongs to.</param>
    /// <returns>The question's own deadline if set; otherwise the activity's enrollment deadline; otherwise the activity's end date and time.</returns>
    public static DateTimeOffset GetEffectiveDeadline(SpecificationQuestion question, Activity activity)
    {
        return question.AnswerDeadline ?? activity.EnrollmentDeadline ?? activity.DateTimeEnd;
    }

    /// <summary>
    /// Determines whether a specification question can still be answered or changed at the given moment.
    /// </summary>
    /// <param name="question">The specification question.</param>
    /// <param name="activity">The activity the question belongs to.</param>
    /// <param name="now">The moment to check against.</param>
    public static bool IsAnswerable(SpecificationQuestion question, Activity activity, DateTimeOffset now)
    {
        return now <= GetEffectiveDeadline(question, activity);
    }
}
