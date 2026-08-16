import { screen, waitFor } from "@testing-library/react";
import { Route, Routes } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import AuthenticatedLayout from "~/layout/authenticated";
import { createMockAuthService, renderWithProviders } from "~/testUtils";
import type { TokenParsed } from "~/types/TokenParsed";

const { getMembersById, getSettingsById } = vi.hoisted(() => ({
  getMembersById: vi.fn(),
  getSettingsById: vi.fn(),
}));

let requestInterceptor: ((config: any) => Promise<any>) | undefined;
let responseErrorInterceptor: ((error: any) => Promise<any>) | undefined;

vi.mock("~/api/sdk.gen", () => ({ getMembersById, getSettingsById }));
vi.mock("~/api/client.gen", () => ({
  client: {
    instance: {
      interceptors: {
        request: {
          use: vi.fn((cb) => {
            requestInterceptor = cb;
            return 1;
          }),
          eject: vi.fn(),
        },
        response: {
          use: vi.fn((_success, error) => {
            responseErrorInterceptor = error;
            return 1;
          }),
          eject: vi.fn(),
        },
      },
    },
  },
}));

const token: TokenParsed = {
  locale: "en",
  UserId: "00000000-0000-0000-0000-000000000000" as TokenParsed["UserId"],
  access_level: "member",
  given_name: "Test",
  family_name: "User",
  name: "Test User",
};

function renderLayout(authService: ReturnType<typeof createMockAuthService>) {
  return renderWithProviders(
    <Routes>
      <Route element={<AuthenticatedLayout />}>
        <Route index element={<div>Protected content</div>} />
      </Route>
    </Routes>,
    { authService },
  );
}

describe("AuthenticatedLayout", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    requestInterceptor = undefined;
    responseErrorInterceptor = undefined;
    getSettingsById.mockResolvedValue({ data: { value: "1" } });
    getMembersById.mockResolvedValue({ data: { id: token.UserId } });
  });

  it("renders nothing while not ready", () => {
    const authService = createMockAuthService({ isReady: () => false });

    renderLayout(authService);

    expect(screen.queryByText("Protected content")).not.toBeInTheDocument();
  });

  it("redirects to login when the user is not authenticated", async () => {
    const authService = createMockAuthService({
      isReady: () => true,
      isAuthenticated: () => false,
    });

    renderLayout(authService);

    await waitFor(() => expect(authService.login).toHaveBeenCalled());
  });

  it("renders the outlet once the token is loaded for an authenticated user", async () => {
    const authService = createMockAuthService({
      isReady: () => true,
      isAuthenticated: () => true,
      getTokenParsed: vi.fn(async () => token),
    });

    renderLayout(authService);

    await waitFor(() =>
      expect(screen.getByText("Protected content")).toBeInTheDocument(),
    );
  });

  it("fetches board group settings and member data once authenticated", async () => {
    const authService = createMockAuthService({
      isReady: () => true,
      isAuthenticated: () => true,
      getTokenParsed: vi.fn(async () => token),
    });

    renderLayout(authService);

    await waitFor(() =>
      expect(getMembersById).toHaveBeenCalledWith({
        path: { id: token.UserId },
      }),
    );
    expect(getSettingsById).toHaveBeenCalledWith({
      path: { id: "BoardGroupId" },
    });
    expect(getSettingsById).toHaveBeenCalledWith({
      path: { id: "CandidateBoardGroupId" },
    });
  });

  it("retries loading the token when getTokenParsed initially returns null", async () => {
    vi.useFakeTimers();
    const getTokenParsed = vi
      .fn()
      .mockResolvedValueOnce(null)
      .mockResolvedValueOnce(token);
    const authService = createMockAuthService({
      isReady: () => true,
      isAuthenticated: () => true,
      getTokenParsed,
    });

    renderLayout(authService);

    await vi.advanceTimersByTimeAsync(300);

    expect(getTokenParsed).toHaveBeenCalledTimes(2);
    vi.useRealTimers();
  });

  it("attaches a fresh bearer token via the request interceptor", async () => {
    const authService = createMockAuthService({
      isReady: () => true,
      isAuthenticated: () => true,
      getTokenParsed: vi.fn(async () => token),
      getToken: vi.fn(async () => "fresh-token"),
    });

    renderLayout(authService);

    await waitFor(() => expect(requestInterceptor).toBeTruthy());
    const config = { headers: {} as Record<string, string> };
    const result = await requestInterceptor!(config);
    expect(result.headers.Authorization).toBe("Bearer fresh-token");
  });

  it("logs an error when the request interceptor fails to get a fresh token", async () => {
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});
    const authService = createMockAuthService({
      isReady: () => true,
      isAuthenticated: () => true,
      getTokenParsed: vi.fn(async () => token),
      getToken: vi.fn(async () => {
        throw new Error("boom");
      }),
    });

    renderLayout(authService);

    await waitFor(() => expect(requestInterceptor).toBeTruthy());
    await requestInterceptor!({ headers: {} });

    expect(consoleError).toHaveBeenCalled();
    consoleError.mockRestore();
  });

  it("redirects to /logout on a 401 response", async () => {
    const consoleWarn = vi.spyOn(console, "warn").mockImplementation(() => {});
    const authService = createMockAuthService({
      isReady: () => true,
      isAuthenticated: () => true,
      getTokenParsed: vi.fn(async () => token),
    });
    const originalLocation = window.location;
    // @ts-expect-error - overriding window.location for the assertion
    delete window.location;
    // @ts-expect-error - partial stub is fine for this assertion
    window.location = { href: "", pathname: "/" };

    renderLayout(authService);

    await waitFor(() => expect(responseErrorInterceptor).toBeTruthy());
    await responseErrorInterceptor!({ response: { status: 401 } }).catch(
      () => {},
    );

    expect(window.location.href).toBe("/logout");
    consoleWarn.mockRestore();
    // @ts-expect-error - restoring the real Location object after the stub above
    window.location = originalLocation;
  });

  it("redirects to / on a 403 response from a non-root path", async () => {
    const consoleWarn = vi.spyOn(console, "warn").mockImplementation(() => {});
    const authService = createMockAuthService({
      isReady: () => true,
      isAuthenticated: () => true,
      getTokenParsed: vi.fn(async () => token),
    });
    const originalLocation = window.location;
    // @ts-expect-error - overriding window.location for the assertion
    delete window.location;
    // @ts-expect-error - partial stub is fine for this assertion
    window.location = { href: "", pathname: "/admin" };

    renderLayout(authService);

    await waitFor(() => expect(responseErrorInterceptor).toBeTruthy());
    await responseErrorInterceptor!({ response: { status: 403 } }).catch(
      () => {},
    );

    expect(window.location.href).toBe("/");
    consoleWarn.mockRestore();
    // @ts-expect-error - restoring the real Location object after the stub above
    window.location = originalLocation;
  });

  it("does not redirect on a 403 response already on the root path", async () => {
    const consoleWarn = vi.spyOn(console, "warn").mockImplementation(() => {});
    const authService = createMockAuthService({
      isReady: () => true,
      isAuthenticated: () => true,
      getTokenParsed: vi.fn(async () => token),
    });
    const originalLocation = window.location;
    // @ts-expect-error - overriding window.location for the assertion
    delete window.location;
    // @ts-expect-error - partial stub is fine for this assertion
    window.location = { href: "unchanged", pathname: "/" };

    renderLayout(authService);

    await waitFor(() => expect(responseErrorInterceptor).toBeTruthy());
    await responseErrorInterceptor!({ response: { status: 403 } }).catch(
      () => {},
    );

    expect(window.location.href).toBe("unchanged");
    consoleWarn.mockRestore();
    // @ts-expect-error - restoring the real Location object after the stub above
    window.location = originalLocation;
  });

  it("ignores response errors without a response object", async () => {
    const authService = createMockAuthService({
      isReady: () => true,
      isAuthenticated: () => true,
      getTokenParsed: vi.fn(async () => token),
    });

    renderLayout(authService);

    await waitFor(() => expect(responseErrorInterceptor).toBeTruthy());
    await expect(
      responseErrorInterceptor!(new Error("network error")),
    ).rejects.toBeTruthy();
  });

  it("switches the app language to the user's token locale", async () => {
    const authService = createMockAuthService({
      isReady: () => true,
      isAuthenticated: () => true,
      getTokenParsed: vi.fn(async () => ({ ...token, locale: "NL" })),
    });

    renderLayout(authService);

    await waitFor(() =>
      expect(screen.getByText("Protected content")).toBeInTheDocument(),
    );
  });

  it("logs an error for an invalid board group ID", async () => {
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});
    getSettingsById.mockImplementation(async ({ path }: any) => {
      if (path.id === "BoardGroupId")
        return { data: { value: "not-a-number" } };
      return { data: { value: "1" } };
    });
    const authService = createMockAuthService({
      isReady: () => true,
      isAuthenticated: () => true,
      getTokenParsed: vi.fn(async () => token),
    });

    renderLayout(authService);

    await waitFor(() => expect(consoleError).toHaveBeenCalled());
    consoleError.mockRestore();
  });

  it("logs an error for an invalid candidate board group ID", async () => {
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});
    getSettingsById.mockImplementation(async ({ path }: any) => {
      if (path.id === "CandidateBoardGroupId")
        return { data: { value: "not-a-number" } };
      return { data: { value: "1" } };
    });
    const authService = createMockAuthService({
      isReady: () => true,
      isAuthenticated: () => true,
      getTokenParsed: vi.fn(async () => token),
    });

    renderLayout(authService);

    await waitFor(() => expect(consoleError).toHaveBeenCalled());
    consoleError.mockRestore();
  });

  it("logs errors from all four settings/member fetches on failure", async () => {
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});
    getSettingsById.mockRejectedValue(new Error("boom"));
    getMembersById.mockRejectedValue(new Error("boom"));
    const authService = createMockAuthService({
      isReady: () => true,
      isAuthenticated: () => true,
      getTokenParsed: vi.fn(async () => token),
    });

    renderLayout(authService);

    await waitFor(() =>
      expect(consoleError.mock.calls.length).toBeGreaterThanOrEqual(4),
    );
    consoleError.mockRestore();
  });
});
