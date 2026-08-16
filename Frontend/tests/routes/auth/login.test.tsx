import { screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import Login from "~/routes/auth/login";
import { createMockAuthService, renderWithProviders } from "~/testUtils";

describe("Login", () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it("renders the redirecting message", () => {
    const authService = createMockAuthService({
      isReady: vi.fn(() => false),
    });

    renderWithProviders(<Login />, { authService });

    expect(screen.getByText("redirecting_to_login")).toBeInTheDocument();
  });

  it("does nothing while the auth service is not ready", () => {
    const authService = createMockAuthService({
      isReady: vi.fn(() => false),
      isAuthenticated: vi.fn(() => false),
    });

    renderWithProviders(<Login />, { authService });

    expect(authService.login).not.toHaveBeenCalled();
  });

  it("triggers the login redirect when not authenticated and ready", async () => {
    const consoleLog = vi.spyOn(console, "log").mockImplementation(() => {});
    const authService = createMockAuthService({
      isReady: vi.fn(() => true),
      isAuthenticated: vi.fn(() => false),
      login: vi.fn(async () => {}),
    });

    renderWithProviders(<Login />, { authService });

    await waitFor(() => expect(authService.login).toHaveBeenCalled());
    expect(consoleLog).toHaveBeenCalledWith(
      "User not authenticated, redirecting to login...",
    );
  });

  it("bounces an already-authenticated user back to the homepage", async () => {
    const authService = createMockAuthService({
      isReady: vi.fn(() => true),
      isAuthenticated: vi.fn(() => true),
    });
    const replaceSpy = vi.fn();
    const originalLocation = window.location;
    // @ts-expect-error - overriding window.location for the assertion
    delete window.location;
    // @ts-expect-error - partial stub is fine for this assertion
    window.location = { ...originalLocation, replace: replaceSpy };

    renderWithProviders(<Login />, { authService });

    await waitFor(() => expect(replaceSpy).toHaveBeenCalledWith("/"));
    expect(authService.login).not.toHaveBeenCalled();

    // @ts-expect-error - restoring the real Location object after the stub above
    window.location = originalLocation;
  });
});
