import type { GetSpecificationQuestionResponseDto } from "~/api";

/**
 * Checks whether every mandatory specification question has a non-empty answer.
 *
 * @param questions - The specification questions to validate against.
 * @param answers - Current answers keyed by question id.
 * @returns True if all mandatory questions have been answered.
 */
export function hasAllMandatoryAnswers(
  questions: GetSpecificationQuestionResponseDto[],
  answers: Record<number, string>,
): boolean {
  return questions.every((q) => {
    if (!q.isMandatory || q.id === undefined) return true;
    return (answers[q.id] ?? "").trim() !== "";
  });
}
