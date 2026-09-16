using Backend.Models.Domain;

namespace Backend.Utils;

/// <summary>
/// Computes whether a specification question's answer can still be given or changed. Each question can
/// opt into closing at the activity's unenrollment deadline instead of its enrollment deadline via
/// <see cref="SpecificationQuestion.CloseOnUnenrollmentDeadline"/>.
/// </summary>
public static class QuestionDeadlineHelper
{
    /// <summary>
    /// Determines the effective deadline until which a specification question can be answered or changed.
    /// </summary>
    /// <param name="question">The specification question.</param>
    /// <param name="activity">The activity the question belongs to.</param>
    /// <returns>
    /// The activity's unenrollment deadline when <see cref="SpecificationQuestion.CloseOnUnenrollmentDeadline"/>
    /// is set and an unenrollment deadline exists; otherwise the activity's enrollment deadline; otherwise the
    /// activity's end date and time.
    /// </returns>
    public static DateTimeOffset GetEffectiveDeadline(SpecificationQuestion question, Activity activity)
    {
        if (question.CloseOnUnenrollmentDeadline && activity.UnenrollmentDeadline != null)
            return activity.UnenrollmentDeadline.Value;

        return activity.EnrollmentDeadline ?? activity.DateTimeEnd;
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
