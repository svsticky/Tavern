import { describe, expect, it, vi } from "vitest";
import { loadHomeLoaderData } from "~/routes/home/home.handlers";

const {
  getActivities,
  getAnnouncements,
  getGroupmemberships,
  getPaymentsUnpaid,
  getEnrollments,
} = vi.hoisted(() => ({
  getActivities: vi.fn(),
  getAnnouncements: vi.fn(),
  getGroupmemberships: vi.fn(),
  getPaymentsUnpaid: vi.fn(),
  getEnrollments: vi.fn(),
}));

vi.mock("~/api", () => ({
  getActivities,
  getAnnouncements,
  getGroupmemberships,
  getPaymentsUnpaid,
  getEnrollments,
}));

function mockAllSucceed() {
  getActivities
    .mockResolvedValueOnce({ data: [{ id: 1 }] })
    .mockResolvedValueOnce({ data: [{ id: 2 }] });
  getAnnouncements.mockResolvedValue({ data: [{ id: 3 }] });
  getGroupmemberships.mockResolvedValue({ data: [{ id: 4 }] });
  getPaymentsUnpaid.mockResolvedValue({ data: [] });
  getEnrollments.mockResolvedValue({ data: [] });
}

describe("loadHomeLoaderData", () => {
  it("loads activities, enrolled activities, announcements, group memberships, payments and enrollment counts on success", async () => {
    mockAllSucceed();

    const result = await loadHomeLoaderData("user-1");

    expect(result).toEqual({
      activities: [{ id: 1 }],
      enrolledActivities: [{ id: 2 }],
      announcements: [{ id: 3 }],
      groupMemberships: [{ id: 4 }],
      outstandingPayments: 0,
      unpaidActivityIds: [],
      pastEnrollmentAmount: 0,
      comingEnrollmentAmount: 0,
    });
    expect(getActivities).toHaveBeenNthCalledWith(1, {
      query: { IncludePast: false, IncludeFuture: true },
    });
    expect(getActivities).toHaveBeenNthCalledWith(2, {
      query: { UserId: "user-1", IncludePast: false, IncludeFuture: true },
    });
    expect(getEnrollments).toHaveBeenCalledWith({
      query: { FromMemberId: "user-1" },
    });
  });

  it("computes outstanding payments and past/coming enrollment counts", async () => {
    mockAllSucceed();
    getPaymentsUnpaid.mockResolvedValue({
      data: [
        { balance: 5, enrollment: { activityId: 1 } },
        { balance: 2.5, enrollment: { activityId: 2 } },
      ],
    });
    getEnrollments.mockResolvedValue({
      data: [
        { activity: { dateTimeEnd: "2020-01-01T00:00:00Z" } },
        { activity: { dateTimeEnd: "2099-01-01T00:00:00Z" } },
        { activity: { dateTimeEnd: "2099-06-01T00:00:00Z" } },
      ],
    });

    const result = await loadHomeLoaderData("user-1");

    expect(result.outstandingPayments).toBe(7.5);
    expect(result.unpaidActivityIds).toEqual([1, 2]);
    expect(result.pastEnrollmentAmount).toBe(1);
    expect(result.comingEnrollmentAmount).toBe(2);
  });

  it("throws when activities fail to load", async () => {
    mockAllSucceed();
    getActivities.mockReset().mockResolvedValue({ error: "fail" });

    await expect(loadHomeLoaderData("user-1")).rejects.toThrow(
      "Failed to load activities",
    );
  });

  it("throws when enrolled activities fail to load", async () => {
    mockAllSucceed();
    getActivities
      .mockReset()
      .mockResolvedValueOnce({ data: [] })
      .mockResolvedValueOnce({ error: "fail" });

    await expect(loadHomeLoaderData("user-1")).rejects.toThrow(
      "Failed to load enrolled activities",
    );
  });

  it("throws when announcements fail to load", async () => {
    mockAllSucceed();
    getAnnouncements.mockResolvedValue({ error: "fail" });

    await expect(loadHomeLoaderData("user-1")).rejects.toThrow(
      "Failed to load announcements",
    );
  });

  it("throws when group memberships fail to load", async () => {
    mockAllSucceed();
    getGroupmemberships.mockResolvedValue({ error: "fail" });

    await expect(loadHomeLoaderData("user-1")).rejects.toThrow(
      "Failed to load group memberships",
    );
  });

  it("throws when outstanding payments fail to load", async () => {
    mockAllSucceed();
    getPaymentsUnpaid.mockResolvedValue({ error: "fail" });

    await expect(loadHomeLoaderData("user-1")).rejects.toThrow(
      "Failed to load outstanding payments",
    );
  });

  it("throws when enrollments fail to load", async () => {
    mockAllSucceed();
    getEnrollments.mockResolvedValue({ error: "fail" });

    await expect(loadHomeLoaderData("user-1")).rejects.toThrow(
      "Failed to load enrollments",
    );
  });
});
