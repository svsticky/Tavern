import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { EnrollmentBalance, Member } from "~/api";

const {
  getActivities,
  getPaymentsExport,
  getPaymentsOverpaid,
  getPaymentsUnpaid,
  postPaymentsActivity,
} = vi.hoisted(() => ({
  getActivities: vi.fn(),
  getPaymentsExport: vi.fn(),
  getPaymentsOverpaid: vi.fn(),
  getPaymentsUnpaid: vi.fn(),
  postPaymentsActivity: vi.fn(),
}));

vi.mock("~/api", () => ({
  getActivities,
  getPaymentsExport,
  getPaymentsOverpaid,
  getPaymentsUnpaid,
  postPaymentsActivity,
}));

vi.mock("react-hot-toast", () => ({
  default: {
    success: vi.fn(),
    error: vi.fn(),
    promise: vi.fn((p) => p.catch(() => {})),
  },
}));

import toast from "react-hot-toast";
import {
  handleMarkAsPaid,
  handlePaymentsExport,
  handleWhatsAppClick,
  loadFinancesData,
  refreshUnpaidPayments,
  setUnpaidPaymentState,
} from "~/routes/admin/finances/finances.handlers";

function balance(
  overrides: Partial<EnrollmentBalance> = {},
): EnrollmentBalance {
  return {
    balance: 10,
    enrollment: {
      activityId: 1,
      activity: {
        id: 1,
        name: "Feest",
        paymentDeadline: "2020-01-01T00:00:00Z",
      },
      member: { id: "m1", firstName: "Jane", lastName: "Doe" },
    },
    ...overrides,
  } as EnrollmentBalance;
}

describe("setUnpaidPaymentState", () => {
  it("filters zero-balance entries, sums totals, and groups by activity/member", () => {
    const balances = [
      balance({ balance: 10 }),
      balance({ balance: 0 }),
      balance({
        balance: 5,
        enrollment: {
          activityId: 2,
          activity: {
            id: 2,
            name: "Borrel",
            paymentDeadline: "2020-02-01T00:00:00Z",
          },
          member: { id: "m2", firstName: "John", lastName: "Smith" },
        },
      } as EnrollmentBalance),
    ];

    const setUnpaidBalances = vi.fn();
    const setTotalUnpaid = vi.fn();
    const setOpenPayments = vi.fn();
    const setUnpaidActivities = vi.fn();
    const setMembersWithOverduePayment = vi.fn();

    setUnpaidPaymentState({
      balances,
      setUnpaidBalances,
      setTotalUnpaid,
      setOpenPayments,
      setUnpaidActivities,
      setMembersWithOverduePayment,
    });

    expect(setUnpaidBalances).toHaveBeenCalledWith([balances[0], balances[2]]);
    expect(setTotalUnpaid).toHaveBeenCalledWith(15);
    expect(setOpenPayments).toHaveBeenCalledWith(3);
    expect(setUnpaidActivities).toHaveBeenCalledWith([
      { id: 1, name: "Feest", paymentDeadline: "2020-01-01T00:00:00Z" },
      { id: 2, name: "Borrel", paymentDeadline: "2020-02-01T00:00:00Z" },
    ]);
    // Note: setMembersWithOverduePayment groups from the *unfiltered* balances array, so the
    // zero-balance entry for member m1 is still included in their enrollments list.
    expect(setMembersWithOverduePayment).toHaveBeenCalledWith([
      {
        member: balances[0].enrollment.member,
        enrollments: [balances[0], balances[1]],
      },
      { member: balances[2].enrollment.member, enrollments: [balances[2]] },
    ]);
  });

  it("does not duplicate activities shared by multiple balances", () => {
    const balances = [balance({ balance: 10 }), balance({ balance: 20 })];

    const setUnpaidActivities = vi.fn();
    setUnpaidPaymentState({
      balances,
      setUnpaidBalances: vi.fn(),
      setTotalUnpaid: vi.fn(),
      setOpenPayments: vi.fn(),
      setUnpaidActivities,
      setMembersWithOverduePayment: vi.fn(),
    });

    expect(setUnpaidActivities).toHaveBeenCalledWith([
      { id: 1, name: "Feest", paymentDeadline: "2020-01-01T00:00:00Z" },
    ]);
  });
});

describe("refreshUnpaidPayments", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("fetches unpaid balances and updates state", async () => {
    getPaymentsUnpaid.mockResolvedValue({ data: [balance()] });
    const setUnpaidBalances = vi.fn();

    await refreshUnpaidPayments({
      setUnpaidBalances,
      setTotalUnpaid: vi.fn(),
      setOpenPayments: vi.fn(),
      setUnpaidActivities: vi.fn(),
      setMembersWithOverduePayment: vi.fn(),
    });

    expect(getPaymentsUnpaid).toHaveBeenCalledWith({
      query: { allUsers: true },
    });
    expect(setUnpaidBalances).toHaveBeenCalled();
  });

  it("does not update state when there is no data", async () => {
    getPaymentsUnpaid.mockResolvedValue({ data: null });
    const setUnpaidBalances = vi.fn();

    await refreshUnpaidPayments({
      setUnpaidBalances,
      setTotalUnpaid: vi.fn(),
      setOpenPayments: vi.fn(),
      setUnpaidActivities: vi.fn(),
      setMembersWithOverduePayment: vi.fn(),
    });

    expect(setUnpaidBalances).not.toHaveBeenCalled();
  });
});

describe("handleWhatsAppClick", () => {
  let openSpy: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    openSpy = vi.fn();
    vi.stubGlobal("open", openSpy);
    vi.useFakeTimers();
    vi.setSystemTime(new Date("2024-06-01T00:00:00Z"));
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  const member: Member = {
    id: "m1",
    firstName: "Jane",
    phoneNumber: "0612345678",
    preferredLanguage: "NL",
  } as Member;

  it("opens a WhatsApp link with a Dutch message using the international phone format", () => {
    const enrollments = [
      balance({
        balance: 15,
        enrollment: {
          activityId: 1,
          activity: {
            id: 1,
            name: "Feest",
            paymentDeadline: "2024-05-01T00:00:00Z",
          },
          member,
        },
      } as EnrollmentBalance),
    ];

    handleWhatsAppClick({ member, enrollments });

    expect(openSpy).toHaveBeenCalledWith(
      expect.stringContaining("https://wa.me/31612345678?text="),
      "_blank",
    );
    const url = openSpy.mock.calls[0][0] as string;
    expect(decodeURIComponent(url)).toContain("Sticky");
    expect(decodeURIComponent(url)).toContain("€15.00");
  });

  it("uses an English message when the member's preferred language is not NL", () => {
    const enMember = { ...member, preferredLanguage: "EN" } as Member;
    const enrollments = [
      balance({
        balance: 15,
        enrollment: {
          activityId: 1,
          activity: {
            id: 1,
            name: "Feest",
            paymentDeadline: "2024-05-01T00:00:00Z",
          },
          member: enMember,
        },
      } as EnrollmentBalance),
    ];

    handleWhatsAppClick({ member: enMember, enrollments });

    const url = openSpy.mock.calls[0][0] as string;
    expect(decodeURIComponent(url)).toContain("treasurer");
  });

  it("filters out enrollments that are not yet past their payment deadline", () => {
    const enrollments = [
      balance({
        balance: 15,
        enrollment: {
          activityId: 1,
          activity: {
            id: 1,
            name: "Future",
            paymentDeadline: "2099-01-01T00:00:00Z",
          },
          member,
        },
      } as EnrollmentBalance),
    ];

    handleWhatsAppClick({ member, enrollments });

    const url = openSpy.mock.calls[0][0] as string;
    expect(decodeURIComponent(url)).toContain("€0.00");
  });
});

describe("handleMarkAsPaid", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  const member: Member = { id: "m1" } as Member;

  it("marks enrollments as paid and refreshes", async () => {
    postPaymentsActivity.mockResolvedValue({});
    const refreshUnpaid = vi.fn().mockResolvedValue(undefined);
    const setLoading = vi.fn();

    handleMarkAsPaid({
      member,
      enrollments: [balance({ enrollment: { activityId: 5 } as any })],
      setLoading,
      refreshUnpaid,
    });

    await vi.waitFor(() => expect(refreshUnpaid).toHaveBeenCalled());
    expect(postPaymentsActivity).toHaveBeenCalledWith({
      body: {
        memberId: "m1",
        activityIds: [5],
        manuallyMarkedAsPaid: true,
      },
    });
    await vi.waitFor(() => expect(setLoading).toHaveBeenLastCalledWith(false));
  });

  it("throws when marking as paid fails", async () => {
    postPaymentsActivity.mockResolvedValue({ error: true, message: "bad" });
    const refreshUnpaid = vi.fn();
    const setLoading = vi.fn();

    handleMarkAsPaid({
      member,
      enrollments: [balance()],
      setLoading,
      refreshUnpaid,
    });

    await vi.waitFor(() => expect(setLoading).toHaveBeenLastCalledWith(false));
    expect(refreshUnpaid).not.toHaveBeenCalled();
  });
});

describe("handlePaymentsExport", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.stubGlobal("URL", {
      ...URL,
      createObjectURL: vi.fn(() => "blob:mock-url"),
      revokeObjectURL: vi.fn(),
    });
  });

  it("downloads a CSV export by creating and clicking a link", async () => {
    getPaymentsExport.mockResolvedValue({ data: new Blob(["a,b"]) });
    const setExporting = vi.fn();
    const clickSpy = vi
      .spyOn(HTMLAnchorElement.prototype, "click")
      .mockImplementation(() => {});

    handlePaymentsExport("2024-01-01", "2024-01-31", setExporting);

    await vi.waitFor(() => expect(clickSpy).toHaveBeenCalled());
    expect(getPaymentsExport).toHaveBeenCalledWith({
      query: { startDate: "2024-01-01", endDate: "2024-01-31" },
      responseType: "blob",
    });
    await vi.waitFor(() =>
      expect(setExporting).toHaveBeenLastCalledWith(false),
    );
    clickSpy.mockRestore();
  });

  it("throws when the export request fails", async () => {
    getPaymentsExport.mockResolvedValue({ error: true, message: "bad" });
    const setExporting = vi.fn();

    handlePaymentsExport("2024-01-01", "2024-01-31", setExporting);

    await vi.waitFor(() =>
      expect(setExporting).toHaveBeenLastCalledWith(false),
    );
  });
});

describe("loadFinancesData", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("loads expired activities, unpaid balances, and overpaid balances", async () => {
    getActivities.mockResolvedValue({ data: [{ id: 1, name: "Old" }] });
    getPaymentsUnpaid.mockResolvedValue({ data: [balance()] });
    getPaymentsOverpaid.mockResolvedValue({
      data: [balance({ balance: -5 }), balance({ balance: 0 })],
    });

    const setExpiredActivities = vi.fn();
    const setOverpaidBalances = vi.fn();
    const setLoading = vi.fn();

    await loadFinancesData({
      setLoading,
      setExpiredActivities,
      setUnpaidBalances: vi.fn(),
      setTotalUnpaid: vi.fn(),
      setOpenPayments: vi.fn(),
      setUnpaidActivities: vi.fn(),
      setMembersWithOverduePayment: vi.fn(),
      setOverpaidBalances,
    });

    expect(setExpiredActivities).toHaveBeenCalledWith([{ id: 1, name: "Old" }]);
    expect(setOverpaidBalances).toHaveBeenCalledWith([
      expect.objectContaining({ balance: -5 }),
    ]);
    expect(setLoading).toHaveBeenLastCalledWith(false);
  });

  it("shows an error toast when expired activities fail to load", async () => {
    getActivities.mockResolvedValue({ error: "bad", data: null });
    getPaymentsUnpaid.mockResolvedValue({ data: [] });
    getPaymentsOverpaid.mockResolvedValue({ data: [] });

    const setLoading = vi.fn();

    await loadFinancesData({
      setLoading,
      setExpiredActivities: vi.fn(),
      setUnpaidBalances: vi.fn(),
      setTotalUnpaid: vi.fn(),
      setOpenPayments: vi.fn(),
      setUnpaidActivities: vi.fn(),
      setMembersWithOverduePayment: vi.fn(),
      setOverpaidBalances: vi.fn(),
    });

    expect(toast.error).toHaveBeenCalledWith(
      "loading_failed: Failed to load expired activities",
    );
    expect(setLoading).toHaveBeenLastCalledWith(false);
  });
});
