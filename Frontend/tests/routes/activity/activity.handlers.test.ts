import { beforeEach, describe, expect, it, vi } from "vitest";
import type { ActivityResponseDto } from "~/api";
import {
  getActivityBackPath,
  handleDeleteActivity,
  handleEditActivityClick,
  loadActivityData,
} from "~/routes/activity/activity.handlers";

const { getActivitiesById, deleteActivitiesById } = vi.hoisted(() => ({
  getActivitiesById: vi.fn(),
  deleteActivitiesById: vi.fn(),
}));

vi.mock("~/api", () => ({ getActivitiesById, deleteActivitiesById }));

const toastErrorFn = vi.fn();
const toastPromiseFn = vi.fn();
vi.mock("react-hot-toast", () => ({
  default: {
    error: (...args: unknown[]) => toastErrorFn(...args),
    promise: (promise: Promise<unknown>, opts: any) => {
      toastPromiseFn(promise, opts);
      promise.catch(() => {});
      return promise;
    },
  },
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

describe("handleDeleteActivity", () => {
  it("does nothing when confirmation is rejected", async () => {
    const confirm = vi.fn().mockResolvedValue(false);
    const navigate = vi.fn();

    await handleDeleteActivity(5, navigate, "/activities/5", confirm);

    expect(confirm).toHaveBeenCalled();
    expect(deleteActivitiesById).not.toHaveBeenCalled();
    expect(navigate).not.toHaveBeenCalled();
  });

  it("deletes the activity and navigates to the back path on confirmation", async () => {
    const confirm = vi.fn().mockResolvedValue(true);
    const navigate = vi.fn();
    deleteActivitiesById.mockResolvedValue({ error: null });

    await handleDeleteActivity(5, navigate, "/activities/5", confirm);

    expect(confirm).toHaveBeenCalled();
    await vi.waitFor(() =>
      expect(deleteActivitiesById).toHaveBeenCalledWith({ path: { id: 5 } }),
    );
    expect(navigate).toHaveBeenCalledWith("/activities");
    expect(toastPromiseFn).toHaveBeenCalledWith(
      expect.any(Promise),
      expect.objectContaining({
        loading: "deleting",
        success: "activity_deleted_successfully",
        error: expect.any(Function),
      }),
    );
  });

  it("navigates to the admin path when in admin context", async () => {
    const confirm = vi.fn().mockResolvedValue(true);
    const navigate = vi.fn();
    deleteActivitiesById.mockResolvedValue({ error: null });

    await handleDeleteActivity(5, navigate, "/admin/activities/5", confirm);

    await vi.waitFor(() =>
      expect(navigate).toHaveBeenCalledWith("/admin/activities"),
    );
  });

  it("shows an error toast when delete API fails", async () => {
    const confirm = vi.fn().mockResolvedValue(true);
    const navigate = vi.fn();
    deleteActivitiesById.mockResolvedValue({ error: "permission denied" });

    await handleDeleteActivity(5, navigate, "/activities/5", confirm);

    await vi.waitFor(() =>
      expect(deleteActivitiesById).toHaveBeenCalledWith({ path: { id: 5 } }),
    );
    expect(navigate).not.toHaveBeenCalled();
    expect(toastPromiseFn).toHaveBeenCalledWith(
      expect.any(Promise),
      expect.objectContaining({
        loading: "deleting",
        success: "activity_deleted_successfully",
        error: expect.any(Function),
      }),
    );
    const opts = toastPromiseFn.mock.calls[0][1];
    expect(opts.error("permission denied")).toBe(
      "failed_to_delete_activity: permission denied",
    );
  });
});
