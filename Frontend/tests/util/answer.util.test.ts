import { describe, expect, it } from "vitest";
import type {
  ActivityResponseDto,
  GetSpecificationQuestionResponseDto,
} from "~/api";
import {
  getQuestionEffectiveDeadline,
  hasAllMandatoryAnswers,
  isQuestionAnswerable,
} from "~/util/answer.util";

function question(
  overrides: Partial<GetSpecificationQuestionResponseDto>,
): GetSpecificationQuestionResponseDto {
  return {
    id: 1,
    questionDutch: "Vraag",
    questionEnglish: "Question",
    type: "String",
    isMandatory: true,
    closeOnUnenrollmentDeadline: false,
    ...overrides,
  } as GetSpecificationQuestionResponseDto;
}

function activity(
  overrides: Partial<ActivityResponseDto> = {},
): ActivityResponseDto {
  return {
    id: 1,
    name: "Party",
    dateTimeStart: "2027-01-01T10:00:00Z",
    dateTimeEnd: "2027-01-01T12:00:00Z",
    enrollmentDeadline: undefined,
    unenrollmentDeadline: undefined,
    ...overrides,
  } as ActivityResponseDto;
}

describe("hasAllMandatoryAnswers", () => {
  it("passes when every mandatory question has a non-empty answer", () => {
    expect(hasAllMandatoryAnswers([question({})], activity(), { 1: "x" })).toBe(
      true,
    );
  });

  it("fails when a mandatory question has no answer at all", () => {
    expect(hasAllMandatoryAnswers([question({})], activity(), {})).toBe(false);
  });

  it("fails when a mandatory question's answer is an empty string", () => {
    // This is the state a MultipleChoice question is left in while its
    // blank placeholder option is selected - the "select an option" default
    // must never be mistaken for a real answer.
    expect(
      hasAllMandatoryAnswers(
        [question({ type: "MultipleChoice", options: ["A", "B"] })],
        activity(),
        { 1: "" },
      ),
    ).toBe(false);
  });

  it("passes once a mandatory MultipleChoice question has a real selected option", () => {
    expect(
      hasAllMandatoryAnswers(
        [question({ type: "MultipleChoice", options: ["A", "B"] })],
        activity(),
        { 1: "A" },
      ),
    ).toBe(true);
  });

  it("ignores unanswered non-mandatory questions", () => {
    expect(
      hasAllMandatoryAnswers(
        [question({ isMandatory: false })],
        activity(),
        {},
      ),
    ).toBe(true);
  });

  it("skips questions with no id", () => {
    expect(
      hasAllMandatoryAnswers([question({ id: undefined })], activity(), {}),
    ).toBe(true);
  });

  it("ignores an unanswered mandatory question whose answer deadline has passed", () => {
    expect(
      hasAllMandatoryAnswers(
        [question({})],
        activity({ enrollmentDeadline: "2020-01-01T00:00:00Z" }),
        {},
      ),
    ).toBe(true);
  });
});

describe("getQuestionEffectiveDeadline", () => {
  it("uses the enrollment deadline when the question doesn't close on unenrollment", () => {
    const deadline = getQuestionEffectiveDeadline(
      question({ closeOnUnenrollmentDeadline: false }),
      activity({
        enrollmentDeadline: "2026-11-01T00:00:00Z",
        unenrollmentDeadline: "2026-06-01T00:00:00Z",
      }),
    );
    expect(deadline?.toISOString()).toBe("2026-11-01T00:00:00.000Z");
  });

  it("uses the unenrollment deadline when the question closes on unenrollment and it is set", () => {
    const deadline = getQuestionEffectiveDeadline(
      question({ closeOnUnenrollmentDeadline: true }),
      activity({
        enrollmentDeadline: "2026-11-01T00:00:00Z",
        unenrollmentDeadline: "2026-06-01T00:00:00Z",
      }),
    );
    expect(deadline?.toISOString()).toBe("2026-06-01T00:00:00.000Z");
  });

  it("falls back to the enrollment deadline when closing on unenrollment but there is no unenrollment deadline", () => {
    const deadline = getQuestionEffectiveDeadline(
      question({ closeOnUnenrollmentDeadline: true }),
      activity({
        enrollmentDeadline: "2026-11-01T00:00:00Z",
        unenrollmentDeadline: undefined,
      }),
    );
    expect(deadline?.toISOString()).toBe("2026-11-01T00:00:00.000Z");
  });

  it("falls back to the activity's end date when neither deadline is set", () => {
    const deadline = getQuestionEffectiveDeadline(question({}), activity({}));
    expect(deadline?.toISOString()).toBe("2027-01-01T12:00:00.000Z");
  });
});

describe("isQuestionAnswerable", () => {
  it("returns true before the effective deadline", () => {
    expect(
      isQuestionAnswerable(
        question({}),
        activity({ dateTimeEnd: "2027-01-01T12:00:00Z" }),
        new Date("2026-01-01T00:00:00Z"),
      ),
    ).toBe(true);
  });

  it("returns false after the effective deadline", () => {
    expect(
      isQuestionAnswerable(
        question({}),
        activity({ dateTimeEnd: "2020-01-01T12:00:00Z" }),
        new Date("2026-01-01T00:00:00Z"),
      ),
    ).toBe(false);
  });

  it("closes at the unenrollment deadline when closeOnUnenrollmentDeadline is true, even though the enrollment deadline is still in the future", () => {
    const now = new Date("2026-06-01T00:00:00Z");
    expect(
      isQuestionAnswerable(
        question({ closeOnUnenrollmentDeadline: true }),
        activity({
          enrollmentDeadline: "2026-12-01T00:00:00Z",
          unenrollmentDeadline: "2026-05-01T00:00:00Z",
        }),
        now,
      ),
    ).toBe(false);
  });

  it("ignores a passed unenrollment deadline when closeOnUnenrollmentDeadline is false", () => {
    const now = new Date("2026-06-01T00:00:00Z");
    expect(
      isQuestionAnswerable(
        question({ closeOnUnenrollmentDeadline: false }),
        activity({
          enrollmentDeadline: "2026-12-01T00:00:00Z",
          unenrollmentDeadline: "2026-05-01T00:00:00Z",
        }),
        now,
      ),
    ).toBe(true);
  });
});
