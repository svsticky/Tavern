import type {
  ActivityResponseDto,
  GetSpecificationQuestionResponseDto,
} from "~/api";

/**
 * Determines the effective deadline until which an activity's specification answers can be given
 * or changed: the unenrollment deadline when `closeAnswersOnUnenrollmentDeadline` is set and an
 * unenrollment deadline exists, otherwise the enrollment deadline, otherwise the activity's end
 * date and time. Mirrors the backend's `AnswerDeadlineHelper`.
 *
 * @param activity - The activity.
 * @returns The effective deadline, or null if none of the three dates is available.
 */
export function getAnswerDeadline(activity: ActivityResponseDto): Date | null {
  const deadline =
    activity.closeAnswersOnUnenrollmentDeadline && activity.unenrollmentDeadline
      ? activity.unenrollmentDeadline
      : (activity.enrollmentDeadline ?? activity.dateTimeEnd);
  return deadline ? new Date(deadline) : null;
}

/**
 * Determines whether an activity's specification answers can still be given or changed.
 *
 * @param activity - The activity.
 * @param now - The reference date (defaults to current date).
 */
export function areAnswersOpen(
  activity: ActivityResponseDto,
  now: Date = new Date(),
): boolean {
  const deadline = getAnswerDeadline(activity);
  return !deadline || now <= deadline;
}

/**
 * Checks whether every mandatory specification question has a non-empty answer. Once the
 * activity's answer deadline has passed, mandatory questions are no longer required - they can no
 * longer be filled in anyway.
 *
 * @param questions - The specification questions to validate against.
 * @param activity - The activity the questions belong to.
 * @param answers - Current answers keyed by question id.
 * @returns True if all mandatory questions (while answers are still open) have been answered.
 */
export function hasAllMandatoryAnswers(
  questions: GetSpecificationQuestionResponseDto[],
  activity: ActivityResponseDto,
  answers: Record<number, string>,
): boolean {
  if (!areAnswersOpen(activity)) return true;

  return questions.every((q) => {
    if (!q.isMandatory || q.id === undefined) return true;
    return (answers[q.id] ?? "").trim() !== "";
  });
}
