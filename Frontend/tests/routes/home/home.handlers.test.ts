import { describe, expect, it, vi } from "vitest";
import { loadHomeLoaderData } from "~/routes/home/home.handlers";

const { getActivities, getAnnouncements, getGroupmemberships } = vi.hoisted(
  () => ({
    getActivities: vi.fn(),
    getAnnouncements: vi.fn(),
    getGroupmemberships: vi.fn(),
  }),
);

vi.mock("~/api", () => ({
  getActivities,
  getAnnouncements,
  getGroupmemberships,
}));

describe("loadHomeLoaderData", () => {
  it("loads activities, enrolled activities, announcements and group memberships on success", async () => {
    getActivities
      .mockResolvedValueOnce({ data: [{ id: 1 }] })
      .mockResolvedValueOnce({ data: [{ id: 2 }] });
    getAnnouncements.mockResolvedValue({ data: [{ id: 3 }] });
    getGroupmemberships.mockResolvedValue({ data: [{ id: 4 }] });

    const result = await loadHomeLoaderData("user-1");

    expect(result).toEqual({
      activities: [{ id: 1 }],
      enrolledActivities: [{ id: 2 }],
      announcements: [{ id: 3 }],
      groupMemberships: [{ id: 4 }],
    });
    expect(getActivities).toHaveBeenNthCalledWith(1, {
      query: { IncludePast: false, IncludeFuture: true },
    });
    expect(getActivities).toHaveBeenNthCalledWith(2, {
      query: { UserId: "user-1", IncludePast: false, IncludeFuture: true },
    });
  });

  it("throws when activities fail to load", async () => {
    getActivities.mockResolvedValue({ error: "fail" });
    getAnnouncements.mockResolvedValue({ data: [] });
    getGroupmemberships.mockResolvedValue({ data: [] });

    await expect(loadHomeLoaderData("user-1")).rejects.toThrow(
      "Failed to load activities",
    );
  });

  it("throws when enrolled activities fail to load", async () => {
    getActivities
      .mockResolvedValueOnce({ data: [] })
      .mockResolvedValueOnce({ error: "fail" });
    getAnnouncements.mockResolvedValue({ data: [] });
    getGroupmemberships.mockResolvedValue({ data: [] });

    await expect(loadHomeLoaderData("user-1")).rejects.toThrow(
      "Failed to load enrolled activities",
    );
  });

  it("throws when announcements fail to load", async () => {
    getActivities.mockResolvedValue({ data: [] });
    getAnnouncements.mockResolvedValue({ error: "fail" });
    getGroupmemberships.mockResolvedValue({ data: [] });

    await expect(loadHomeLoaderData("user-1")).rejects.toThrow(
      "Failed to load announcements",
    );
  });

  it("throws when group memberships fail to load", async () => {
    getActivities.mockResolvedValue({ data: [] });
    getAnnouncements.mockResolvedValue({ data: [] });
    getGroupmemberships.mockResolvedValue({ error: "fail" });

    await expect(loadHomeLoaderData("user-1")).rejects.toThrow(
      "Failed to load group memberships",
    );
  });
});
