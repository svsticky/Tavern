import { fireEvent, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type {
  Activity,
  ActivityResponseDto,
  EnrollmentBalance,
  Member,
} from "~/api";
import { renderRoute } from "~/testUtils";

const {
  loadFinancesLoaderData,
  requireBoardOrCandidateBoard,
  loadExpiredActivities,
  handleMarkAsPaid,
  handleOverpaidProcessed,
  handlePaymentsExport,
  handleWhatsAppClick,
  refreshUnpaidPayments,
} = vi.hoisted(() => ({
  loadFinancesLoaderData: vi.fn(),
  requireBoardOrCandidateBoard: vi.fn(),
  loadExpiredActivities: vi.fn(),
  handleMarkAsPaid: vi.fn(),
  handleOverpaidProcessed: vi.fn(),
  handlePaymentsExport: vi.fn(),
  handleWhatsAppClick: vi.fn(),
  refreshUnpaidPayments: vi.fn(),
}));

vi.mock("~/util/loaderAuth.util", () => ({ requireBoardOrCandidateBoard }));

vi.mock("~/routes/admin/finances/finances.handlers", () => ({
  loadFinancesLoaderData,
  loadExpiredActivities,
  handleMarkAsPaid,
  handleOverpaidProcessed,
  handlePaymentsExport,
  handleWhatsAppClick,
  refreshUnpaidPayments,
}));

import * as route from "~/routes/admin/finances/finances";

/** Renders the finances page. */
function renderFinances() {
  return renderRoute(route, { path: "/admin/finances" });
}

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
  loadFinancesLoaderData.mockResolvedValue({
    totalUnpaid: overrides.totalUnpaid ?? 0,
    openPayments: overrides.openPayments ?? 0,
    unpaidActivities: overrides.unpaidActivities ?? [],
    membersWithOverduePayment: overrides.membersWithOverduePayment ?? [],
    unpaidBalances: overrides.unpaidBalances ?? [],
    overpaidBalances: overrides.overpaidBalances ?? [],
    committeeYear: 2026,
    expiredActivities: overrides.expiredActivities ?? [],
  });

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
    requireBoardOrCandidateBoard.mockResolvedValue({});
    loadWith({});
  });

  it("shows the error page when the data fails to load", async () => {
    loadFinancesLoaderData.mockRejectedValue(
      new Error("Failed to load unpaid balances"),
    );

    renderFinances();

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Failed to load unpaid balances",
    );
  });

  it("renders the total unpaid and open payments KPI", async () => {
    loadWith({ totalUnpaid: 42, openPayments: 3 });

    renderFinances();

    expect(await screen.findByText("3 open_payments")).toBeInTheDocument();
    expect(screen.getByText(/42/)).toBeInTheDocument();
  });

  it("shows the overpaid empty state when there are no overpaid balances", async () => {
    loadWith({ overpaidBalances: [] });

    renderFinances();

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

    renderFinances();

    expect(await screen.findByText("John Smith")).toBeInTheDocument();
    expect(screen.getByText("€20.00")).toBeInTheDocument();
  });

  it("marks an overpaid balance as processed", async () => {
    const overpaid = {
      balance: -20,
      enrollment: {
        memberId: "m2",
        activityId: 3,
        member: { firstName: "John", lastName: "Smith" },
        activity: { name: "Borrel" },
      },
    } as EnrollmentBalance;
    loadWith({ overpaidBalances: [overpaid] });

    renderFinances();

    fireEvent.click(await screen.findByText("processed"));
    expect(handleOverpaidProcessed).toHaveBeenCalledWith(
      expect.objectContaining({ balance: overpaid }),
    );
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

    renderFinances();

    expect(await screen.findByText("Old Party")).toBeInTheDocument();
    expect(screen.getByText("go_to_activity")).toBeInTheDocument();
  });

  it("loads the expired activities of another year when the year changes", async () => {
    renderFinances();

    fireEvent.change(await screen.findByLabelText("year"), {
      target: { value: "2024" },
    });

    expect(loadExpiredActivities).toHaveBeenCalledWith(
      expect.objectContaining({ year: 2024 }),
    );
    expect(screen.getByLabelText("year")).toHaveValue("2024");
  });

  it("renders unpaid activities with an expandable member list and marks as paid", async () => {
    loadWith({
      unpaidActivities: [{ id: 1, name: "Feest" } as Activity],
      membersWithOverduePayment: [{ member, enrollments: [unpaidBalance()] }],
      unpaidBalances: [unpaidBalance()],
    });

    renderFinances();

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

  it("only lists members with an unpaid enrollment for that activity, with that enrollment's balance", async () => {
    const otherMember = {
      ...member,
      id: "m2",
      firstName: "John",
      lastName: "Smith",
    } as Member;
    const feest = unpaidBalance();
    const borrel = unpaidBalance({
      balance: 3,
      enrollment: {
        activityId: 2,
        activity: {
          id: 2,
          name: "Borrel",
          paymentDeadline: "2099-01-01T00:00:00Z",
        },
        member: otherMember,
      },
    } as Partial<EnrollmentBalance>);
    const janeBorrel = unpaidBalance({
      balance: 4,
      enrollment: { ...borrel.enrollment, member },
    });
    loadWith({
      unpaidActivities: [
        { id: 1, name: "Feest" } as Activity,
        { id: 2, name: "Borrel" } as Activity,
      ],
      membersWithOverduePayment: [
        { member, enrollments: [feest, janeBorrel] },
        { member: otherMember, enrollments: [borrel] },
      ],
      unpaidBalances: [feest, janeBorrel, borrel],
    });

    renderFinances();

    expect(await screen.findByText("Feest")).toBeInTheDocument();
    expect(screen.getByText("unpaid_members (1)")).toBeInTheDocument();
    expect(screen.getByText("unpaid_members (2)")).toBeInTheDocument();
    expect(screen.getByText("€12.50")).toBeInTheDocument();
    expect(screen.getByText("€4.00")).toBeInTheDocument();
    expect(screen.queryByText("€16.50")).not.toBeInTheDocument();

    fireEvent.click(screen.getAllByText("mark_as_paid")[0]);
    expect(handleMarkAsPaid).toHaveBeenCalledWith(
      expect.objectContaining({ member, enrollments: [feest] }),
    );
  });

  it("shows overdue members highlighted and triggers WhatsApp reminders", async () => {
    loadWith({
      membersWithOverduePayment: [{ member, enrollments: [unpaidBalance()] }],
    });

    renderFinances();

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

    renderFinances();

    await screen.findByText("overdue_payment");
    expect(screen.queryByText("WhatsApp")).not.toBeInTheDocument();
  });

  it("enables export only once both dates are filled and calls the export handler", async () => {
    renderFinances();
    await screen.findByText("no_overpaid_balances");

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
