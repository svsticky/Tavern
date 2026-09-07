import { fireEvent, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { ActivityResponseDto } from "~/api";
import DashboardHeader from "~/components/DashboardHeader";
import { createMockAuthService, renderWithProviders } from "~/testUtils";
import type { TokenParsed } from "~/types/TokenParsed";

const { getEnrollments, getPaymentsUnpaid, postPaymentsActivity } = vi.hoisted(
  () => ({
    getEnrollments: vi.fn(),
    getPaymentsUnpaid: vi.fn(),
    postPaymentsActivity: vi.fn(),
  }),
);

vi.mock("~/api", () => ({
  getEnrollments,
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
    getEnrollments.mockResolvedValue({ data: [] });
  });

  it("renders the greeting with the user's name", async () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => token),
    });
    renderWithProviders(<DashboardHeader name="Jane" />, { authService });
    expect(screen.getByText("Hey Jane!")).toBeInTheDocument();
  });

  it("stops loading without fetching when the user is not authenticated", async () => {
    const authService = createMockAuthService({
      isAuthenticated: vi.fn(() => false),
    });
    renderWithProviders(<DashboardHeader name="Jane" />, { authService });

    await waitFor(() =>
      expect(screen.queryByText("loading")).not.toBeInTheDocument(),
    );
    expect(getPaymentsUnpaid).not.toHaveBeenCalled();
  });

  it("logs an error when the token fails to parse", async () => {
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => null),
    });
    renderWithProviders(<DashboardHeader name="Jane" />, { authService });

    await waitFor(() => expect(consoleError).toHaveBeenCalled());
    consoleError.mockRestore();
  });

  it("computes outstanding payments and enrollment counts", async () => {
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
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => token),
    });
    renderWithProviders(<DashboardHeader name="Jane" />, { authService });

    expect(await screen.findByText("€7.50")).toBeInTheDocument();
    expect(screen.getByText("2")).toBeInTheDocument();
    expect(screen.getByText("1")).toBeInTheDocument();
  });

  it("shows an error toast when data loading fails", async () => {
    getPaymentsUnpaid.mockResolvedValue({ error: "fail" });
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => token),
    });
    renderWithProviders(<DashboardHeader name="Jane" />, { authService });

    await waitFor(() => expect(toastErrorFn).toHaveBeenCalled());
    consoleError.mockRestore();
  });

  it("shows an error toast when fetching enrollments fails", async () => {
    getEnrollments.mockResolvedValue({ error: "fail" });
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => token),
    });
    renderWithProviders(<DashboardHeader name="Jane" />, { authService });

    await waitFor(() => expect(toastErrorFn).toHaveBeenCalled());
    consoleError.mockRestore();
  });

  it("shows an error toast when no enrollment data is returned", async () => {
    getEnrollments.mockResolvedValue({ data: undefined });
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => token),
    });
    renderWithProviders(<DashboardHeader name="Jane" />, { authService });

    await waitFor(() => expect(toastErrorFn).toHaveBeenCalled());
    consoleError.mockRestore();
  });

  it("shows an error toast when the payment request fails to return a checkout URL", async () => {
    getPaymentsUnpaid.mockResolvedValue({
      data: [{ balance: 5, enrollment: { activityId: 1 } }],
    });
    postPaymentsActivity.mockResolvedValue({ data: {} });
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => token),
    });
    renderWithProviders(<DashboardHeader name="Jane" />, { authService });

    const payButton = await screen.findByText("pay");
    fireEvent.click(payButton);

    await waitFor(() => expect(postPaymentsActivity).toHaveBeenCalled());
  });

  it("shows an error toast when the payment request itself errors", async () => {
    getPaymentsUnpaid.mockResolvedValue({
      data: [{ balance: 5, enrollment: { activityId: 1 } }],
    });
    postPaymentsActivity.mockResolvedValue({ error: "fail" });
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => token),
    });
    renderWithProviders(<DashboardHeader name="Jane" />, { authService });

    const payButton = await screen.findByText("pay");
    fireEvent.click(payButton);

    await waitFor(() => expect(postPaymentsActivity).toHaveBeenCalled());
  });

  it("shows the participant count without a limit when there is none", async () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => token),
    });
    renderWithProviders(
      <DashboardHeader
        name="Jane"
        nextActivity={buildActivity({ participantLimit: undefined })}
      />,
      { authService },
    );

    expect(await screen.findByText("0 participants")).toBeInTheDocument();
  });

  it("disables the pay button when there is nothing outstanding", async () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => token),
    });
    renderWithProviders(<DashboardHeader name="Jane" />, { authService });

    expect(await screen.findByText("pay")).toBeDisabled();
  });

  it("redirects to the checkout URL when paying outstanding balances", async () => {
    getPaymentsUnpaid.mockResolvedValue({
      data: [{ balance: 5, enrollment: { activityId: 1 } }],
    });
    postPaymentsActivity.mockResolvedValue({
      data: { checkoutUrl: "https://pay.example.com/checkout" },
    });
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => token),
    });
    renderWithProviders(<DashboardHeader name="Jane" />, { authService });

    const payButton = await screen.findByText("pay");
    expect(payButton).not.toBeDisabled();
    fireEvent.click(payButton);

    await waitFor(() =>
      expect(postPaymentsActivity).toHaveBeenCalledWith({
        body: { memberId: token.UserId, activityIds: [1] },
      }),
    );
  });

  it("does not render the next-activity card when there is none", async () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => token),
    });
    renderWithProviders(<DashboardHeader name="Jane" />, { authService });

    await waitFor(() => expect(getPaymentsUnpaid).toHaveBeenCalled());
    expect(screen.queryByText("upcoming_activity")).not.toBeInTheDocument();
  });

  it("renders the next-activity card and navigates on click", async () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => token),
    });
    renderWithProviders(
      <DashboardHeader
        name="Jane"
        nextActivity={buildActivity({ participantLimit: 20 })}
      />,
      { authService },
    );

    expect(await screen.findByText("Party")).toBeInTheDocument();
    fireEvent.click(screen.getByText("view_details"));
  });

  it("polls for payment confirmation when returning from payment checkout", async () => {
    window.history.pushState({}, "", "/?paymentReturn=activity");
    const replaceStateSpy = vi.spyOn(window.history, "replaceState");

    // First attempt still has unpaid, second attempt has 0 unpaid
    getPaymentsUnpaid
      .mockResolvedValueOnce({
        data: [{ balance: 10, enrollment: { activityId: 1 } }],
      })
      .mockResolvedValueOnce({
        data: [],
      });
    getEnrollments.mockResolvedValue({ data: [] });

    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => token),
    });

    renderWithProviders(<DashboardHeader name="Jane" />, { authService });

    await waitFor(
      () => {
        expect(getPaymentsUnpaid).toHaveBeenCalledTimes(2);
      },
      { timeout: 3500 },
    );

    expect(replaceStateSpy).toHaveBeenCalled();
    window.history.pushState({}, "", "/");
  });
});
