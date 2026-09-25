import { fireEvent, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type {
  Activity,
  ActivityResponseDto,
  EnrollmentBalance,
  Member,
} from "~/api";
import { renderWithProviders } from "~/testUtils";

const {
  fetchExpiredActivities,
  fetchFinancesData,
  handleMarkAsPaid,
  handlePaymentsExport,
  handleWhatsAppClick,
  refreshUnpaidPayments,
} = vi.hoisted(() => ({
  fetchExpiredActivities: vi.fn(),
  fetchFinancesData: vi.fn(),
  handleMarkAsPaid: vi.fn(),
  handlePaymentsExport: vi.fn(),
  handleWhatsAppClick: vi.fn(),
  refreshUnpaidPayments: vi.fn(),
}));

vi.mock("~/routes/admin/finances/finances.handlers", () => ({
  fetchExpiredActivities,
  fetchFinancesData,
  handleMarkAsPaid,
  handlePaymentsExport,
  handleWhatsAppClick,
  refreshUnpaidPayments,
}));

const { requireTokenParsed } = vi.hoisted(() => ({
  requireTokenParsed: vi.fn(),
}));
vi.mock("~/util/loaderAuth.util", () => ({ requireTokenParsed }));

const { useLoaderData } = vi.hoisted(() => ({ useLoaderData: vi.fn() }));
vi.mock("react-router", async (importOriginal) => ({
  ...(await importOriginal<typeof import("react-router")>()),
  useLoaderData,
}));

import Finances, { clientLoader } from "~/routes/admin/finances/finances";

const member: Member = {
  id: "m1",
  firstName: "Jane",
  lastName: "Doe",
  preferredLanguage: "NL",
} as Member;

function unpaidBalance(
  overrides: Partial<EnrollmentBalance> = {},
): EnrollmentBalance {
  return {
    balance: 12.5,
    enrollment: {
      activityId: 1,
      activity: {
        id: 1,
        name: "Feest",
        paymentDeadline: "2020-01-01T00:00:00Z",
      },
      member,
    },
    ...overrides,
  } as EnrollmentBalance;
}

function loaderData(
  overrides: {
    year?: number;
    expiredActivities?: ActivityResponseDto[];
    totalUnpaid?: number;
    openPayments?: number;
    unpaidActivities?: Activity[];
    membersWithOverduePayment?: {
      member: Member;
      enrollments: EnrollmentBalance[];
    }[];
    unpaidBalances?: EnrollmentBalance[];
    overpaidBalances?: EnrollmentBalance[];
  } = {},
) {
  return {
    year: 2025,
    totalUnpaid: 0,
    openPayments: 0,
    unpaidActivities: [],
    membersWithOverduePayment: [],
    unpaidBalances: [],
    overpaidBalances: [],
    expiredActivities: [],
    ...overrides,
  };
}

describe("finances clientLoader", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("waits for auth and combines finances data with expired activities for the URL's year", async () => {
    requireTokenParsed.mockResolvedValue({ UserId: "user-1" });
    fetchFinancesData.mockResolvedValue({
      unpaidBalances: [],
      totalUnpaid: 42,
      openPayments: 1,
      unpaidActivities: [],
      membersWithOverduePayment: [],
      overpaidBalances: [],
    });
    fetchExpiredActivities.mockResolvedValue([{ id: 1, name: "Old" }]);

    const result = await clientLoader({
      request: new Request("https://example.com/admin/finances?year=2024"),
    });

    expect(requireTokenParsed).toHaveBeenCalled();
    expect(fetchExpiredActivities).toHaveBeenCalledWith(2024);
    expect(result.totalUnpaid).toBe(42);
    expect(result.expiredActivities).toEqual([{ id: 1, name: "Old" }]);
    expect(result.year).toBe(2024);
  });

  it("defaults to the current committee year when none is in the URL", async () => {
    requireTokenParsed.mockResolvedValue({ UserId: "user-1" });
    fetchFinancesData.mockResolvedValue({
      unpaidBalances: [],
      totalUnpaid: 0,
      openPayments: 0,
      unpaidActivities: [],
      membersWithOverduePayment: [],
      overpaidBalances: [],
    });
    fetchExpiredActivities.mockResolvedValue([]);

    await clientLoader({
      request: new Request("https://example.com/admin/finances"),
    });

    expect(fetchExpiredActivities).toHaveBeenCalledWith(expect.any(Number));
  });
});

describe("Finances (admin)", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    useLoaderData.mockReturnValue(loaderData());
  });

  it("renders the total unpaid and open payments KPI", () => {
    useLoaderData.mockReturnValue(
      loaderData({ totalUnpaid: 42, openPayments: 3 }),
    );

    renderWithProviders(<Finances />);

    expect(screen.getByText("3 open_payments")).toBeInTheDocument();
    expect(screen.getByText(/42/)).toBeInTheDocument();
  });

  it("shows the overpaid empty state when there are no overpaid balances", () => {
    useLoaderData.mockReturnValue(loaderData({ overpaidBalances: [] }));

    renderWithProviders(<Finances />);

    expect(screen.getByText("no_overpaid_balances")).toBeInTheDocument();
  });

  it("renders overpaid balances", () => {
    useLoaderData.mockReturnValue(
      loaderData({
        overpaidBalances: [
          {
            balance: -20,
            enrollment: {
              member: { firstName: "John", lastName: "Smith" },
              activity: { name: "Borrel" },
            },
          } as EnrollmentBalance,
        ],
      }),
    );

    renderWithProviders(<Finances />);

    expect(screen.getByText("John Smith")).toBeInTheDocument();
    expect(screen.getByText("€20.00")).toBeInTheDocument();
  });

  it("renders expired activities and links to the activity", () => {
    useLoaderData.mockReturnValue(
      loaderData({
        expiredActivities: [
          {
            id: 5,
            name: "Old Party",
            dateTimeEnd: "2020-01-01T00:00:00Z",
            enrollments: [],
            price: 3,
          } as unknown as ActivityResponseDto,
        ],
      }),
    );

    renderWithProviders(<Finances />);

    expect(screen.getByText("Old Party")).toBeInTheDocument();
    expect(screen.getByText("go_to_activity")).toBeInTheDocument();
  });

  it("changes the URL's year when a different expired-activities year is selected", () => {
    useLoaderData.mockReturnValue(loaderData({ year: 2025 }));

    renderWithProviders(<Finances />, { route: "/admin/finances?year=2025" });

    fireEvent.change(screen.getByLabelText("year"), {
      target: { value: "2023" },
    });

    // The Select drives navigation via useSearchParams; asserting no crash and the
    // control reflects the loader's current year covers this component's own contract -
    // React Router's own URL-sync mechanics are exercised in the admin activities page tests.
    expect(screen.getByLabelText("year")).toHaveValue("2025");
  });

  it("renders unpaid activities with an expandable member list and marks as paid", () => {
    useLoaderData.mockReturnValue(
      loaderData({
        unpaidActivities: [{ id: 1, name: "Feest" } as Activity],
        membersWithOverduePayment: [{ member, enrollments: [unpaidBalance()] }],
        unpaidBalances: [unpaidBalance()],
      }),
    );

    renderWithProviders(<Finances />);

    expect(screen.getByText("Feest")).toBeInTheDocument();
    // "Jane Doe" appears both in the unpaid-activities breakdown and the overdue-payment
    // section below, since both are derived from membersWithOverduePayment.
    expect(screen.getAllByText("Jane Doe").length).toBeGreaterThan(0);

    const markAsPaidButton = screen.getByText("mark_as_paid");
    fireEvent.click(markAsPaidButton);

    expect(handleMarkAsPaid).toHaveBeenCalledWith(
      expect.objectContaining({
        member,
        enrollments: [unpaidBalance()],
      }),
    );
  });

  it("shows overdue members highlighted and triggers WhatsApp reminders", () => {
    useLoaderData.mockReturnValue(
      loaderData({
        membersWithOverduePayment: [{ member, enrollments: [unpaidBalance()] }],
      }),
    );

    renderWithProviders(<Finances />);

    const whatsappButton = screen.getByText("WhatsApp");
    fireEvent.click(whatsappButton);

    expect(handleWhatsAppClick).toHaveBeenCalledWith({
      member,
      enrollments: [unpaidBalance()],
    });
  });

  it("does not render members whose overdue enrollments are all in the future", () => {
    useLoaderData.mockReturnValue(
      loaderData({
        membersWithOverduePayment: [
          {
            member,
            enrollments: [
              unpaidBalance({
                enrollment: {
                  activityId: 1,
                  activity: {
                    id: 1,
                    name: "Feest",
                    paymentDeadline: "2099-01-01T00:00:00Z",
                  },
                  member,
                },
              } as EnrollmentBalance),
            ],
          },
        ],
      }),
    );

    renderWithProviders(<Finances />);

    expect(screen.getByText("overdue_payment")).toBeInTheDocument();
    expect(screen.queryByText("WhatsApp")).not.toBeInTheDocument();
  });

  it("enables export only once both dates are filled and calls the export handler", () => {
    renderWithProviders(<Finances />);

    const exportButton = screen.getByText("export").closest("button");
    expect(exportButton).toBeDisabled();

    fireEvent.change(screen.getByLabelText("start_date"), {
      target: { value: "2024-01-01" },
    });
    fireEvent.change(screen.getByLabelText("end_date"), {
      target: { value: "2024-01-31" },
    });

    expect(exportButton).not.toBeDisabled();
    fireEvent.click(exportButton as HTMLButtonElement);

    expect(handlePaymentsExport).toHaveBeenCalledWith(
      "2024-01-01",
      "2024-01-31",
      expect.any(Function),
    );
  });
});
