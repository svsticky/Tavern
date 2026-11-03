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
 * - Use `renderRoute` for route modules: it runs the route's `clientLoader` and renders the page
 *   with the loaded data, like the real app. Route loaders check the login through
 *   `~/util/loaderAuth.util`, so mock that module in route tests.
 */
import { type RenderOptions, render } from "@testing-library/react";
import type { ComponentType, ReactElement, ReactNode } from "react";
import {
  createRoutesStub,
  isRouteErrorResponse,
  MemoryRouter,
  type ShouldRevalidateFunction,
  useRouteError,
} from "react-router";
import { vi } from "vitest";
import type { IAuthService } from "~/auth/IAuthService";
import { AppProvider } from "~/context/AppContext";
import AuthContext from "~/context/AuthContext";
import type { TokenParsed } from "~/types/TokenParsed";

/**
 * Builds the parsed token of a regular member. Pass `is_admin: true` for a board member.
 *
 * @param {Partial<TokenParsed>} [overrides] - The fields to replace.
 * @returns {TokenParsed} The parsed token.
 */
export function makeTokenParsed(
  overrides: Partial<TokenParsed> = {},
): TokenParsed {
  return {
    locale: "en",
    UserId: "00000000-0000-0000-0000-000000000000",
    access_level: "member",
    given_name: "Test",
    family_name: "User",
    name: "Test User",
    ...overrides,
  };
}

/**
 * Builds a fully-stubbed IAuthService. All methods are vi.fn() so call assertions work
 * out of the box; override only the methods a given test cares about.
 *
 * @param {Partial<IAuthService>} [overrides] - The methods to replace on the mock.
 * @returns {IAuthService} The mock auth service.
 */
export function createMockAuthService(
  overrides: Partial<IAuthService> = {},
): IAuthService {
  return {
    init: vi.fn(async () => {}),
    login: vi.fn(async () => {}),
    logout: vi.fn(async () => {}),
    isAuthenticated: vi.fn(() => true),
    isReady: vi.fn(() => true),
    getToken: vi.fn(async () => "mock-token"),
    getTokenParsed: vi.fn(async () => makeTokenParsed()),
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
 *
 * @param {ReactElement} ui - The component to render.
 * @param {RenderWithProvidersOptions} [options] - The initial route, auth service, whether to add the AppProvider, and other RTL render options.
 * @returns The RTL render result, plus the `authService` that was used.
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
        {children}
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

/** The parts of a route module that `renderRoute` uses. */
type RouteModule = {
  default: ComponentType;
  // Route modules type their loader arguments with generated route types.
  clientLoader?: (args: any) => unknown;
  HydrateFallback?: ComponentType;
  shouldRevalidate?: ShouldRevalidateFunction;
};

type RenderRouteOptions = Omit<RenderOptions, "wrapper"> & {
  /** The route's path pattern, for example "/activities/:id". Defaults to "/". */
  path?: string;
  /** The URL to open. Defaults to `path`. */
  route?: string;
  /** Auth service to expose via useAuth(). Defaults to a fully-authenticated mock. */
  authService?: IAuthService;
  /** Other routes to add next to the route, for example the pages it navigates to. */
  otherRoutes?: { path: string; Component: ComponentType }[];
  /** Set to false to render without the AppProvider (rarely needed). */
  withAppProvider?: boolean;
};

/**
 * Shown instead of the route when its loader or page throws. Renders the error
 * message (or the status of a thrown response) so tests can assert on it.
 *
 * @component
 */
function TestRouteErrorBoundary() {
  const error = useRouteError();
  const message = isRouteErrorResponse(error)
    ? `${error.status}`
    : error instanceof Error
      ? error.message
      : String(error);
  return <div role="alert">route error: {message}</div>;
}

/**
 * Renders a route module the way the app does: the route's `clientLoader` runs
 * first, then the page renders with the loaded data. Errors thrown by the loader
 * or page show "route error: <message>" (or the status of a thrown response).
 *
 * @param {RouteModule} routeModule - The imported route module (`import * as route from "~/routes/..."`).
 * @param {RenderRouteOptions} [options] - The path, URL, auth service, other routes, whether to add the AppProvider, and other RTL render options.
 * @returns The RTL render result, plus the `authService` that was used.
 */
export function renderRoute(
  routeModule: RouteModule,
  {
    path = "/",
    route = path,
    authService = createMockAuthService(),
    otherRoutes = [],
    withAppProvider = true,
    ...renderOptions
  }: RenderRouteOptions = {},
) {
  const Stub = createRoutesStub([
    {
      path,
      Component: routeModule.default,
      loader: routeModule.clientLoader,
      HydrateFallback: routeModule.HydrateFallback,
      shouldRevalidate: routeModule.shouldRevalidate,
      ErrorBoundary: TestRouteErrorBoundary,
    },
    ...otherRoutes,
  ]);

  const stub = <Stub initialEntries={[route]} />;

  return {
    authService,
    ...render(
      <AuthContext.Provider value={authService}>
        {withAppProvider ? <AppProvider>{stub}</AppProvider> : stub}
      </AuthContext.Provider>,
      renderOptions,
    ),
  };
}

export * from "@testing-library/react";
