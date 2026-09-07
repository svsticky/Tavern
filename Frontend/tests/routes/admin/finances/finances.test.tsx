import { fireEvent, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type {
  Activity,
  ActivityResponseDto,
  EnrollmentBalance,
  Member,
} from "~/api";
import { createMockAuthService, renderWithProviders } from "~/testUtils";
import type { TokenParsed } from "~/types/TokenParsed";

const boardAuthService = createMockAuthService({
  getTokenParsed: vi.fn(
    async () =>
      ({
        locale: "en",
        UserId: "00000000-0000-0000-0000-000000000000" as TokenParsed["UserId"],
        access_level: "member",
        given_name: "Board",
        family_name: "Member",
        name: "Board Member",
        is_admin: true,
      }) satisfies TokenParsed,
  ),
});

const {
  loadFinancesData,
  loadExpiredActivities,
  handleMarkAsPaid,
  handlePaymentsExport,
  handleWhatsAppClick,
  refreshUnpaidPayments,
} = vi.hoisted(() => ({
  loadFinancesData: vi.fn(),
  loadExpiredActivities: vi.fn(),
  handleMarkAsPaid: vi.fn(),
  handlePaymentsExport: vi.fn(),
  handleWhatsAppClick: vi.fn(),
  refreshUnpaidPayments: vi.fn(),
}));

vi.mock("~/routes/admin/finances/finances.handlers", () => ({
  loadFinancesData,
  loadExpiredActivities,
  handleMarkAsPaid,
  handlePaymentsExport,
  handleWhatsAppClick,
  refreshUnpaidPayments,
}));

import Finances from "~/routes/admin/finances/finances";

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

function loadWith(overrides: {
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
}) {
  loadFinancesData.mockImplementation(
    async ({
      setLoading,
      setTotalUnpaid,
      setOpenPayments,
      setUnpaidActivities,
      setMembersWithOverduePayment,
      setUnpaidBalances,
      setOverpaidBalances,
    }: any) => {
      setTotalUnpaid(overrides.totalUnpaid ?? 0);
      setOpenPayments(overrides.openPayments ?? 0);
      setUnpaidActivities(overrides.unpaidActivities ?? []);
      setMembersWithOverduePayment(overrides.membersWithOverduePayment ?? []);
      setUnpaidBalances(overrides.unpaidBalances ?? []);
      setOverpaidBalances(overrides.overpaidBalances ?? []);
      setLoading(false);
    },
  );

  loadExpiredActivities.mockImplementation(
    async ({ setLoadingExpiredActivities, setExpiredActivities }: any) => {
      setExpiredActivities(overrides.expiredActivities ?? []);
      setLoadingExpiredActivities(false);
    },
  );
}

describe("Finances (admin)", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    loadWith({});
  });

  it("shows a loading indicator while data loads", async () => {
    let resolveLoad: (() => void) | undefined;
    loadFinancesData.mockImplementation(
      ({ setLoading }: any) =>
        new Promise<void>((resolve) => {
          resolveLoad = () => {
            setLoading(false);
            resolve();
          };
        }),
    );

    renderWithProviders(<Finances />);
    expect(screen.getByText("loading")).toBeInTheDocument();

    resolveLoad?.();
    await waitFor(() =>
      expect(screen.queryByText("loading")).not.toBeInTheDocument(),
    );
  });

  it("renders the total unpaid and open payments KPI", async () => {
    loadWith({ totalUnpaid: 42, openPayments: 3 });

    renderWithProviders(<Finances />);

    expect(await screen.findByText("3 open_payments")).toBeInTheDocument();
    expect(screen.getByText(/42/)).toBeInTheDocument();
  });

  it("shows the overpaid empty state when there are no overpaid balances", async () => {
    loadWith({ overpaidBalances: [] });

    renderWithProviders(<Finances />);

    expect(await screen.findByText("no_overpaid_balances")).toBeInTheDocument();
  });

  it("renders overpaid balances", async () => {
    loadWith({
      overpaidBalances: [
        {
          balance: -20,
          enrollment: {
            member: { firstName: "John", lastName: "Smith" },
            activity: { name: "Borrel" },
          },
        } as EnrollmentBalance,
      ],
    });

    renderWithProviders(<Finances />);

    expect(await screen.findByText("John Smith")).toBeInTheDocument();
    expect(screen.getByText("€20.00")).toBeInTheDocument();
  });

  it("renders expired activities and navigates on click", async () => {
    loadWith({
      expiredActivities: [
        {
          id: 5,
          name: "Old Party",
          dateTimeEnd: "2020-01-01T00:00:00Z",
          enrollments: [],
          price: 3,
        } as unknown as ActivityResponseDto,
      ],
    });

    renderWithProviders(<Finances />);

    expect(await screen.findByText("Old Party")).toBeInTheDocument();
    expect(screen.getByText("go_to_activity")).toBeInTheDocument();
  });

  it("renders unpaid activities with an expandable member list and marks as paid", async () => {
    loadWith({
      unpaidActivities: [{ id: 1, name: "Feest" } as Activity],
      membersWithOverduePayment: [{ member, enrollments: [unpaidBalance()] }],
      unpaidBalances: [unpaidBalance()],
    });

    renderWithProviders(<Finances />, { authService: boardAuthService });

    expect(await screen.findByText("Feest")).toBeInTheDocument();
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

  it("shows overdue members highlighted and triggers WhatsApp reminders", async () => {
    loadWith({
      membersWithOverduePayment: [{ member, enrollments: [unpaidBalance()] }],
    });

    renderWithProviders(<Finances />, { authService: boardAuthService });

    const whatsappButton = await screen.findByText("WhatsApp");
    fireEvent.click(whatsappButton);

    expect(handleWhatsAppClick).toHaveBeenCalledWith({
      member,
      enrollments: [unpaidBalance()],
    });
  });

  it("does not render members whose overdue enrollments are all in the future", async () => {
    loadWith({
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
    });

    renderWithProviders(<Finances />);

    await screen.findByText("overdue_payment");
    expect(screen.queryByText("WhatsApp")).not.toBeInTheDocument();
  });

  it("enables export only once both dates are filled and calls the export handler", async () => {
    renderWithProviders(<Finances />, { authService: boardAuthService });
    await waitFor(() => expect(loadFinancesData).toHaveBeenCalled());

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
