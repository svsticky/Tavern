import { beforeEach, describe, expect, it, vi } from "vitest";
import type { ActivityResponseDto } from "~/api";
import {
  getEditActivityBackPath,
  loadEditActivityLoaderData,
} from "~/routes/edit-activity/edit-activity.handlers";
import { makeTokenParsed } from "~/testUtils";

const { getActivitiesById, getGroups, requireTokenParsed, loadCommitteeYear } =
  vi.hoisted(() => ({
    getActivitiesById: vi.fn(),
    getGroups: vi.fn(),
    requireTokenParsed: vi.fn(),
    loadCommitteeYear: vi.fn(),
  }));

vi.mock("~/api", () => ({ getActivitiesById, getGroups }));
vi.mock("~/util/loaderAuth.util", () => ({
  requireTokenParsed,
  loadCommitteeYear,
}));

const token = makeTokenParsed();
const groups = [{ id: 1, name: "Feestcie" }];

describe("loadEditActivityLoaderData", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    requireTokenParsed.mockResolvedValue(token);
    loadCommitteeYear.mockResolvedValue(2026);
    getGroups.mockResolvedValue({ data: groups });
  });

  it("loads the active groups of the current committee year", async () => {
    await loadEditActivityLoaderData({});

    expect(getGroups).toHaveBeenCalledWith({
      query: { IncludeInactive: false, MembershipYear: 2026 },
    });
  });

  it("returns the token and groups without an activity when creating", async () => {
    await expect(loadEditActivityLoaderData({})).resolves.toEqual({
      tokenParsed: token,
      activity: null,
      groups,
    });
    expect(getActivitiesById).not.toHaveBeenCalled();
  });

  it("loads the activity when editing", async () => {
    const activity = { id: 5, name: "Party" } as ActivityResponseDto;
    getActivitiesById.mockResolvedValue({ data: activity });

    await expect(loadEditActivityLoaderData({ id: "5" })).resolves.toEqual({
      tokenParsed: token,
      activity,
      groups,
    });
    expect(getActivitiesById).toHaveBeenCalledWith({ path: { id: 5 } });
  });

  it("throws when the groups fail to load", async () => {
    getGroups.mockResolvedValue({ error: "fail" });

    await expect(loadEditActivityLoaderData({})).rejects.toThrow(
      "Failed to load groups",
    );
  });

  it("throws when the activity fails to load", async () => {
    getActivitiesById.mockResolvedValue({ error: "fail" });

    await expect(loadEditActivityLoaderData({ id: "5" })).rejects.toThrow(
      "Failed to load activities",
    );
  });
});

describe("getEditActivityBackPath", () => {
  it("returns the admin activity detail path when editing in an admin context", () => {
    expect(getEditActivityBackPath("/admin/activities/edit/5", true, "5")).toBe(
      "/admin/activities/5",
    );
  });

  it("returns the plain activity detail path when editing outside admin", () => {
    expect(getEditActivityBackPath("/activities/edit/5", true, "5")).toBe(
      "/activities/5",
    );
  });

  it("returns the admin activities list path when creating in an admin context", () => {
    expect(
      getEditActivityBackPath("/admin/activities/create", false, undefined),
    ).toBe("/admin/activities");
  });

  it("returns the plain activities list path when creating outside admin", () => {
    expect(
      getEditActivityBackPath("/activities/create", false, undefined),
    ).toBe("/activities");
  });
});
