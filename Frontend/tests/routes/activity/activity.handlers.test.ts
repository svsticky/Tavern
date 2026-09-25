import { beforeEach, describe, expect, it, vi } from "vitest";
import type { ActivityResponseDto } from "~/api";
import {
  fetchActivity,
  fetchOrganizerName,
  getActivityBackPath,
  handleEditActivityClick,
} from "~/routes/activity/activity.handlers";

const { getActivitiesById, getGroupsById } = vi.hoisted(() => ({
  getActivitiesById: vi.fn(),
  getGroupsById: vi.fn(),
}));

vi.mock("~/api", () => ({ getActivitiesById, getGroupsById }));

describe("fetchActivity", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("returns the activity on success", async () => {
    const activity = { id: 1, name: "Party" } as ActivityResponseDto;
    getActivitiesById.mockResolvedValue({ data: activity });

    await expect(fetchActivity(1)).resolves.toBe(activity);
    expect(getActivitiesById).toHaveBeenCalledWith({ path: { id: 1 } });
  });

  it("throws the API error so React Router's error boundary handles it", async () => {
    getActivitiesById.mockResolvedValue({ error: new Error("fail") });

    await expect(fetchActivity(1)).rejects.toThrow("fail");
  });

  it("throws when the response has no data", async () => {
    getActivitiesById.mockResolvedValue({});

    await expect(fetchActivity(1)).rejects.toThrow("Failed to load activity");
  });
});

describe("fetchOrganizerName", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("returns the group's name", async () => {
    getGroupsById.mockResolvedValue({ data: { name: "BaCo" } });

    await expect(fetchOrganizerName(5)).resolves.toBe("BaCo");
    expect(getGroupsById).toHaveBeenCalledWith({ path: { id: 5 } });
  });

  for (const organizerId of [null, undefined, 0]) {
    it(`skips the lookup when there is no organizer (${JSON.stringify(organizerId)})`, async () => {
      await expect(fetchOrganizerName(organizerId)).resolves.toBeNull();
      expect(getGroupsById).not.toHaveBeenCalled();
    });
  }

  it("yields null instead of failing the page when the lookup fails", async () => {
    getGroupsById.mockResolvedValue({ error: "nope" });

    await expect(fetchOrganizerName(5)).resolves.toBeNull();
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
