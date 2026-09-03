import { describe, expect, it } from "vitest";
import type { GetSpecificationQuestionResponseDto } from "~/api";
import { hasAllMandatoryAnswers } from "~/util/answer.util";

function question(
  overrides: Partial<GetSpecificationQuestionResponseDto>,
): GetSpecificationQuestionResponseDto {
  return {
    id: 1,
    questionDutch: "Vraag",
    questionEnglish: "Question",
    type: "String",
    isMandatory: true,
    ...overrides,
  } as GetSpecificationQuestionResponseDto;
}

describe("hasAllMandatoryAnswers", () => {
  it("passes when every mandatory question has a non-empty answer", () => {
    expect(hasAllMandatoryAnswers([question({})], { 1: "x" })).toBe(true);
  });

  it("fails when a mandatory question has no answer at all", () => {
    expect(hasAllMandatoryAnswers([question({})], {})).toBe(false);
  });

  it("fails when a mandatory question's answer is an empty string", () => {
    // This is the state a MultipleChoice question is left in while its
    // blank placeholder option is selected - the "select an option" default
    // must never be mistaken for a real answer.
    expect(
      hasAllMandatoryAnswers(
        [question({ type: "MultipleChoice", options: ["A", "B"] })],
        { 1: "" },
      ),
    ).toBe(false);
  });

  it("passes once a mandatory MultipleChoice question has a real selected option", () => {
    expect(
      hasAllMandatoryAnswers(
        [question({ type: "MultipleChoice", options: ["A", "B"] })],
        { 1: "A" },
      ),
    ).toBe(true);
  });

  it("ignores unanswered non-mandatory questions", () => {
    expect(hasAllMandatoryAnswers([question({ isMandatory: false })], {})).toBe(
      true,
    );
  });

  it("skips questions with no id", () => {
    expect(
      hasAllMandatoryAnswers([question({ id: undefined })], {}),
    ).toBe(true);
  });
});
