import { beforeEach, describe, expect, it, vi } from "vitest";
import type { ActivityResponseDto } from "~/api";
import {
  getActivityBackPath,
  handleEditActivityClick,
  loadActivityLoaderData,
} from "~/routes/activity/activity.handlers";
import { makeTokenParsed } from "~/testUtils";

const { getActivitiesById, getGroupsById, requireTokenParsed } = vi.hoisted(
  () => ({
    getActivitiesById: vi.fn(),
    getGroupsById: vi.fn(),
    requireTokenParsed: vi.fn(),
  }),
);

vi.mock("~/api", () => ({ getActivitiesById, getGroupsById }));
vi.mock("~/util/loaderAuth.util", () => ({ requireTokenParsed }));

const token = makeTokenParsed();

describe("loadActivityLoaderData", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    requireTokenParsed.mockResolvedValue(token);
  });

  it("returns the token, the activity and its organizer's name", async () => {
    const activity = {
      id: 1,
      name: "Party",
      organizerId: 7,
    } as ActivityResponseDto;
    getActivitiesById.mockResolvedValue({ data: activity });
    getGroupsById.mockResolvedValue({ data: { name: "Feestcie" } });

    await expect(loadActivityLoaderData({ id: "1" })).resolves.toEqual({
      tokenParsed: token,
      activity,
      organizerName: "Feestcie",
    });
    expect(getActivitiesById).toHaveBeenCalledWith({ path: { id: 1 } });
    expect(getGroupsById).toHaveBeenCalledWith({ path: { id: 7 } });
  });

  it("returns no organizer name when the activity has no organizer", async () => {
    getActivitiesById.mockResolvedValue({
      data: { id: 1, organizerId: null },
    });

    const data = await loadActivityLoaderData({ id: "1" });

    expect(data.organizerName).toBeNull();
    expect(getGroupsById).not.toHaveBeenCalled();
  });

  it("returns no organizer name when the organizer fails to load", async () => {
    getActivitiesById.mockResolvedValue({ data: { id: 1, organizerId: 7 } });
    getGroupsById.mockResolvedValue({ error: "fail" });

    const data = await loadActivityLoaderData({ id: "1" });

    expect(data.organizerName).toBeNull();
  });

  it("throws when the activity fails to load", async () => {
    getActivitiesById.mockResolvedValue({ error: "fail" });

    await expect(loadActivityLoaderData({ id: "1" })).rejects.toThrow(
      "Failed to load activities",
    );
  });
});

describe("getActivityBackPath", () => {
  it("returns the admin path when the current path starts with /admin", () => {
    expect(getActivityBackPath("/admin/activities/5")).toBe(
      "/admin/activities",
    );
  });

  it("returns the plain activities path otherwise", () => {
    expect(getActivityBackPath("/activities/5")).toBe("/activities");
  });
});

describe("handleEditActivityClick", () => {
  it("navigates to the admin edit path when in an admin context", () => {
    const navigate = vi.fn();
    handleEditActivityClick(navigate, "/admin/activities/5", 5);
    expect(navigate).toHaveBeenCalledWith("/admin/activities/edit/5");
  });

  it("navigates to the plain edit path otherwise", () => {
    const navigate = vi.fn();
    handleEditActivityClick(navigate, "/activities/5", 5);
    expect(navigate).toHaveBeenCalledWith("/activities/edit/5");
  });
});
