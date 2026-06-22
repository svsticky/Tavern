import type { ActivityResponseDto } from "~/api";

export interface EnrollmentStatus {
  canEnroll: boolean;
  canUnenroll: boolean;
}

/**
 * Calculates whether the user can enroll or unenroll from an activity.
 *
 * @param activity The activity object.
 * @param now The reference date (defaults to current date).
 */
export function getActivityEnrollmentStatus(
  activity: ActivityResponseDto,
  now: Date = new Date(),
): EnrollmentStatus {
  const unenrollmentDeadline = activity.unenrollmentDeadline
    ? new Date(activity.unenrollmentDeadline)
    : null;
  const enrollOpenDate = activity.enrollOpenDate
    ? new Date(activity.enrollOpenDate)
    : null;
  const enrollmentDeadline = activity.enrollmentDeadline
    ? new Date(activity.enrollmentDeadline)
    : activity.dateTimeEnd
      ? new Date(activity.dateTimeEnd)
      : null;

  const beforeEnrollmentDeadline = enrollmentDeadline
    ? now < enrollmentDeadline
    : now < new Date(activity.dateTimeStart);
  const beforeUnenrollmentDeadline = unenrollmentDeadline
    ? now < unenrollmentDeadline
    : now < new Date(activity.dateTimeStart);
  const afterEnrollmentOpenDate = enrollOpenDate
    ? now >= enrollOpenDate
    : false;

  const canEnroll =
    (activity.isEnrollable || afterEnrollmentOpenDate) &&
    beforeEnrollmentDeadline;
  const canUnenroll =
    beforeUnenrollmentDeadline &&
    beforeEnrollmentDeadline &&
    (afterEnrollmentOpenDate || activity.isEnrollable);

  return { canEnroll, canUnenroll };
}
