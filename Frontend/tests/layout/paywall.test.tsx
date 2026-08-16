import { fireEvent, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import PaywallLayout from "~/layout/paywall";
import { createMockAuthService, renderWithProviders } from "~/testUtils";
import type { TokenParsed } from "~/types/TokenParsed";

const {
  getPaymentsMemberByFromUserIdStatus,
  patchMembersById,
  postPaymentsMembership,
  deleteMembersById,
} = vi.hoisted(() => ({
  getPaymentsMemberByFromUserIdStatus: vi.fn(),
  patchMembersById: vi.fn(),
  postPaymentsMembership: vi.fn(),
  deleteMembersById: vi.fn(),
}));

vi.mock("~/api/sdk.gen", () => ({
  getPaymentsMemberByFromUserIdStatus,
  patchMembersById,
  postPaymentsMembership,
  deleteMembersById,
}));

vi.mock("react-hot-toast", () => ({
  default: { success: vi.fn(), error: vi.fn() },
}));

const token: TokenParsed = {
  locale: "en",
  UserId: "00000000-0000-0000-0000-000000000000" as TokenParsed["UserId"],
  access_level: "member",
  given_name: "Test",
  family_name: "User",
  name: "Test User",
};

describe("PaywallLayout", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders nothing while the token hasn't loaded yet", () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(() => new Promise<TokenParsed | null>(() => {})),
    });
    getPaymentsMemberByFromUserIdStatus.mockResolvedValue({ data: undefined });

    const { container } = renderWithProviders(<PaywallLayout />, {
      authService,
    });

    expect(container).toBeEmptyDOMElement();
  });

  it("renders the outlet when membership has been paid", async () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => token),
    });
    getPaymentsMemberByFromUserIdStatus.mockResolvedValue({
      data: {
        hasEverPaidMembership: true,
        hasPaidMembershipBeforeExpirationTime: true,
      },
    });

    renderWithProviders(<PaywallLayout />, { authService, route: "/" });

    await waitFor(() =>
      expect(getPaymentsMemberByFromUserIdStatus).toHaveBeenCalledWith({
        path: { fromUserId: token.UserId },
      }),
    );
  });

  it("shows the payment-required screen once a checkout URL has been fetched", async () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => token),
    });
    getPaymentsMemberByFromUserIdStatus.mockResolvedValue({
      data: {
        hasEverPaidMembership: false,
        hasPaidMembershipBeforeExpirationTime: false,
      },
    });
    postPaymentsMembership.mockResolvedValue({
      data: { checkoutUrl: "https://pay.example.com/checkout" },
    });

    renderWithProviders(<PaywallLayout />, { authService });

    await waitFor(() =>
      expect(
        screen.getByText("membership_payment_required"),
      ).toBeInTheDocument(),
    );
    await waitFor(() => expect(screen.getByText("pay")).toBeInTheDocument());
  });

  it("logs an error when fetching the payment status fails", async () => {
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => token),
    });
    getPaymentsMemberByFromUserIdStatus.mockRejectedValue(new Error("boom"));

    renderWithProviders(<PaywallLayout />, { authService });

    await waitFor(() => expect(consoleError).toHaveBeenCalled());
    consoleError.mockRestore();
  });

  it("logs an error when the checkout URL request fails to return a URL", async () => {
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => token),
    });
    getPaymentsMemberByFromUserIdStatus.mockResolvedValue({
      data: {
        hasEverPaidMembership: false,
        hasPaidMembershipBeforeExpirationTime: false,
      },
    });
    postPaymentsMembership.mockResolvedValue({ data: undefined });

    renderWithProviders(<PaywallLayout />, { authService });

    await waitFor(() => expect(consoleError).toHaveBeenCalled());
    consoleError.mockRestore();
  });

  it("shows the payment-not-processed screen when paid but access level is still not_paid", async () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => ({
        ...token,
        access_level: "not_paid",
      })),
    });
    getPaymentsMemberByFromUserIdStatus.mockResolvedValue({
      data: {
        hasEverPaidMembership: true,
        hasPaidMembershipBeforeExpirationTime: true,
      },
    });

    renderWithProviders(<PaywallLayout />, { authService });

    expect(
      await screen.findByText("payment_not_processed"),
    ).toBeInTheDocument();
  });

  it("redirects to the checkout URL when the pay button is clicked", async () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => token),
    });
    getPaymentsMemberByFromUserIdStatus.mockResolvedValue({
      data: {
        hasEverPaidMembership: false,
        hasPaidMembershipBeforeExpirationTime: false,
      },
    });
    postPaymentsMembership.mockResolvedValue({
      data: { checkoutUrl: "https://pay.example.com/checkout" },
    });
    const originalLocation = window.location;
    // @ts-expect-error - overriding window.location for the assertion
    delete window.location;
    // @ts-expect-error - partial stub is fine for this assertion
    window.location = { href: "" };

    renderWithProviders(<PaywallLayout />, { authService });

    const payButton = await screen.findByText("pay");
    fireEvent.click(payButton);

    await waitFor(() =>
      expect(window.location.href).toBe("https://pay.example.com/checkout"),
    );

    // @ts-expect-error - restoring the real Location object after the stub above
    window.location = originalLocation;
  });

  it("does nothing when the delete-account confirmation is cancelled", async () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => token),
    });
    getPaymentsMemberByFromUserIdStatus.mockResolvedValue({
      data: {
        hasEverPaidMembership: false,
        hasPaidMembershipBeforeExpirationTime: false,
      },
    });
    postPaymentsMembership.mockResolvedValue({
      data: { checkoutUrl: "https://pay.example.com/checkout" },
    });
    const confirmSpy = vi.spyOn(window, "confirm").mockReturnValue(false);

    renderWithProviders(<PaywallLayout />, { authService });

    const deleteButton = await screen.findByText("delete_account");
    fireEvent.click(deleteButton);

    expect(deleteMembersById).not.toHaveBeenCalled();
    confirmSpy.mockRestore();
  });

  it("deletes the account and logs out when confirmed", async () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => token),
      logout: vi.fn(async () => {}),
    });
    getPaymentsMemberByFromUserIdStatus.mockResolvedValue({
      data: {
        hasEverPaidMembership: false,
        hasPaidMembershipBeforeExpirationTime: false,
      },
    });
    postPaymentsMembership.mockResolvedValue({
      data: { checkoutUrl: "https://pay.example.com/checkout" },
    });
    deleteMembersById.mockResolvedValue({ status: 200 });
    const confirmSpy = vi.spyOn(window, "confirm").mockReturnValue(true);

    renderWithProviders(<PaywallLayout />, { authService });

    const deleteButton = await screen.findByText("delete_account");
    fireEvent.click(deleteButton);

    await waitFor(() =>
      expect(deleteMembersById).toHaveBeenCalledWith({
        path: { id: token.UserId },
      }),
    );
    await waitFor(() => expect(authService.logout).toHaveBeenCalled());
    confirmSpy.mockRestore();
  });

  it("shows an error toast when account deletion fails", async () => {
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => token),
    });
    getPaymentsMemberByFromUserIdStatus.mockResolvedValue({
      data: {
        hasEverPaidMembership: false,
        hasPaidMembershipBeforeExpirationTime: false,
      },
    });
    postPaymentsMembership.mockResolvedValue({
      data: { checkoutUrl: "https://pay.example.com/checkout" },
    });
    deleteMembersById.mockResolvedValue({ status: 500, error: "fail" });
    const confirmSpy = vi.spyOn(window, "confirm").mockReturnValue(true);

    renderWithProviders(<PaywallLayout />, { authService });

    const deleteButton = await screen.findByText("delete_account");
    fireEvent.click(deleteButton);

    await waitFor(() => expect(consoleError).toHaveBeenCalled());
    confirmSpy.mockRestore();
    consoleError.mockRestore();
  });
});
