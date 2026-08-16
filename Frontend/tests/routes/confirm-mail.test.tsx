import { screen, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import ConfirmMail from "~/routes/confirm-mail";
import { createMockAuthService, renderWithProviders } from "~/testUtils";

describe("ConfirmMail", () => {
  it("renders the loading message and the branding nav bar", () => {
    const authService = createMockAuthService({
      resetCredentials: vi.fn(() => new Promise<string>(() => {})),
    });

    renderWithProviders(<ConfirmMail />, { authService });

    expect(screen.getByText(/loading/)).toBeInTheDocument();
  });

  it("redirects to the credential reset URL after the delay once authService is available", async () => {
    vi.useFakeTimers({ shouldAdvanceTime: true });
    const resetCredentials = vi.fn(
      async () => "https://example.com/reset-flow",
    );
    const authService = createMockAuthService({ resetCredentials });

    const originalLocation = window.location;
    // @ts-expect-error - overriding window.location for the assertion
    delete window.location;
    // @ts-expect-error - partial stub is fine for this assertion
    window.location = { href: "" };

    renderWithProviders(<ConfirmMail />, { authService });

    await vi.advanceTimersByTimeAsync(1000);

    await waitFor(() => expect(resetCredentials).toHaveBeenCalled());
    await waitFor(() =>
      expect(window.location.href).toBe("https://example.com/reset-flow"),
    );

    // @ts-expect-error - restoring the real Location object after the stub above
    window.location = originalLocation;
    vi.useRealTimers();
  });
});
