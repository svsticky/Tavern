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

/**
 * Determines whether an activity is configured to support enrollments at all.
 * An activity is enrollable if `isEnrollable` is true OR `enrollOpenDate` is defined.
 *
 * @param activity The activity object.
 * @returns `true` if the activity is configured for enrollments, `false` otherwise.
 */
export function isActivityEnrollable(
  activity?: ActivityResponseDto | null,
): boolean {
  if (!activity) return false;
  return Boolean(activity.isEnrollable || activity.enrollOpenDate);
}

/**
 * Determines whether an activity has opened for enrollment (either in the past or currently).
 * Returns `false` only for activities designed to never have enrollment or not yet have enrollment.
 * Once opened, this remains `true` even after enrollment closes after its closing date.
 *
 * @param activity The activity object.
 * @param now The reference date (defaults to current date).
 * @returns `true` if enrollment has opened, `false` otherwise.
 */
export function hasEnrollmentOpened(
  activity?: ActivityResponseDto | null,
  now: Date = new Date(),
): boolean {
  if (!activity) return false;
  const enrollOpenDate = activity.enrollOpenDate
    ? new Date(activity.enrollOpenDate)
    : null;
  const afterEnrollOpenDate = enrollOpenDate ? now >= enrollOpenDate : false;
  return Boolean(activity.isEnrollable || afterEnrollOpenDate);
}
