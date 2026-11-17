import { render, screen } from "@testing-library/react";
import type { ReactNode } from "react";
import { MemoryRouter, Route, Routes } from "react-router";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

const { MockKeycloakAuthService, instances } = vi.hoisted(() => {
  const instances: { init: ReturnType<typeof vi.fn> }[] = [];
  class MockKeycloakAuthService {
    init = vi.fn(async () => {});
    AuthProvider = ({ children }: { children: ReactNode }) => (
      <div data-testid="mock-provider">{children}</div>
    );
    constructor() {
      instances.push(this);
    }
  }
  return { MockKeycloakAuthService, instances };
});

vi.mock("~/auth/KeycloakService", () => ({
  KeycloakAuthService: MockKeycloakAuthService,
}));

/**
 * Imports a fresh copy of the module, because it keeps the auth service it
 * created for the rest of the page's life.
 */
async function importAuthService() {
  vi.resetModules();
  return import("~/layout/auth-service");
}

async function renderLayout() {
  const { default: AuthServiceLayout } = await importAuthService();
  return render(
    <MemoryRouter initialEntries={["/"]}>
      <Routes>
        <Route element={<AuthServiceLayout />}>
          <Route index element={<div>Protected page</div>} />
        </Route>
      </Routes>
    </MemoryRouter>,
  );
}

describe("AuthServiceLayout", () => {
  beforeEach(() => {
    instances.length = 0;
  });

  afterEach(() => {
    vi.unstubAllEnvs();
  });

  it("instantiates a KeycloakAuthService by default and renders its provider around the outlet", async () => {
    await renderLayout();

    expect(screen.getByTestId("mock-provider")).toBeInTheDocument();
    expect(screen.getByText("Protected page")).toBeInTheDocument();
    expect(instances).toHaveLength(1);
  });

  it("shows an unsupported-system message when AUTH_SYSTEM is not recognized", async () => {
    vi.stubEnv("AUTH_SYSTEM", "some-other-system");

    await renderLayout();

    expect(
      screen.getByText(/Unsupported authentication system/),
    ).toBeInTheDocument();
    expect(instances).toHaveLength(0);
  });
});

describe("getActiveAuthService", () => {
  beforeEach(() => {
    instances.length = 0;
  });

  afterEach(() => {
    vi.unstubAllEnvs();
  });

  it("creates the service once and returns the same instance afterwards", async () => {
    const { getActiveAuthService } = await importAuthService();

    const first = getActiveAuthService();

    expect(getActiveAuthService()).toBe(first);
    expect(first).toBe(instances[0]);
    expect(instances).toHaveLength(1);
  });

  it("is shared with the layout", async () => {
    const { default: AuthServiceLayout, getActiveAuthService } =
      await importAuthService();
    render(
      <MemoryRouter>
        <Routes>
          <Route element={<AuthServiceLayout />}>
            <Route index element={<div>Protected page</div>} />
          </Route>
        </Routes>
      </MemoryRouter>,
    );

    expect(getActiveAuthService()).toBe(instances[0]);
    expect(instances).toHaveLength(1);
  });
});

describe("waitForAuthReady", () => {
  beforeEach(() => {
    instances.length = 0;
  });

  afterEach(() => {
    vi.unstubAllEnvs();
  });

  it("initializes the service and returns it", async () => {
    const { waitForAuthReady } = await importAuthService();

    const authService = await waitForAuthReady();

    expect(authService).toBe(instances[0]);
    expect(instances[0].init).toHaveBeenCalled();
  });

  it("returns null when the auth system is not supported", async () => {
    vi.stubEnv("AUTH_SYSTEM", "some-other-system");
    const { waitForAuthReady } = await importAuthService();

    await expect(waitForAuthReady()).resolves.toBeNull();
  });
});
