import { screen, waitFor } from "@testing-library/react";
import { useEffect } from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { useApp } from "~/context/AppContext";
import AdminLayout from "~/layout/admin";
import { createMockAuthService, renderWithProviders } from "~/testUtils";
import type { TokenParsed } from "~/types/TokenParsed";

const { navigateMock } = vi.hoisted(() => ({ navigateMock: vi.fn() }));

vi.mock("react-router", async () => {
  const actual =
    await vi.importActual<typeof import("react-router")>("react-router");
  return { ...actual, useNavigate: () => navigateMock };
});

const boardToken: TokenParsed = {
  locale: "en",
  UserId: "00000000-0000-0000-0000-000000000000" as TokenParsed["UserId"],
  access_level: "member",
  given_name: "Board",
  family_name: "Member",
  name: "Board Member",
  is_admin: true,
};

const regularToken: TokenParsed = { ...boardToken, is_admin: false };

// AdminLayout only reads boardGroupId/candidateBoardGroupId from AppContext; in the real app
// they're populated by the authenticated layout further up the tree. This harness sets them
// directly on the same AppProvider instance so the authorization branches can be exercised.
function WithGroupIdsPopulated() {
  const { setBoardGroupId, setCandidateBoardGroupId } = useApp();
  useEffect(() => {
    setBoardGroupId(1);
    setCandidateBoardGroupId(2);
  }, [setBoardGroupId, setCandidateBoardGroupId]);
  return <AdminLayout />;
}

describe("AdminLayout", () => {
  beforeEach(() => {
    navigateMock.mockClear();
  });

  it("shows a loading state while app context group IDs are not yet populated", () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(() => new Promise<TokenParsed | null>(() => {})),
    });

    renderWithProviders(<AdminLayout />, { authService });

    expect(screen.getByText("loading")).toBeInTheDocument();
  });

  it("keeps showing loading (never authorizes) while group IDs stay unpopulated, even for a board member", async () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => boardToken),
    });

    renderWithProviders(<AdminLayout />, { authService });

    await waitFor(() => expect(authService.getTokenParsed).toHaveBeenCalled());
    expect(screen.getByText("loading")).toBeInTheDocument();
  });

  it("redirects a non-board member to home once group IDs are populated", async () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => regularToken),
    });

    renderWithProviders(<WithGroupIdsPopulated />, { authService });

    await waitFor(() => expect(navigateMock).toHaveBeenCalledWith("/"));
  });

  it("stops loading and authorizes a board member once group IDs are populated", async () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => boardToken),
    });

    renderWithProviders(<WithGroupIdsPopulated />, { authService });

    await waitFor(() =>
      expect(screen.queryByText("loading")).not.toBeInTheDocument(),
    );
  });
});
