import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { MemberResponseDto } from "~/api";
import { isMemberInTargetAudience } from "~/util/targetaudience.util";

function buildMember(
  overrides: Partial<MemberResponseDto> = {},
): MemberResponseDto {
  return {
    id: "00000000-0000-0000-0000-000000000000",
    studyEnrollments: [],
    gratie: false,
    groupMemberships: [],
    ...overrides,
  } as unknown as MemberResponseDto;
}

describe("isMemberInTargetAudience", () => {
  beforeEach(() => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date("2026-06-01T00:00:00Z"));
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it("returns false for a null member", () => {
    expect(isMemberInTargetAudience(null, "All")).toBe(false);
  });

  it("returns false when the audience mask is 0 ('None')", () => {
    expect(isMemberInTargetAudience(buildMember(), "None")).toBe(false);
  });

  it("returns true for 'All' regardless of member data", () => {
    expect(isMemberInTargetAudience(buildMember(), "All")).toBe(true);
  });

  it("matches FirstYears based on a Bachelor enrollment within the last year", () => {
    const member = buildMember({
      studyEnrollments: [
        {
          studyType: "Bachelor",
          status: "Enrolled",
          enrollmentDate: "2026-01-01T00:00:00Z",
        },
      ],
    } as unknown as Partial<MemberResponseDto>);
    expect(isMemberInTargetAudience(member, "FirstYears")).toBe(true);
    expect(isMemberInTargetAudience(member, "SecondYears")).toBe(false);
  });

  it("matches SecondYears based on a Bachelor enrollment 1-2 years ago", () => {
    const member = buildMember({
      studyEnrollments: [
        {
          studyType: "Bachelor",
          status: "Enrolled",
          enrollmentDate: "2024-12-01T00:00:00Z",
        },
      ],
    } as unknown as Partial<MemberResponseDto>);
    expect(isMemberInTargetAudience(member, "SecondYears")).toBe(true);
  });

  it("matches ThirdYearsAndAbove for older Bachelor enrollments", () => {
    const member = buildMember({
      studyEnrollments: [
        {
          studyType: "Bachelor",
          status: "Enrolled",
          enrollmentDate: "2020-01-01T00:00:00Z",
        },
      ],
    } as unknown as Partial<MemberResponseDto>);
    expect(isMemberInTargetAudience(member, "ThirdYearsAndAbove")).toBe(true);
  });

  it("matches Masters for an enrolled Master study", () => {
    const member = buildMember({
      studyEnrollments: [
        {
          studyType: "Master",
          status: "Enrolled",
          enrollmentDate: "2026-01-01T00:00:00Z",
        },
      ],
    } as unknown as Partial<MemberResponseDto>);
    expect(isMemberInTargetAudience(member, "Masters")).toBe(true);
  });

  it("ignores study enrollments that are not Enrolled", () => {
    const member = buildMember({
      studyEnrollments: [
        {
          studyType: "Bachelor",
          status: "Dropout",
          enrollmentDate: "2026-01-01T00:00:00Z",
        },
      ],
    } as unknown as Partial<MemberResponseDto>);
    expect(isMemberInTargetAudience(member, "FirstYears")).toBe(false);
  });

  it("matches Gratie based on the member's gratie flag", () => {
    expect(
      isMemberInTargetAudience(buildMember({ gratie: true }), "Gratie"),
    ).toBe(true);
    expect(
      isMemberInTargetAudience(buildMember({ gratie: false }), "Gratie"),
    ).toBe(false);
  });

  it("matches ActiveMembers based on a group membership for the current committee year", () => {
    // System time is 2026-06-01, which is before the Aug 1 rollover, so the
    // current committee year is 2026.
    const member = buildMember({
      groupMemberships: [{ membershipYear: 2026 }],
    } as unknown as Partial<MemberResponseDto>);
    expect(isMemberInTargetAudience(member, "ActiveMembers")).toBe(true);
    expect(
      isMemberInTargetAudience(
        buildMember({
          groupMemberships: [{ membershipYear: 2025 }],
        } as unknown as Partial<MemberResponseDto>),
        "ActiveMembers",
      ),
    ).toBe(false);
  });

  it("returns false when the member has no studyEnrollments at all", () => {
    const member = { id: "x" } as unknown as MemberResponseDto;
    expect(isMemberInTargetAudience(member, "All")).toBe(false);
  });

  it("returns true when any of several combined flags match", () => {
    const member = buildMember({ gratie: true });
    expect(isMemberInTargetAudience(member, "FirstYears, Gratie")).toBe(true);
  });
});
