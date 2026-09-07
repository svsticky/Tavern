/**
 * Shared test helpers for component/route tests.
 *
 * Conventions used across this test suite:
 * - Do NOT import `~/i18n` (or anything that transitively imports it) in tests — it wires up the
 *   real HttpBackend + LanguageDetector, which try to hit the network / read browser APIs.
 *   vitest.setup.ts instead does a minimal, synchronous, network-free `i18next.init()` (empty
 *   resources, no backend) so every test gets a real, working i18n instance for free: `t("save")`
 *   falls back to returning the key `"save"` for any missing translation (same as before), and
 *   `i18n.language` is a real, safe `"en"` (some components read `i18n.language` directly from
 *   `useTranslation()`, which would otherwise crash since `i18n` itself is `undefined` until an
 *   instance is registered). You don't need to do anything for this - it's automatic.
 * - Mock `~/api` per-test with `vi.hoisted` + `vi.mock("~/api", ...)` for whichever named exports
 *   the unit under test actually calls; there's no single shared API mock because each
 *   component/handler calls a different subset of the generated SDK.
 * - Use `renderWithProviders` instead of RTL's `render` for anything that uses `useAuth()`,
 *   `useApp()`, or react-router hooks (`useNavigate`, `useParams`, `Link`, etc.).
 */
import { type RenderOptions, render } from "@testing-library/react";
import type { ReactElement, ReactNode } from "react";
import { MemoryRouter } from "react-router";
import { vi } from "vitest";
import type { IAuthService } from "~/auth/IAuthService";
import { AdminModeProvider } from "~/context/AdminModeContext";
import { AppProvider } from "~/context/AppContext";
import AuthContext from "~/context/AuthContext";
import type { TokenParsed } from "~/types/TokenParsed";

/**
 * Builds a fully-stubbed IAuthService. All methods are vi.fn() so call assertions work
 * out of the box; override only the methods a given test cares about.
 */
export function createMockAuthService(
  overrides: Partial<IAuthService> = {},
): IAuthService {
  return {
    login: vi.fn(async () => {}),
    logout: vi.fn(async () => {}),
    isAuthenticated: vi.fn(() => true),
    isReady: vi.fn(() => true),
    getToken: vi.fn(async () => "mock-token"),
    getTokenParsed: vi.fn(async () => ({
      locale: "en",
      UserId: "00000000-0000-0000-0000-000000000000" as TokenParsed["UserId"],
      access_level: "member",
      given_name: "Test",
      family_name: "User",
      name: "Test User",
    })),
    AuthProvider: ({ children }: { children: ReactNode }) => <>{children}</>,
    getUpdateEmailUrl: vi.fn(async () => "https://example.com/update-email"),
    getUpdatePasswordUrl: vi.fn(
      async () => "https://example.com/update-password",
    ),
    configure2FA: vi.fn(async () => "https://example.com/2fa"),
    ...overrides,
  };
}

type RenderWithProvidersOptions = Omit<RenderOptions, "wrapper"> & {
  /** Initial router path. Defaults to "/". */
  route?: string;
  /** Auth service to expose via useAuth(). Defaults to a fully-authenticated mock. */
  authService?: IAuthService;
  /** Set to false to render without the AppProvider (rarely needed). */
  withAppProvider?: boolean;
};

/**
 * Renders a component wrapped with the same providers the real app tree supplies:
 * a router, AuthContext, and AppContext. Use this for anything that calls useAuth(),
 * useApp(), useNavigate(), useParams(), or renders `<Link>`.
 */
export function renderWithProviders(
  ui: ReactElement,
  {
    route = "/",
    authService = createMockAuthService(),
    withAppProvider = true,
    ...renderOptions
  }: RenderWithProvidersOptions = {},
) {
  function Wrapper({ children }: { children: ReactNode }) {
    const withAuth = (
      <AuthContext.Provider value={authService}>
        <AdminModeProvider>{children}</AdminModeProvider>
      </AuthContext.Provider>
    );
    const inner = withAppProvider ? (
      <AppProvider>{withAuth}</AppProvider>
    ) : (
      withAuth
    );
    return <MemoryRouter initialEntries={[route]}>{inner}</MemoryRouter>;
  }

  return {
    authService,
    ...render(ui, { wrapper: Wrapper, ...renderOptions }),
  };
}

export * from "@testing-library/react";
