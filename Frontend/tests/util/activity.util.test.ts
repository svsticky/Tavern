import { describe, expect, it } from "vitest";
import type { ActivityResponseDto } from "~/api";
import { getActivityEnrollmentStatus } from "~/util/activity.util";

function buildActivity(
  overrides: Partial<ActivityResponseDto> = {},
): ActivityResponseDto {
  return {
    id: 1,
    name: "Party",
    isEnrollable: true,
    dateTimeStart: "2026-06-01T10:00:00Z",
    dateTimeEnd: "2026-06-01T12:00:00Z",
    enrollmentDeadline: undefined,
    unenrollmentDeadline: undefined,
    enrollOpenDate: undefined,
    ...overrides,
  } as ActivityResponseDto;
}

const now = new Date("2026-05-01T00:00:00Z");

describe("getActivityEnrollmentStatus", () => {
  it("allows enrolling and unenrolling for an isEnrollable activity before its start", () => {
    const activity = buildActivity();
    const status = getActivityEnrollmentStatus(activity, now);
    expect(status.canEnroll).toBe(true);
    expect(status.canUnenroll).toBe(true);
  });

  it("disallows enrolling when the activity is not enrollable and not yet open", () => {
    const activity = buildActivity({
      isEnrollable: false,
      enrollOpenDate: undefined,
    });
    const status = getActivityEnrollmentStatus(activity, now);
    expect(status.canEnroll).toBe(false);
  });

  it("allows enrolling once the enrollOpenDate has passed, even if not isEnrollable", () => {
    const activity = buildActivity({
      isEnrollable: false,
      enrollOpenDate: "2026-04-01T00:00:00Z",
    });
    const status = getActivityEnrollmentStatus(activity, now);
    expect(status.canEnroll).toBe(true);
  });

  it("disallows enrolling before the enrollOpenDate", () => {
    const activity = buildActivity({
      isEnrollable: false,
      enrollOpenDate: "2026-06-01T00:00:00Z",
    });
    const status = getActivityEnrollmentStatus(activity, now);
    expect(status.canEnroll).toBe(false);
  });

  it("uses the explicit enrollmentDeadline over dateTimeEnd when present", () => {
    const activity = buildActivity({
      enrollmentDeadline: "2026-04-15T00:00:00Z",
    });
    const status = getActivityEnrollmentStatus(activity, now);
    expect(status.canEnroll).toBe(false);
  });

  it("falls back to dateTimeStart as the deadline when neither enrollmentDeadline nor dateTimeEnd exist", () => {
    const activity = buildActivity({
      dateTimeEnd: undefined,
      dateTimeStart: "2026-04-15T00:00:00Z",
    });
    const status = getActivityEnrollmentStatus(activity, now);
    expect(status.canEnroll).toBe(false);
  });

  it("disallows unenrolling once the unenrollmentDeadline has passed", () => {
    const activity = buildActivity({
      unenrollmentDeadline: "2026-04-15T00:00:00Z",
    });
    const status = getActivityEnrollmentStatus(activity, now);
    expect(status.canUnenroll).toBe(false);
  });

  it("disallows unenrolling after the enrollment window itself has closed", () => {
    const activity = buildActivity({
      dateTimeEnd: "2026-04-15T00:00:00Z",
    });
    const status = getActivityEnrollmentStatus(activity, now);
    expect(status.canUnenroll).toBe(false);
  });
});
