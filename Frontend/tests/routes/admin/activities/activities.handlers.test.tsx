import type { NavigateFunction } from "react-router";
import { describe, expect, it, vi } from "vitest";
import type { ActivityResponseDto } from "~/api";

const { getActivities } = vi.hoisted(() => ({
  getActivities: vi.fn(),
}));

vi.mock("~/api", () => ({ getActivities }));

import {
  fetchAdminActivitiesPage,
  handleViewActivity,
} from "~/routes/admin/activities/activities.handlers";

describe("fetchAdminActivitiesPage", () => {
  it("fetches activities for the given year/page/search", async () => {
    const activities: ActivityResponseDto[] = [
      { id: 1, name: "Feest" } as ActivityResponseDto,
    ];
    getActivities.mockResolvedValue({ data: activities });

    const result = await fetchAdminActivitiesPage(2024, 2, 15, "feest");

    expect(getActivities).toHaveBeenCalledWith({
      query: {
        IncludePast: true,
        IncludeFuture: true,
        Year: 2024,
        Page: 2,
        PageSize: 15,
        Search: "feest",
      },
    });
    expect(result).toEqual(activities);
  });

  it("omits Search when empty", async () => {
    getActivities.mockResolvedValue({ data: [] });

    await fetchAdminActivitiesPage(2024, 1, 15, "");

    expect(getActivities).toHaveBeenCalledWith({
      query: {
        IncludePast: true,
        IncludeFuture: true,
        Year: 2024,
        Page: 1,
        PageSize: 15,
        Search: undefined,
      },
    });
  });

  it("throws the response error when present", async () => {
    getActivities.mockResolvedValue({ error: "bad", data: null });

    await expect(fetchAdminActivitiesPage(2024, 1, 15)).rejects.toBe("bad");
  });

  it("throws a generic error when there is no data and no error", async () => {
    getActivities.mockResolvedValue({ error: null, data: null });

    await expect(fetchAdminActivitiesPage(2024, 1, 15)).rejects.toThrow(
      "Failed to load activities",
    );
  });
});

describe("handleViewActivity", () => {
  it("navigates to the admin activity detail route", () => {
    const navigate = vi.fn() as unknown as NavigateFunction;
    handleViewActivity(navigate, 42);
    expect(navigate).toHaveBeenCalledWith("/admin/activities/42");
  });
});
