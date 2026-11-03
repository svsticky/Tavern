import { beforeEach, describe, expect, it, vi } from "vitest";
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

const future = new Date(Date.now() + 86_400_000).toISOString();
const past = new Date(Date.now() - 86_400_000).toISOString();

/** Makes every request succeed with an empty list. */
function mockAllEmpty() {
  getActivities.mockResolvedValue({ data: [] });
  getAnnouncements.mockResolvedValue({ data: [] });
  getGroupmemberships.mockResolvedValue({ data: [] });
  getPaymentsUnpaid.mockResolvedValue({ data: [] });
  getEnrollments.mockResolvedValue({ data: [] });
}

describe("loadHomeLoaderData", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockAllEmpty();
  });

  it("requests the upcoming activities and the user's own data", async () => {
    await loadHomeLoaderData("user-1");

    expect(getActivities).toHaveBeenCalledWith({
      query: { IncludePast: false, IncludeFuture: true },
    });
    expect(getActivities).toHaveBeenCalledWith({
      query: { UserId: "user-1", IncludePast: false, IncludeFuture: true },
    });
    expect(getGroupmemberships).toHaveBeenCalledWith({
      query: { MemberId: "user-1" },
    });
    expect(getEnrollments).toHaveBeenCalledWith({
      query: { FromMemberId: "user-1" },
    });
    expect(getPaymentsUnpaid).toHaveBeenCalled();
    expect(getAnnouncements).toHaveBeenCalled();
  });

  it("returns the loaded data", async () => {
    getActivities
      .mockResolvedValueOnce({ data: [{ id: 1 }] })
      .mockResolvedValueOnce({ data: [{ id: 2, dateTimeEnd: future }] });
    getGroupmemberships.mockResolvedValue({ data: [{ id: 4 }] });
    getPaymentsUnpaid.mockResolvedValue({ data: [{ enrollmentId: 5 }] });
    getEnrollments.mockResolvedValue({ data: [{ id: 6 }] });

    await expect(loadHomeLoaderData("user-1")).resolves.toEqual({
      activities: [{ id: 1 }],
      futureEnrollments: [{ id: 2, dateTimeEnd: future }],
      announcements: [],
      groupMemberships: [{ id: 4 }],
      unpaidBalances: [{ enrollmentId: 5 }],
      enrollments: [{ id: 6 }],
    });
  });

  it("leaves out enrolled activities that have already ended", async () => {
    getActivities.mockResolvedValueOnce({ data: [] }).mockResolvedValueOnce({
      data: [
        { id: 1, dateTimeEnd: past },
        { id: 2, dateTimeEnd: future },
      ],
    });

    const data = await loadHomeLoaderData("user-1");

    expect(data.futureEnrollments.map((a) => a.id)).toEqual([2]);
  });

  it("returns only the two latest announcements", async () => {
    getAnnouncements.mockResolvedValue({
      data: [{ id: 1 }, { id: 2 }, { id: 3 }],
    });

    const data = await loadHomeLoaderData("user-1");

    expect(data.announcements).toEqual([{ id: 1 }, { id: 2 }]);
  });

  it.each([
    ["activities", () => getActivities.mockResolvedValue({ error: "fail" })],
    [
      "enrolled activities",
      () =>
        getActivities
          .mockResolvedValueOnce({ data: [] })
          .mockResolvedValueOnce({ error: "fail" }),
    ],
    [
      "announcements",
      () => getAnnouncements.mockResolvedValue({ error: "fail" }),
    ],
    [
      "group memberships",
      () => getGroupmemberships.mockResolvedValue({ error: "fail" }),
    ],
    [
      "unpaid balances",
      () => getPaymentsUnpaid.mockResolvedValue({ error: "fail" }),
    ],
    ["enrollments", () => getEnrollments.mockResolvedValue({ error: "fail" })],
  ])("throws when the %s fail to load", async (name, mockFailure) => {
    mockFailure();

    await expect(loadHomeLoaderData("user-1")).rejects.toThrow(
      `Failed to load ${name}`,
    );
  });
});
