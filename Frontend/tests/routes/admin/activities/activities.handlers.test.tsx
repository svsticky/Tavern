import type { NavigateFunction } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { ActivityResponseDto } from "~/api";
import {
  fetchAdminActivities,
  handleViewActivity,
} from "~/routes/admin/activities/activities.handlers";

const { getActivities } = vi.hoisted(() => ({
  getActivities: vi.fn(),
}));

vi.mock("~/api", () => ({ getActivities }));

describe("fetchAdminActivities", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("fetches past and future activities of the year for the given page", async () => {
    const activities = [{ id: 1, name: "Feest" }] as ActivityResponseDto[];
    getActivities.mockResolvedValue({ data: activities });

    await expect(
      fetchAdminActivities({ year: 2024, search: "", page: 2, pageSize: 15 }),
    ).resolves.toEqual(activities);
    expect(getActivities).toHaveBeenCalledWith({
      query: {
        IncludePast: true,
        IncludeFuture: true,
        Year: 2024,
        Page: 2,
        PageSize: 15,
        Search: undefined,
      },
    });
  });

  it("passes the search term", async () => {
    getActivities.mockResolvedValue({ data: [] });

    await fetchAdminActivities({
      year: 2024,
      search: "borrel",
      page: 1,
      pageSize: 15,
    });

    expect(getActivities).toHaveBeenCalledWith({
      query: expect.objectContaining({ Search: "borrel" }),
    });
  });

  it("throws the API error", async () => {
    getActivities.mockResolvedValue({ error: "bad", data: null });

    await expect(
      fetchAdminActivities({ year: 2024, search: "", page: 1, pageSize: 15 }),
    ).rejects.toBe("bad");
  });

  it("throws when the response has no data", async () => {
    getActivities.mockResolvedValue({ error: null, data: null });

    await expect(
      fetchAdminActivities({ year: 2024, search: "", page: 1, pageSize: 15 }),
    ).rejects.toThrow("Failed to load activities");
  });
});

describe("handleViewActivity", () => {
  it("navigates to the admin activity detail route", () => {
    const navigate = vi.fn() as unknown as NavigateFunction;
    handleViewActivity(navigate, 42);
    expect(navigate).toHaveBeenCalledWith("/admin/activities/42");
  });
});
