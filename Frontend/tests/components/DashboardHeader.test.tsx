import { fireEvent, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type {
  ActivityResponseDto,
  EnrollmentBalance,
  EnrollmentResponseDto,
} from "~/api";
import DashboardHeader from "~/components/DashboardHeader";
import {
  createMockAuthService,
  makeTokenParsed,
  renderWithProviders,
} from "~/testUtils";
import { formatDate } from "~/util/date.util";

const { getPaymentsUnpaid, postPaymentsActivity } = vi.hoisted(() => ({
  getPaymentsUnpaid: vi.fn(),
  postPaymentsActivity: vi.fn(),
}));

vi.mock("~/api", () => ({ getPaymentsUnpaid, postPaymentsActivity }));

const { toastPromiseErrors } = vi.hoisted(() => ({
  toastPromiseErrors: vi.fn(),
}));

vi.mock("react-hot-toast", () => ({
  default: {
    error: vi.fn(),
    promise: vi.fn((p: Promise<unknown>, opts: any) => {
      p.then(
        (data) => opts.success?.(data),
        (err) => toastPromiseErrors(opts.error?.(err)),
      ).catch(() => {});
      return p;
    }),
  },
}));

const token = makeTokenParsed({ given_name: "Jane", family_name: "Doe" });

function buildActivity(
  overrides: Partial<ActivityResponseDto> = {},
): ActivityResponseDto {
  return {
    id: 1,
    name: "Party",
    dateTimeStart: "2026-09-01T10:00:00Z",
    enrollments: [],
    ...overrides,
  } as ActivityResponseDto;
}

function balance(
  amount: number,
  activityId: number,
  activityName = "Activity",
): EnrollmentBalance {
  return {
    balance: amount,
    enrollment: { activityId, activity: { name: activityName } },
  } as EnrollmentBalance;
}

/** Waits for the header to load the user's token, which paying needs. */
const waitForToken = () => new Promise((resolve) => setTimeout(resolve, 0));

/** Renders the header for Jane, with no balances or enrollments unless given. */
function renderHeader({
  nextActivity,
  unpaidBalances = [],
  enrollments = [],
}: {
  nextActivity?: ActivityResponseDto;
  unpaidBalances?: EnrollmentBalance[];
  enrollments?: unknown[];
} = {}) {
  const authService = createMockAuthService({
    getTokenParsed: vi.fn(async () => token),
  });
  return renderWithProviders(
    <DashboardHeader
      name="Jane"
      nextActivity={nextActivity as ActivityResponseDto}
      unpaidBalances={unpaidBalances}
      enrollments={enrollments as EnrollmentResponseDto[]}
    />,
    { authService },
  );
}

describe("DashboardHeader", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    getPaymentsUnpaid.mockResolvedValue({ data: [] });
  });

  afterEach(() => {
    window.history.replaceState({}, "", "/");
  });

  it("renders the greeting with the user's name", () => {
    renderHeader();
    expect(screen.getByText("Hey Jane!")).toBeInTheDocument();
  });

  it("computes outstanding payments and enrollment counts", () => {
    renderHeader({
      unpaidBalances: [balance(5, 1), balance(2.5, 2)],
      enrollments: [
        { activity: { dateTimeEnd: "2020-01-01T00:00:00Z" } },
        { activity: { dateTimeEnd: "2099-01-01T00:00:00Z" } },
        { activity: { dateTimeEnd: "2099-06-01T00:00:00Z" } },
      ],
    });

    expect(screen.getByText("€7.50")).toBeInTheDocument();
    expect(screen.getByText("2")).toBeInTheDocument();
    expect(screen.getByText("1")).toBeInTheDocument();
  });

  it("does not fetch the payments when not returning from a payment", () => {
    renderHeader();
    expect(getPaymentsUnpaid).not.toHaveBeenCalled();
  });

  it("refreshes the outstanding payments when returning from a payment", async () => {
    window.history.replaceState({}, "", "/?paymentReturn=activity");
    getPaymentsUnpaid.mockResolvedValue({ data: [] });
    renderHeader({ unpaidBalances: [balance(5, 1)] });

    expect(screen.getByText("€5.00")).toBeInTheDocument();
    expect(await screen.findByText("€0.00")).toBeInTheDocument();
    expect(window.location.search).toBe("");
  });

  it("disables the pay button when there is nothing outstanding", () => {
    renderHeader();
    expect(screen.getByText("pay")).toBeDisabled();
  });

  it("starts a payment for all outstanding activities", async () => {
    postPaymentsActivity.mockResolvedValue({
      data: { checkoutUrl: "https://pay.example.com/checkout" },
    });
    renderHeader({ unpaidBalances: [balance(5, 1), balance(3, 4)] });

    const payButton = screen.getByText("pay");
    expect(payButton).not.toBeDisabled();
    await waitForToken();
    fireEvent.click(payButton);

    await waitFor(() =>
      expect(postPaymentsActivity).toHaveBeenCalledWith({
        body: { memberId: token.UserId, activityIds: [1, 4] },
      }),
    );
  });

  it("shows an error when no checkout URL is returned", async () => {
    postPaymentsActivity.mockResolvedValue({ data: {} });
    renderHeader({ unpaidBalances: [balance(5, 1)] });

    await waitForToken();
    fireEvent.click(screen.getByText("pay"));

    await waitFor(() =>
      expect(toastPromiseErrors).toHaveBeenCalledWith(
        expect.stringContaining("payment_initiation_failed"),
      ),
    );
  });

  it("shows an error when the payment request itself errors", async () => {
    postPaymentsActivity.mockResolvedValue({ error: "fail" });
    renderHeader({ unpaidBalances: [balance(5, 1)] });

    await waitForToken();
    fireEvent.click(screen.getByText("pay"));

    await waitFor(() =>
      expect(toastPromiseErrors).toHaveBeenCalledWith(
        expect.stringContaining("payment_initiation_failed"),
      ),
    );
  });

  it("shows the participant count without a limit when there is none", () => {
    renderHeader({
      nextActivity: buildActivity({ participantLimit: undefined }),
    });

    expect(screen.getByText("0 participants")).toBeInTheDocument();
  });

  it("does not render the next-activity card when there is none", () => {
    renderHeader();
    expect(screen.queryByText("upcoming_activity")).not.toBeInTheDocument();
  });

  it("renders the next-activity card", () => {
    renderHeader({ nextActivity: buildActivity({ participantLimit: 20 }) });

    expect(screen.getByText("Party")).toBeInTheDocument();
    expect(screen.getByText("view_details")).toBeInTheDocument();
  });

  it("opens a modal with the attended activities", () => {
    renderHeader({
      enrollments: [
        {
          price: 4,
          activity: {
            name: "Borrel",
            dateTimeStart: "2019-12-31T20:00:00Z",
            dateTimeEnd: "2020-01-01T00:00:00Z",
          },
        },
        {
          activity: {
            name: "Future",
            price: 0,
            dateTimeEnd: "2099-01-01T00:00:00Z",
          },
        },
      ],
    });

    fireEvent.click(screen.getByText("attended"));

    expect(screen.getByText("attended_activities")).toBeInTheDocument();
    expect(screen.getByText("Borrel")).toBeInTheDocument();
    expect(
      screen.getByText(
        formatDate(new Date("2019-12-31T20:00:00Z"), "dateOnly"),
      ),
    ).toBeInTheDocument();
    expect(screen.queryByText("€4.00")).not.toBeInTheDocument();
    expect(screen.queryByText("Future")).not.toBeInTheDocument();

    fireEvent.click(screen.getByLabelText("close_modal"));
    expect(screen.queryByText("attended_activities")).not.toBeInTheDocument();
  });

  it("opens a modal with the outstanding payments", () => {
    renderHeader({ unpaidBalances: [balance(5, 1, "Gala")] });

    fireEvent.click(screen.getByText("€5.00"));

    expect(screen.getByText("Gala")).toBeInTheDocument();
    expect(screen.getAllByText("€5.00")).toHaveLength(2);

    fireEvent.click(screen.getByLabelText("close_modal"));
    expect(screen.queryByText("Gala")).not.toBeInTheDocument();
  });

  it("opens a modal with the enrolled activities", () => {
    renderHeader({
      enrollments: [
        {
          price: 3,
          activity: {
            id: 2,
            name: "Lunch",
            dateTimeStart: "2098-12-31T12:00:00Z",
            dateTimeEnd: "2099-01-01T00:00:00Z",
          },
        },
        {
          activity: {
            name: "Past",
            price: 0,
            dateTimeEnd: "2020-01-01T00:00:00Z",
          },
        },
      ],
    });

    fireEvent.click(screen.getByText("enrollments"));

    expect(screen.getByText("enrolled_activities")).toBeInTheDocument();
    expect(screen.getByText("Lunch")).toBeInTheDocument();
    expect(
      screen.getByText(
        formatDate(new Date("2098-12-31T12:00:00Z"), "dateOnly"),
      ),
    ).toBeInTheDocument();
    expect(screen.queryByText("€3.00")).not.toBeInTheDocument();
    expect(screen.getByRole("link", { name: /Lunch/ })).toHaveAttribute(
      "href",
      "/activities/2",
    );
    expect(screen.queryByText("Past")).not.toBeInTheDocument();

    fireEvent.click(screen.getByLabelText("close_modal"));
    expect(screen.queryByText("enrolled_activities")).not.toBeInTheDocument();
  });
});
