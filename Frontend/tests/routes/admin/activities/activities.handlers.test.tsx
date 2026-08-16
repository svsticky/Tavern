import type { NavigateFunction } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { ActivityResponseDto } from "~/api";

const { getActivities } = vi.hoisted(() => ({
  getActivities: vi.fn(),
}));

vi.mock("~/api", () => ({ getActivities }));

vi.mock("react-hot-toast", () => ({
  default: { success: vi.fn(), error: vi.fn(), promise: vi.fn((p) => p) },
}));

import toast from "react-hot-toast";
import {
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
      },
    });
    expect(setLoading).toHaveBeenNthCalledWith(1, true);
    expect(setActivities).toHaveBeenCalledWith(activities);
    expect(setLoading).toHaveBeenNthCalledWith(2, false);
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
