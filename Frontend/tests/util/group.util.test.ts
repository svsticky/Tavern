import { describe, expect, it } from "vitest";
import type { ActivityResponseDto } from "~/api/types.gen";
import type { Permission } from "~/types/Permission";
import type { GroupMembershipClaim, TokenParsed } from "~/types/TokenParsed";
import {
  canEditActivity,
  getGroupIdsWithPermission,
  hasPermission,
  isBoardOrCandidateBoard,
  isInGroupWithId,
} from "~/util/group.util";

function membershipEntry(
  id: number,
  name: string,
  permissions: Permission[] = [],
  role: GroupMembershipClaim["role"] = null,
): GroupMembershipClaim {
  return { id, name, permissions, role };
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

describe("isInGroupWithId", () => {
  it("matches by group id", () => {
    const token = buildToken({
      group_memberships: [membershipEntry(42, "Web")],
    });
    expect(isInGroupWithId(token, 42)).toBe(true);
    expect(isInGroupWithId(token, 99)).toBe(false);
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

describe("hasPermission", () => {
  it("always returns true for board/candidate board members", () => {
    const token = buildToken({ is_admin: true });
    expect(hasPermission(token, "ManageMembers")).toBe(true);
  });

  it("returns false when the token has no memberships", () => {
    expect(hasPermission(buildToken(), "ViewMembers")).toBe(false);
  });

  it("matches a permission granted directly to a group", () => {
    const token = buildToken({
      group_memberships: [membershipEntry(1, "Web", ["ViewMembers"])],
    });
    expect(hasPermission(token, "ViewMembers")).toBe(true);
    expect(hasPermission(token, "ManageMembers")).toBe(false);
  });

  it("matches a permission granted to the member's role", () => {
    const token = buildToken({
      group_memberships: [
        membershipEntry(1, "Web", [], {
          id: 5,
          name: "Chair",
          alias: "Voorzitter",
          permissions: ["ViewFinances"],
        }),
      ],
    });
    expect(hasPermission(token, "ViewFinances")).toBe(true);
  });

  it("for the group-scoped EditActivityForGroup, only matches the specified group", () => {
    const token = buildToken({
      group_memberships: [
        membershipEntry(1, "Web", ["EditActivityForGroup"]),
        membershipEntry(2, "Finance"),
      ],
    });
    expect(hasPermission(token, "EditActivityForGroup", 1)).toBe(true);
    expect(hasPermission(token, "EditActivityForGroup", 2)).toBe(false);
  });

  it("for global-effect permissions, ignores which group granted it", () => {
    const token = buildToken({
      group_memberships: [membershipEntry(1, "Web", ["ManageMembers"])],
    });
    expect(hasPermission(token, "ManageMembers", 999)).toBe(true);
  });
});

describe("getGroupIdsWithPermission", () => {
  it("returns the ids of every group granting the permission, directly or via role", () => {
    const token = buildToken({
      group_memberships: [
        membershipEntry(1, "Web", ["EditActivityForGroup"]),
        membershipEntry(2, "Finance", [], {
          id: 5,
          name: "Chair",
          alias: "Voorzitter",
          permissions: ["EditActivityForGroup"],
        }),
        membershipEntry(3, "Board"),
      ],
    });
    expect(
      getGroupIdsWithPermission(token, "EditActivityForGroup").sort(),
    ).toEqual([1, 2]);
  });

  it("returns an empty array when the token has no memberships", () => {
    expect(getGroupIdsWithPermission(buildToken(), "ManageMembers")).toEqual(
      [],
    );
  });
});

describe("canEditActivity", () => {
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

  it("allows an EditAllActivities holder to edit any activity", () => {
    const token = buildToken({
      group_memberships: [membershipEntry(1, "Board", ["EditAllActivities"])],
    });
    expect(canEditActivity(buildActivity({ showOnWebsite: true }), token)).toBe(
      true,
    );
  });

  it("allows a member with EditActivityForGroup for the organizing group to edit before it has started", () => {
    const token = buildToken({
      group_memberships: [membershipEntry(7, "Web", ["EditActivityForGroup"])],
    });
    expect(canEditActivity(buildActivity(), token)).toBe(true);
  });

  it("disallows a member with EditActivityForGroup for a different group", () => {
    const token = buildToken({
      group_memberships: [membershipEntry(9, "Web", ["EditActivityForGroup"])],
    });
    expect(canEditActivity(buildActivity(), token)).toBe(false);
  });

  it("disallows a non-organizer, non-board member from editing", () => {
    const token = buildToken({ group_memberships: [] });
    expect(canEditActivity(buildActivity(), token)).toBe(false);
  });

  it("disallows editing once the activity has been published to the website", () => {
    const token = buildToken({
      group_memberships: [membershipEntry(7, "Web", ["EditActivityForGroup"])],
    });
    expect(canEditActivity(buildActivity({ showOnWebsite: true }), token)).toBe(
      false,
    );
  });

  it("disallows editing once the activity has already started", () => {
    const token = buildToken({
      group_memberships: [membershipEntry(7, "Web", ["EditActivityForGroup"])],
    });
    expect(
      canEditActivity(
        buildActivity({ dateTimeStart: "2000-01-01T00:00:00Z" }),
        token,
      ),
    ).toBe(false);
  });
});
