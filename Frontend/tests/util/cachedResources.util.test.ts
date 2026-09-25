import { beforeEach, describe, expect, it, vi } from "vitest";

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

import {
  loadAnnouncements,
  loadEnrolledActivities,
  loadGroupMemberships,
  loadUpcomingActivities,
} from "~/util/cachedResources.util";
import { invalidateCacheForMutation } from "~/util/resourceCache.util";

describe("cached resource loaders", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    getActivities.mockResolvedValue({ data: [{ id: 1 }] });
    getAnnouncements.mockResolvedValue({ data: [{ id: 9 }] });
    getGroupmemberships.mockResolvedValue({ data: [{ id: 5 }] });
  });

  it("shares one upcoming-activities fetch between the pages that show it", async () => {
    await loadUpcomingActivities(); // e.g. the home dashboard's loader
    await loadUpcomingActivities(); // e.g. /activities right after

    expect(getActivities).toHaveBeenCalledTimes(1);
  });

  it("refetches upcoming activities after the user creates, edits or deletes one", async () => {
    await loadUpcomingActivities();

    invalidateCacheForMutation("post", "/activities");
    await loadUpcomingActivities();

    expect(getActivities).toHaveBeenCalledTimes(2);
  });

  it("refetches upcoming activities after someone enrolls, since the participant counts changed", async () => {
    await loadUpcomingActivities();

    invalidateCacheForMutation("post", "/enrollments");
    await loadUpcomingActivities();

    expect(getActivities).toHaveBeenCalledTimes(2);
  });

  it("keeps enrolled activities per user", async () => {
    await loadEnrolledActivities("user-1");
    await loadEnrolledActivities("user-2");
    await loadEnrolledActivities("user-1");

    expect(getActivities).toHaveBeenCalledTimes(2);
    expect(getActivities).toHaveBeenCalledWith({
      query: { UserId: "user-1", IncludePast: false, IncludeFuture: true },
    });
  });

  it("refetches every user's enrolled activities after an enrollment change", async () => {
    await loadEnrolledActivities("user-1");
    await loadEnrolledActivities("user-2");

    invalidateCacheForMutation("delete", "/enrollments/42/abc");
    await loadEnrolledActivities("user-1");
    await loadEnrolledActivities("user-2");

    expect(getActivities).toHaveBeenCalledTimes(4);
  });

  it("shares announcements and refetches them after a change", async () => {
    await loadAnnouncements();
    await loadAnnouncements();
    expect(getAnnouncements).toHaveBeenCalledTimes(1);

    invalidateCacheForMutation("put", "/announcements/9");
    await loadAnnouncements();
    expect(getAnnouncements).toHaveBeenCalledTimes(2);
  });

  it("refetches group memberships after a membership is added, changed or removed", async () => {
    await loadGroupMemberships("user-1");
    await loadGroupMemberships("user-1");
    expect(getGroupmemberships).toHaveBeenCalledTimes(1);

    invalidateCacheForMutation("post", "/groupmemberships");
    await loadGroupMemberships("user-1");
    expect(getGroupmemberships).toHaveBeenCalledTimes(2);
  });

  it("throws on a failed response instead of caching or returning nothing", async () => {
    getActivities.mockResolvedValue({ error: {}, data: undefined });

    await expect(loadUpcomingActivities()).rejects.toThrow(
      "Failed to load activities",
    );

    getActivities.mockResolvedValue({ data: [{ id: 1 }] });
    await expect(loadUpcomingActivities()).resolves.toEqual([{ id: 1 }]);
  });
});
