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
 * Determines whether the user should be shown Dutch or English activity texts.
 * Prioritizes the active member profile language, then fallback token claim or i18n language.
 */
export function isDutchLocale(
  preferredLanguage?: string | null,
  i18nLanguage?: string | null,
  tokenLocale?: string | null,
): boolean {
  if (preferredLanguage) {
    return preferredLanguage.toUpperCase() === "NL";
  }
  if (tokenLocale) {
    return tokenLocale.toUpperCase() === "NL";
  }
  if (i18nLanguage) {
    return i18nLanguage.toLowerCase().startsWith("nl");
  }
  return true;
}
