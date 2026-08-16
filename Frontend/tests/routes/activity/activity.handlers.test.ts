import { beforeEach, describe, expect, it, vi } from "vitest";
import type { ActivityResponseDto } from "~/api";
import {
  getActivityBackPath,
  handleEditActivityClick,
  loadActivityData,
} from "~/routes/activity/activity.handlers";

const { getActivitiesById } = vi.hoisted(() => ({
  getActivitiesById: vi.fn(),
}));

vi.mock("~/api", () => ({ getActivitiesById }));

const toastErrorFn = vi.fn();
vi.mock("react-hot-toast", () => ({
  default: { error: (...args: unknown[]) => toastErrorFn(...args) },
}));

describe("loadActivityData", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("sets the activity on success", async () => {
    const activity = { id: 1, name: "Party" } as ActivityResponseDto;
    getActivitiesById.mockResolvedValue({ data: activity });
    const setActivity = vi.fn();
    const setLoading = vi.fn();

    await loadActivityData({ activityId: 1, setLoading, setActivity });

    expect(getActivitiesById).toHaveBeenCalledWith({ path: { id: 1 } });
    expect(setActivity).toHaveBeenCalledWith(activity);
    expect(setLoading).toHaveBeenNthCalledWith(1, true);
    expect(setLoading).toHaveBeenNthCalledWith(2, false);
  });

  it("logs and shows an error toast on failure", async () => {
    getActivitiesById.mockResolvedValue({ error: "fail" });
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});
    const setActivity = vi.fn();

    await loadActivityData({
      activityId: 1,
      setLoading: vi.fn(),
      setActivity,
    });

    expect(setActivity).not.toHaveBeenCalled();
    expect(consoleError).toHaveBeenCalled();
    expect(toastErrorFn).toHaveBeenCalled();
    consoleError.mockRestore();
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
