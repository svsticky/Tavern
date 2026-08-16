import { describe, expect, it } from "vitest";
import {
  createMockAuthService,
  renderWithProviders,
  screen,
} from "~/testUtils";

describe("createMockAuthService", () => {
  it("provides working default implementations for every auth method", async () => {
    const authService = createMockAuthService();

    await expect(authService.login()).resolves.toBeUndefined();
    await expect(authService.logout("/")).resolves.toBeUndefined();
    expect(authService.isAuthenticated()).toBe(true);
    expect(authService.isReady()).toBe(true);
    await expect(authService.getToken()).resolves.toBe("mock-token");
    await expect(authService.getTokenParsed()).resolves.toEqual(
      expect.objectContaining({ locale: "en" }),
    );
    await expect(authService.getUpdateEmailUrl()).resolves.toBe(
      "https://example.com/update-email",
    );
    await expect(authService.getUpdatePasswordUrl()).resolves.toBe(
      "https://example.com/update-password",
    );
    await expect(authService.resetCredentials()).resolves.toBe(
      "https://example.com/reset",
    );
    await expect(authService.configureMFA()).resolves.toBe(
      "https://example.com/mfa",
    );
  });

  it("allows overriding individual methods", async () => {
    const authService = createMockAuthService({
      isAuthenticated: () => false,
    });
    expect(authService.isAuthenticated()).toBe(false);
  });
});

describe("renderWithProviders", () => {
  it("renders without the AppProvider when withAppProvider is false", () => {
    renderWithProviders(<div>content</div>, { withAppProvider: false });
    expect(screen.getByText("content")).toBeInTheDocument();
  });

  it("renders at a custom initial route", () => {
    renderWithProviders(<div>content</div>, { route: "/custom-route" });
    expect(screen.getByText("content")).toBeInTheDocument();
  });
});
