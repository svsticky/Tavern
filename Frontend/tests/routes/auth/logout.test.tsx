import { screen, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import LogoutPage from "~/routes/auth/logout";
import { createMockAuthService, renderWithProviders } from "~/testUtils";

const { navigateMock } = vi.hoisted(() => ({ navigateMock: vi.fn() }));

vi.mock("react-router", async () => {
  const actual =
    await vi.importActual<typeof import("react-router")>("react-router");
  return { ...actual, useNavigate: () => navigateMock };
});

describe("LogoutPage", () => {
  it("renders the logging out message", () => {
    const authService = createMockAuthService({
      isReady: vi.fn(() => false),
    });

    renderWithProviders(<LogoutPage />, { authService });

    expect(screen.getByText("logging_out")).toBeInTheDocument();
  });

  it("does nothing while the auth service is not ready", () => {
    const authService = createMockAuthService({
      isReady: vi.fn(() => false),
    });

    renderWithProviders(<LogoutPage />, { authService });

    expect(authService.logout).not.toHaveBeenCalled();
    expect(navigateMock).not.toHaveBeenCalled();
  });

  it("logs out an authenticated user", async () => {
    const authService = createMockAuthService({
      isReady: vi.fn(() => true),
      isAuthenticated: vi.fn(() => true),
      logout: vi.fn(async () => {}),
    });

    renderWithProviders(<LogoutPage />, { authService });

    await waitFor(() =>
      expect(authService.logout).toHaveBeenCalledWith(
        `${window.location.origin}/login`,
      ),
    );
    expect(navigateMock).not.toHaveBeenCalled();
  });

  it("navigates to /login for an already-unauthenticated user", async () => {
    const authService = createMockAuthService({
      isReady: vi.fn(() => true),
      isAuthenticated: vi.fn(() => false),
    });

    renderWithProviders(<LogoutPage />, { authService });

    await waitFor(() =>
      expect(navigateMock).toHaveBeenCalledWith("/login", { replace: true }),
    );
    expect(authService.logout).not.toHaveBeenCalled();
  });
});
