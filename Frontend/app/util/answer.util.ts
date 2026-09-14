import type {
  ActivityResponseDto,
  GetSpecificationQuestionResponseDto,
} from "~/api";

/**
 * Determines the effective deadline until which a specification question can be answered or
 * changed: the question's own deadline if set, otherwise the activity's enrollment deadline,
 * otherwise the activity's end date and time. Mirrors the backend's `QuestionDeadlineHelper`.
 *
 * @param question - The specification question.
 * @param activity - The activity the question belongs to.
 * @returns The effective deadline, or null if none of the three dates is available.
 */
export function getQuestionEffectiveDeadline(
  question: GetSpecificationQuestionResponseDto,
  activity: ActivityResponseDto,
): Date | null {
  const deadline =
    question.answerDeadline ?? activity.enrollmentDeadline ?? activity.dateTimeEnd;
  return deadline ? new Date(deadline) : null;
}

/**
 * Determines whether a specification question can still be answered or changed.
 *
 * @param question - The specification question.
 * @param activity - The activity the question belongs to.
 * @param now - The reference date (defaults to current date).
 */
export function isQuestionAnswerable(
  question: GetSpecificationQuestionResponseDto,
  activity: ActivityResponseDto,
  now: Date = new Date(),
): boolean {
  const deadline = getQuestionEffectiveDeadline(question, activity);
  return !deadline || now <= deadline;
}

/**
 * Checks whether every mandatory specification question that can still be answered has a
 * non-empty answer. A mandatory question whose answer deadline has already passed is no longer
 * required - it can no longer be filled in anyway.
 *
 * @param questions - The specification questions to validate against.
 * @param activity - The activity the questions belong to.
 * @param answers - Current answers keyed by question id.
 * @returns True if all still-answerable mandatory questions have been answered.
 */
export function hasAllMandatoryAnswers(
  questions: GetSpecificationQuestionResponseDto[],
  activity: ActivityResponseDto,
  answers: Record<number, string>,
): boolean {
  return questions.every((q) => {
    if (!q.isMandatory || q.id === undefined) return true;
    if (!isQuestionAnswerable(q, activity)) return true;
    return (answers[q.id] ?? "").trim() !== "";
  });
}
