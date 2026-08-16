import { beforeEach, describe, expect, it, vi } from "vitest";
import type { ActivityResponseDto } from "~/api";
import {
  getEditActivityBackPath,
  loadEditActivityData,
} from "~/routes/edit-activity/edit-activity.handlers";

const { getActivitiesById } = vi.hoisted(() => ({
  getActivitiesById: vi.fn(),
}));

vi.mock("~/api", () => ({ getActivitiesById }));

const toastErrorFn = vi.fn();
vi.mock("react-hot-toast", () => ({
  default: { error: (...args: unknown[]) => toastErrorFn(...args) },
}));

describe("loadEditActivityData", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("does nothing but stop loading when creating a new activity", async () => {
    const setActivity = vi.fn();
    const setLoading = vi.fn();

    await loadEditActivityData({
      isEdit: false,
      id: undefined,
      setActivity,
      setLoading,
    });

    expect(getActivitiesById).not.toHaveBeenCalled();
    expect(setActivity).not.toHaveBeenCalled();
    expect(setLoading).toHaveBeenCalledWith(false);
  });

  it("fetches and sets the activity when editing", async () => {
    const activity = { id: 5, name: "Party" } as ActivityResponseDto;
    getActivitiesById.mockResolvedValue({ data: activity });
    const setActivity = vi.fn();
    const setLoading = vi.fn();

    await loadEditActivityData({
      isEdit: true,
      id: "5",
      setActivity,
      setLoading,
    });

    expect(getActivitiesById).toHaveBeenCalledWith({ path: { id: 5 } });
    expect(setActivity).toHaveBeenCalledWith(activity);
    expect(setLoading).toHaveBeenCalledWith(false);
  });

  it("logs and shows an error toast when the fetch fails", async () => {
    getActivitiesById.mockResolvedValue({ error: "fail" });
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});
    const setActivity = vi.fn();

    await loadEditActivityData({
      isEdit: true,
      id: "5",
      setActivity,
      setLoading: vi.fn(),
    });

    expect(setActivity).not.toHaveBeenCalled();
    expect(consoleError).toHaveBeenCalled();
    expect(toastErrorFn).toHaveBeenCalled();
    consoleError.mockRestore();
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
