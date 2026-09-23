import { configure } from "@testing-library/dom";
import { fireEvent, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { ActivityResponseDto } from "~/api";
import DashboardHeader from "~/components/DashboardHeader";
import { createMockAuthService, renderWithProviders } from "~/testUtils";
import type { TokenParsed } from "~/types/TokenParsed";

// The payment-return poll waits 1500ms between attempts (real timers) - give async
// queries enough headroom to observe a poll actually settling.
configure({ asyncUtilTimeout: 20000 });
vi.setConfig({ testTimeout: 25000 });

const { getPaymentsUnpaid, postPaymentsActivity } = vi.hoisted(() => ({
  getPaymentsUnpaid: vi.fn(),
  postPaymentsActivity: vi.fn(),
}));

vi.mock("~/api", () => ({
  getPaymentsUnpaid,
  postPaymentsActivity,
}));

const toastErrorFn = vi.fn();
vi.mock("react-hot-toast", () => ({
  default: {
    error: (...args: unknown[]) => toastErrorFn(...args),
    promise: vi.fn((p: Promise<unknown>, opts: any) => {
      p.then(
        (data) => opts.success?.(data),
        (err) => opts.error?.(err),
      ).catch(() => {});
      return p;
    }),
  },
}));

const token: TokenParsed = {
  locale: "en",
  UserId: "00000000-0000-0000-0000-000000000000" as TokenParsed["UserId"],
  access_level: "member",
  given_name: "Jane",
  family_name: "Doe",
  name: "Jane Doe",
};

const defaultProps = {
  name: "Jane",
  outstandingPayments: 0,
  unpaidActivityIds: [] as number[],
  pastEnrollmentAmount: 0,
  comingEnrollmentAmount: 0,
};

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

describe("DashboardHeader", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    getPaymentsUnpaid.mockResolvedValue({ data: [] });
  });

  afterEach(() => {
    // Some tests set a `?paymentReturn=activity` URL to trigger the poll effect -
    // reset it so it doesn't leak into the next test.
    window.history.pushState({}, "", "/");
  });

  it("renders the greeting with the user's name", () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => token),
    });
    renderWithProviders(<DashboardHeader {...defaultProps} />, {
      authService,
    });
    expect(screen.getByText("Hey Jane!")).toBeInTheDocument();
  });

  it("renders the enrollment/payment totals from props, with no fetch on a normal mount", () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => token),
    });
    renderWithProviders(
      <DashboardHeader
        {...defaultProps}
        outstandingPayments={7.5}
        unpaidActivityIds={[1, 2]}
        pastEnrollmentAmount={1}
        comingEnrollmentAmount={2}
      />,
      { authService },
    );

    expect(screen.getByText("€7.50")).toBeInTheDocument();
    expect(screen.getByText("2")).toBeInTheDocument();
    expect(screen.getByText("1")).toBeInTheDocument();
    expect(getPaymentsUnpaid).not.toHaveBeenCalled();
  });

  it("polls the payments endpoint until the webhook clears the balance, showing the confirmed total", async () => {
    window.history.pushState({}, "", "/?paymentReturn=activity");
    getPaymentsUnpaid
      .mockResolvedValueOnce({
        data: [{ balance: 3, enrollment: { activityId: 9 } }],
      })
      .mockResolvedValueOnce({ data: [] });
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => token),
    });
    renderWithProviders(
      <DashboardHeader {...defaultProps} outstandingPayments={0} />,
      { authService },
    );

    // First poll still finds it unpaid - shows the "confirming" state, not a stale total.
    expect(await screen.findByText("confirming_payment")).toBeInTheDocument();
    // Second poll (after the 1500ms interval) finds it cleared.
    expect(await screen.findByText("€0.00")).toBeInTheDocument();
    expect(getPaymentsUnpaid).toHaveBeenCalledTimes(2);
    // The `paymentReturn` marker is stripped from the URL once settled.
    await waitFor(() =>
      expect(window.location.search).not.toContain("paymentReturn"),
    );
  });

  it("shows an error toast when the payment-return poll fails", async () => {
    window.history.pushState({}, "", "/?paymentReturn=activity");
    getPaymentsUnpaid.mockResolvedValue({ error: "fail" });
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => token),
    });
    renderWithProviders(<DashboardHeader {...defaultProps} />, {
      authService,
    });

    await waitFor(() => expect(toastErrorFn).toHaveBeenCalled());
    consoleError.mockRestore();
  });

  it("shows an error toast when the payment request fails to return a checkout URL", async () => {
    postPaymentsActivity.mockResolvedValue({ data: {} });
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => token),
    });
    renderWithProviders(
      <DashboardHeader
        {...defaultProps}
        outstandingPayments={5}
        unpaidActivityIds={[1]}
      />,
      { authService },
    );

    fireEvent.click(await screen.findByText("pay"));

    await waitFor(() => expect(postPaymentsActivity).toHaveBeenCalled());
  });

  it("shows an error toast when the payment request itself errors", async () => {
    postPaymentsActivity.mockResolvedValue({ error: "fail" });
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => token),
    });
    renderWithProviders(
      <DashboardHeader
        {...defaultProps}
        outstandingPayments={5}
        unpaidActivityIds={[1]}
      />,
      { authService },
    );

    fireEvent.click(await screen.findByText("pay"));

    await waitFor(() => expect(postPaymentsActivity).toHaveBeenCalled());
  });

  it("shows the participant count without a limit when there is none", () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => token),
    });
    renderWithProviders(
      <DashboardHeader
        {...defaultProps}
        nextActivity={buildActivity({ participantLimit: undefined })}
      />,
      { authService },
    );

    expect(screen.getByText("0 participants")).toBeInTheDocument();
  });

  it("disables the pay button when there is nothing outstanding", () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => token),
    });
    renderWithProviders(<DashboardHeader {...defaultProps} />, {
      authService,
    });

    expect(screen.getByText("pay")).toBeDisabled();
  });

  it("redirects to the checkout URL when paying outstanding balances", async () => {
    postPaymentsActivity.mockResolvedValue({
      data: { checkoutUrl: "https://pay.example.com/checkout" },
    });
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => token),
    });
    renderWithProviders(
      <DashboardHeader
        {...defaultProps}
        outstandingPayments={5}
        unpaidActivityIds={[1]}
      />,
      { authService },
    );

    const payButton = await screen.findByText("pay");
    expect(payButton).not.toBeDisabled();
    fireEvent.click(payButton);

    await waitFor(() =>
      expect(postPaymentsActivity).toHaveBeenCalledWith({
        body: { memberId: token.UserId, activityIds: [1] },
      }),
    );
  });

  it("does not render the next-activity card when there is none", () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => token),
    });
    renderWithProviders(<DashboardHeader {...defaultProps} />, {
      authService,
    });

    expect(screen.queryByText("upcoming_activity")).not.toBeInTheDocument();
  });

  it("renders the next-activity card and navigates on click", () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => token),
    });
    renderWithProviders(
      <DashboardHeader
        {...defaultProps}
        nextActivity={buildActivity({ participantLimit: 20 })}
      />,
      { authService },
    );

    expect(screen.getByText("Party")).toBeInTheDocument();
    fireEvent.click(screen.getByText("view_details"));
  });
});
