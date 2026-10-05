import { describe, expect, it } from "vitest";
import {
  appendErrorMessage,
  getErrorMessage,
  getFriendlyErrorMessage,
} from "~/util/error.util";

describe("getErrorMessage", () => {
  it("returns the string directly when the error is a string", () => {
    expect(getErrorMessage("oops")).toBe("oops");
  });

  it("trims and ignores blank strings", () => {
    expect(getErrorMessage("   ")).toBeUndefined();
  });

  it("returns the message when the error is an Error instance", () => {
    expect(getErrorMessage(new Error("boom"))).toBe("boom");
  });

  it("returns undefined for null, undefined, and non-object primitives", () => {
    expect(getErrorMessage(null)).toBeUndefined();
    expect(getErrorMessage(undefined)).toBeUndefined();
    expect(getErrorMessage(42)).toBeUndefined();
  });

  it("extracts the first validation error message, prefixed by field name", () => {
    expect(
      getErrorMessage({
        errors: { Email: ["Email is required", "Email is invalid"] },
      }),
    ).toBe("Email: Email is required");
  });

  it("skips validation fields with empty arrays and falls through to the next", () => {
    expect(
      getErrorMessage({
        errors: { Empty: [], Name: ["Name is required"] },
      }),
    ).toBe("Name: Name is required");
  });

  it("falls back to `detail`, then `title`, then `message`, then `error`, in that order", () => {
    expect(getErrorMessage({ detail: "detail msg", title: "title msg" })).toBe(
      "detail msg",
    );
    expect(getErrorMessage({ title: "title msg", message: "msg" })).toBe(
      "title msg",
    );
    expect(getErrorMessage({ message: "msg", error: "err" })).toBe("msg");
    expect(getErrorMessage({ error: "err" })).toBe("err");
  });

  it("returns undefined when none of the known shapes match", () => {
    expect(getErrorMessage({ foo: "bar" })).toBeUndefined();
  });

  it("extracts a non-array validation error string, prefixed by field name", () => {
    expect(getErrorMessage({ errors: { General: "Something failed" } })).toBe(
      "General: Something failed",
    );
  });

  it("skips array fields with no valid string entries and falls through", () => {
    expect(
      getErrorMessage({
        errors: { Empty: ["", "   "], Name: ["Name is required"] },
      }),
    ).toBe("Name: Name is required");
  });

  it("returns undefined when the errors object has no usable messages", () => {
    expect(getErrorMessage({ errors: { Empty: [] } })).toBeUndefined();
  });

  it("returns undefined when errors is not an object", () => {
    expect(getErrorMessage({ errors: "not an object" })).toBeUndefined();
  });
});

describe("appendErrorMessage", () => {
  it("appends the extracted error message to the base message", () => {
    expect(appendErrorMessage("Save failed", new Error("network down"))).toBe(
      "Save failed: network down",
    );
  });

  it("returns just the base message when no error message can be extracted", () => {
    expect(appendErrorMessage("Save failed", undefined)).toBe("Save failed");
    expect(appendErrorMessage("Save failed", {})).toBe("Save failed");
  });
});

describe("getFriendlyErrorMessage", () => {
  it("maps a known duplicate-email backend message to its translation key", () => {
    expect(
      getFriendlyErrorMessage(
        "Registration failed",
        new Error("An account with this email address already exists."),
      ),
    ).toBe("email_already_registered");
  });

  it("maps a known duplicate-student-number backend message to its translation key", () => {
    expect(
      getFriendlyErrorMessage(
        "Registration failed",
        new Error("An account with this student number already exists."),
      ),
    ).toBe("student_number_already_registered");
  });

  it("maps known enrollment backend messages to their translation keys", () => {
    expect(
      getFriendlyErrorMessage(
        "Enrollment failed",
        new Error("Member is already enrolled (or on waiting list)."),
      ),
    ).toBe("member_already_enrolled_in_activity");

    expect(
      getFriendlyErrorMessage(
        "Enrollment failed",
        new Error("Member does not have a paid membership payment."),
      ),
    ).toBe("member_no_paid_membership");

    expect(
      getFriendlyErrorMessage(
        "Enrollment failed",
        new Error("Member is suspended and cannot enroll in activities."),
      ),
    ).toBe("member_suspended");

    expect(
      getFriendlyErrorMessage(
        "Enrollment failed",
        new Error(
          "Member does not meet the age requirement for this activity.",
        ),
      ),
    ).toBe("member_age_requirement_not_met");

    expect(
      getFriendlyErrorMessage(
        "Enrollment failed",
        new Error("Missing mandatory answers."),
      ),
    ).toBe("missing_mandatory_answers");

    expect(
      getFriendlyErrorMessage(
        "Enrollment failed",
        new Error(
          "Cannot answer or change this question after its answer deadline has passed.",
        ),
      ),
    ).toBe("question_deadline_passed");

    expect(
      getFriendlyErrorMessage(
        "Enrollment failed",
        new Error("Enrollment deadline has passed."),
      ),
    ).toBe("enrollment_deadline_passed");

    expect(
      getFriendlyErrorMessage(
        "Enrollment failed",
        new Error("Activity is not open for enrollment."),
      ),
    ).toBe("activity_not_open_for_enrollment");

    expect(
      getFriendlyErrorMessage(
        "Enrollment failed",
        new Error("Activity is not visible for enrollment."),
      ),
    ).toBe("activity_not_visible_for_enrollment");

    expect(
      getFriendlyErrorMessage(
        "Enrollment failed",
        new Error("Activity has already started."),
      ),
    ).toBe("activity_already_started");

    expect(
      getFriendlyErrorMessage(
        "Enrollment failed",
        new Error("Unenrollment deadline has passed."),
      ),
    ).toBe("unenrollment_deadline_passed");
  });

  it("falls back to appending the raw message for unknown errors", () => {
    expect(
      getFriendlyErrorMessage("Registration failed", new Error("boom")),
    ).toBe("Registration failed: boom");
  });

  it("returns just the base message when no error message can be extracted", () => {
    expect(getFriendlyErrorMessage("Registration failed", undefined)).toBe(
      "Registration failed",
    );
  });
});
