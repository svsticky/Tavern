import { render, screen } from "@testing-library/react";
import type { ReactNode } from "react";
import { MemoryRouter, Route, Routes } from "react-router";
import { afterEach, describe, expect, it, vi } from "vitest";
import AuthServiceLayout, { getActiveAuthService } from "~/layout/auth-service";

const { MockKeycloakAuthService, instances } = vi.hoisted(() => {
  const instances: unknown[] = [];
  class MockKeycloakAuthService {
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

function renderLayout() {
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
  afterEach(() => {
    instances.length = 0;
    vi.unstubAllEnvs();
  });

  it("instantiates a KeycloakAuthService by default and renders its provider around the outlet", () => {
    renderLayout();

    expect(screen.getByTestId("mock-provider")).toBeInTheDocument();
    expect(screen.getByText("Protected page")).toBeInTheDocument();
    expect(instances).toHaveLength(1);
  });

  it("exposes the instantiated service via getActiveAuthService", () => {
    renderLayout();

    expect(getActiveAuthService()).toBe(instances[0]);
  });

  it("shows an unsupported-system message when AUTH_SYSTEM is not recognized", () => {
    vi.stubEnv("AUTH_SYSTEM", "some-other-system");

    renderLayout();

    expect(
      screen.getByText(/Unsupported authentication system/),
    ).toBeInTheDocument();
    expect(instances).toHaveLength(0);
  });
});
