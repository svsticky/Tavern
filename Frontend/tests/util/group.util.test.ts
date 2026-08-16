import { describe, expect, it } from "vitest";
import type { ActivityResponseDto } from "~/api/types.gen";
import type { TokenParsed } from "~/types/TokenParsed";
import { getCommitteeYear } from "~/util/date.util";
import {
  canEditActivity,
  isBoardOrCandidateBoard,
  isInGroupWithId,
  isInGroupWithName,
} from "~/util/group.util";

function membershipEntry(
  year: number,
  groupId: number,
  groupName: string,
  roleId: number,
  roleName: string,
) {
  return `${year}:${groupId};${groupName}:${roleId};${roleName}`;
}

function buildToken(overrides: Partial<TokenParsed> = {}): TokenParsed {
  return {
    locale: "en",
    UserId: "00000000-0000-0000-0000-000000000000" as TokenParsed["UserId"],
    access_level: "member",
    given_name: "Test",
    family_name: "User",
    name: "Test User",
    ...overrides,
  };
}

describe("isInGroupWithName", () => {
  const year = getCommitteeYear();

  it("returns false when there are no group memberships", () => {
    expect(isInGroupWithName(buildToken(), "Web")).toBe(false);
  });

  it("matches by group name for the current committee year by default", () => {
    const token = buildToken({
      group_memberships: [membershipEntry(year, 1, "Web", 2, "Chair")],
    });
    expect(isInGroupWithName(token, "Web")).toBe(true);
  });

  it("does not match a different group name", () => {
    const token = buildToken({
      group_memberships: [membershipEntry(year, 1, "Web", 2, "Chair")],
    });
    expect(isInGroupWithName(token, "Finance")).toBe(false);
  });

  it("matches by role when a role is specified", () => {
    const token = buildToken({
      group_memberships: [membershipEntry(year, 1, "Web", 2, "Chair")],
    });
    expect(isInGroupWithName(token, "Web", "Chair")).toBe(true);
    expect(isInGroupWithName(token, "Web", "Secretary")).toBe(false);
  });

  it("respects an explicit year override", () => {
    const token = buildToken({
      group_memberships: [membershipEntry(year - 1, 1, "Web", 2, "Chair")],
    });
    expect(isInGroupWithName(token, "Web", undefined, year - 1)).toBe(true);
    expect(isInGroupWithName(token, "Web")).toBe(false);
  });
});

describe("isInGroupWithId", () => {
  const year = getCommitteeYear();

  it("matches by group id for the current committee year", () => {
    const token = buildToken({
      group_memberships: [membershipEntry(year, 42, "Web", 2, "Chair")],
    });
    expect(isInGroupWithId(token, 42)).toBe(true);
    expect(isInGroupWithId(token, 99)).toBe(false);
  });

  it("matches by role id when specified", () => {
    const token = buildToken({
      group_memberships: [membershipEntry(year, 42, "Web", 2, "Chair")],
    });
    expect(isInGroupWithId(token, 42, "2")).toBe(true);
    expect(isInGroupWithId(token, 42, "3")).toBe(false);
  });

  it("returns false when there are no group memberships", () => {
    expect(isInGroupWithId(buildToken(), 42)).toBe(false);
  });
});

describe("isBoardOrCandidateBoard", () => {
  it("returns false for a null token", () => {
    expect(isBoardOrCandidateBoard(null)).toBe(false);
  });

  it("reflects the is_admin flag", () => {
    expect(isBoardOrCandidateBoard(buildToken({ is_admin: true }))).toBe(true);
    expect(isBoardOrCandidateBoard(buildToken({ is_admin: false }))).toBe(
      false,
    );
    expect(isBoardOrCandidateBoard(buildToken({}))).toBe(false);
  });
});

describe("canEditActivity", () => {
  const year = getCommitteeYear();

  function buildActivity(
    overrides: Partial<ActivityResponseDto> = {},
  ): ActivityResponseDto {
    return {
      id: 1,
      name: "Party",
      showInKoala: false,
      showOnWebsite: false,
      organizerId: 7,
      dateTimeStart: "2999-01-01T00:00:00Z",
      dateTimeEnd: "2999-01-01T02:00:00Z",
      ...overrides,
    } as ActivityResponseDto;
  }

  it("always allows board members to edit", () => {
    const token = buildToken({ is_admin: true });
    expect(canEditActivity(buildActivity(), token)).toBe(true);
  });

  it("allows an organizer in the organizing group to edit before the activity has started", () => {
    const token = buildToken({
      group_memberships: [membershipEntry(year, 7, "Web", 2, "Chair")],
    });
    expect(canEditActivity(buildActivity(), token)).toBe(true);
  });

  it("disallows a non-organizer, non-board member from editing", () => {
    const token = buildToken({ group_memberships: [] });
    expect(canEditActivity(buildActivity(), token)).toBe(false);
  });

  it("disallows editing once the activity has been published to the website", () => {
    const token = buildToken({
      group_memberships: [membershipEntry(year, 7, "Web", 2, "Chair")],
    });
    expect(canEditActivity(buildActivity({ showOnWebsite: true }), token)).toBe(
      false,
    );
  });

  it("disallows editing once the activity has already started", () => {
    const token = buildToken({
      group_memberships: [membershipEntry(year, 7, "Web", 2, "Chair")],
    });
    expect(
      canEditActivity(
        buildActivity({ dateTimeStart: "2000-01-01T00:00:00Z" }),
        token,
      ),
    ).toBe(false);
  });
});
