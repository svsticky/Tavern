import type { NavigateFunction } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { ActivityResponseDto } from "~/api";

const { getActivities, deleteActivitiesById } = vi.hoisted(() => ({
  getActivities: vi.fn(),
  deleteActivitiesById: vi.fn(),
}));

vi.mock("~/api", () => ({ getActivities, deleteActivitiesById }));

vi.mock("react-hot-toast", () => ({
  default: {
    success: vi.fn(),
    error: vi.fn(),
    promise: vi.fn((promise: Promise<unknown>, opts: any) => {
      promise
        .then(
          (data) => opts?.success?.(data),
          (err) => opts?.error?.(err),
        )
        .catch(() => {});
      return promise;
    }),
  },
}));

import toast from "react-hot-toast";
import {
  handleDeleteAdminActivity,
  handleViewActivity,
  loadAdminActivities,
} from "~/routes/admin/activities/activities.handlers";

describe("loadAdminActivities", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("fetches activities for the given year/page and updates state", async () => {
    const setLoading = vi.fn();
    const setActivities = vi.fn();
    const activities: ActivityResponseDto[] = [
      { id: 1, name: "Feest" } as ActivityResponseDto,
    ];
    getActivities.mockResolvedValue({ data: activities });

    await loadAdminActivities(2024, setLoading, setActivities, 2, 15);

    expect(getActivities).toHaveBeenCalledWith({
      query: {
        IncludePast: true,
        IncludeFuture: true,
        Year: 2024,
        Page: 2,
        PageSize: 15,
        IsArchived: false,
      },
    });
    expect(setLoading).toHaveBeenNthCalledWith(1, true);
    expect(setActivities).toHaveBeenCalledWith(activities);
    expect(setLoading).toHaveBeenNthCalledWith(2, false);
  });

  it("fetches archived activities when isArchived is set to true", async () => {
    const setLoading = vi.fn();
    const setActivities = vi.fn();
    const activities: ActivityResponseDto[] = [
      {
        id: 2,
        name: "Archived Event",
        isArchived: true,
      } as ActivityResponseDto,
    ];
    getActivities.mockResolvedValue({ data: activities });

    await loadAdminActivities(
      2024,
      setLoading,
      setActivities,
      1,
      15,
      true,
      true,
    );

    expect(getActivities).toHaveBeenCalledWith({
      query: {
        IncludePast: true,
        IncludeFuture: true,
        Year: 2024,
        Page: 1,
        PageSize: 15,
        IsArchived: true,
      },
    });
    expect(setActivities).toHaveBeenCalledWith(activities);
  });

  it("shows an error toast and does not set activities when the response has an error", async () => {
    const setLoading = vi.fn();
    const setActivities = vi.fn();
    getActivities.mockResolvedValue({ error: "bad", data: null });

    await loadAdminActivities(2024, setLoading, setActivities);

    expect(setActivities).not.toHaveBeenCalled();
    // response.error is truthy ("bad"), so the handler throws that directly rather than
    // falling back to the generic "Failed to load activities" message.
    expect(toast.error).toHaveBeenCalledWith("loading_failed: bad");
    expect(setLoading).toHaveBeenLastCalledWith(false);
  });

  it("shows an error toast and logs when the response has no data", async () => {
    const setLoading = vi.fn();
    const setActivities = vi.fn();
    getActivities.mockResolvedValue({ error: null, data: null });

    await loadAdminActivities(2024, setLoading, setActivities);

    expect(setActivities).not.toHaveBeenCalled();
    expect(toast.error).toHaveBeenCalledWith(
      "loading_failed: Failed to load activities",
    );
  });

  it("catches thrown/rejected errors from the API call", async () => {
    const setLoading = vi.fn();
    const setActivities = vi.fn();
    getActivities.mockRejectedValue(new Error("network down"));

    await loadAdminActivities(2024, setLoading, setActivities);

    expect(toast.error).toHaveBeenCalledWith("loading_failed: network down");
    expect(setLoading).toHaveBeenLastCalledWith(false);
  });
});

describe("handleViewActivity", () => {
  it("navigates to the admin activity detail route", () => {
    const navigate = vi.fn() as unknown as NavigateFunction;
    handleViewActivity(navigate, 42);
    expect(navigate).toHaveBeenCalledWith("/admin/activities/42");
  });
});

describe("handleDeleteAdminActivity", () => {
  it("does nothing when confirmation is rejected", async () => {
    const confirm = vi.fn().mockResolvedValue(false);
    const onSuccess = vi.fn();

    await handleDeleteAdminActivity(42, confirm, onSuccess);

    expect(confirm).toHaveBeenCalled();
    expect(deleteActivitiesById).not.toHaveBeenCalled();
    expect(onSuccess).not.toHaveBeenCalled();
  });

  it("calls delete endpoint and invokes onSuccess when confirmed", async () => {
    const confirm = vi.fn().mockResolvedValue(true);
    const onSuccess = vi.fn();
    deleteActivitiesById.mockResolvedValue({ error: null });

    await handleDeleteAdminActivity(42, confirm, onSuccess);

    expect(confirm).toHaveBeenCalled();
    await vi.waitFor(() =>
      expect(deleteActivitiesById).toHaveBeenCalledWith({ path: { id: 42 } }),
    );
    expect(onSuccess).toHaveBeenCalled();
  });

  it("does not invoke onSuccess when delete endpoint returns error", async () => {
    const confirm = vi.fn().mockResolvedValue(true);
    const onSuccess = vi.fn();
    deleteActivitiesById.mockResolvedValue({ error: "Failed to delete" });

    await handleDeleteAdminActivity(42, confirm, onSuccess);

    await vi.waitFor(() =>
      expect(deleteActivitiesById).toHaveBeenCalledWith({ path: { id: 42 } }),
    );
    expect(onSuccess).not.toHaveBeenCalled();
  });
});
