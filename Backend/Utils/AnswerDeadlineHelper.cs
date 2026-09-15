using Backend.Models.Domain;

namespace Backend.Utils;

/// <summary>
/// Computes whether an activity's specification answers can still be given or changed. The
/// effective deadline is the activity's unenrollment deadline when
/// <see cref="Activity.CloseAnswersOnUnenrollmentDeadline"/> is set and an unenrollment deadline
/// exists; otherwise the enrollment deadline; otherwise the activity's end date and time.
/// </summary>
public static class AnswerDeadlineHelper
{
    /// <summary>
    /// Determines the effective deadline until which an activity's specification answers can be
    /// given or changed.
    /// </summary>
    /// <param name="activity">The activity.</param>
    public static DateTimeOffset GetEffectiveDeadline(Activity activity)
    {
        if (activity.CloseAnswersOnUnenrollmentDeadline && activity.UnenrollmentDeadline != null)
            return activity.UnenrollmentDeadline.Value;

        return activity.EnrollmentDeadline ?? activity.DateTimeEnd;
    }

    /// <summary>
    /// Determines whether an activity's specification answers can still be given or changed at the given moment.
    /// </summary>
    /// <param name="activity">The activity.</param>
    /// <param name="now">The moment to check against.</param>
    public static bool AreAnswersOpen(Activity activity, DateTimeOffset now)
    {
        return now <= GetEffectiveDeadline(activity);
    }
}
