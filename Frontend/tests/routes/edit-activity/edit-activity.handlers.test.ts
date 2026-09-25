import { beforeEach, describe, expect, it, vi } from "vitest";
import type { ActivityResponseDto } from "~/api";
import {
  fetchEditActivity,
  getEditActivityBackPath,
} from "~/routes/edit-activity/edit-activity.handlers";

const { getActivitiesById } = vi.hoisted(() => ({
  getActivitiesById: vi.fn(),
}));

vi.mock("~/api", () => ({ getActivitiesById }));

describe("fetchEditActivity", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("returns the activity to edit", async () => {
    const activity = { id: 5, name: "Party" } as ActivityResponseDto;
    getActivitiesById.mockResolvedValue({ data: activity });

    await expect(fetchEditActivity("5")).resolves.toBe(activity);
    expect(getActivitiesById).toHaveBeenCalledWith({ path: { id: 5 } });
  });

  it("throws the API error so React Router's error boundary handles it", async () => {
    getActivitiesById.mockResolvedValue({ error: new Error("fail") });

    await expect(fetchEditActivity("5")).rejects.toThrow("fail");
  });

  it("throws when the response has no data", async () => {
    getActivitiesById.mockResolvedValue({});

    await expect(fetchEditActivity("5")).rejects.toThrow(
      "Failed to load activity",
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
