import { fireEvent, render, screen } from "@testing-library/react";
import type React from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import AdminModeToggle from "~/components/UI/AdminModeToggle";
import {
  ADMIN_MODE_STORAGE_KEY,
  AdminModeProvider,
} from "~/context/AdminModeContext";
import AuthContext from "~/context/AuthContext";
import { createMockAuthService } from "~/testUtils";
import type { TokenParsed } from "~/types/TokenParsed";
import { isBoardOrCandidateBoard } from "~/util/group.util";

function renderWithAdminMode(
  ui: React.ReactElement,
  {
    isAdmin = true,
    initialStorage,
  }: { isAdmin?: boolean; initialStorage?: string } = {},
) {
  if (initialStorage !== undefined) {
    localStorage.setItem(ADMIN_MODE_STORAGE_KEY, initialStorage);
  }

  const authService = createMockAuthService({
    getTokenParsed: vi.fn(
      async () =>
        ({
          locale: "en",
          UserId:
            "00000000-0000-0000-0000-000000000000" as TokenParsed["UserId"],
          access_level: "member",
          given_name: "Test",
          family_name: "Admin",
          name: "Test Admin",
          is_admin: isAdmin,
        }) satisfies TokenParsed,
    ),
  });

  return render(
    <AuthContext.Provider value={authService}>
      <AdminModeProvider initialIsAdmin={isAdmin}>{ui}</AdminModeProvider>
    </AuthContext.Provider>,
  );
}

describe("AdminModeToggle", () => {
  beforeEach(() => {
    localStorage.clear();
    vi.restoreAllMocks();
  });

  it("does not render when the user is not an admin", async () => {
    const { container } = renderWithAdminMode(<AdminModeToggle />, {
      isAdmin: false,
    });

    expect(
      await screen.queryByRole("button", { name: /admin_mode/i }),
    ).toBeNull();
    expect(container).toBeEmptyDOMElement();
  });

  it("renders segmented control with Admin mode and Member view options for admins", async () => {
    renderWithAdminMode(<AdminModeToggle variant="segmented" />);

    expect(
      await screen.findByRole("button", { name: /admin_mode/i }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: /member_mode/i }),
    ).toBeInTheDocument();
  });

  it("switches to member view and updates localStorage and group.util when clicked", async () => {
    renderWithAdminMode(<AdminModeToggle variant="segmented" />);

    const memberBtn = await screen.findByRole("button", {
      name: /member_mode/i,
    });
    fireEvent.click(memberBtn);

    expect(localStorage.getItem(ADMIN_MODE_STORAGE_KEY)).toBe("false");

    const token = { is_admin: true } as TokenParsed;
    expect(isBoardOrCandidateBoard(token)).toBe(false);

    const adminBtn = screen.getByRole("button", { name: /admin_mode/i });
    fireEvent.click(adminBtn);

    expect(localStorage.getItem(ADMIN_MODE_STORAGE_KEY)).toBe("true");
    expect(isBoardOrCandidateBoard(token)).toBe(true);
  });

  it("renders switch variant and toggles on click", async () => {
    renderWithAdminMode(<AdminModeToggle variant="switch" />);

    const toggle = await screen.findByRole("switch");
    expect(toggle).toBeInTheDocument();

    fireEvent.click(toggle);
    expect(localStorage.getItem(ADMIN_MODE_STORAGE_KEY)).toBe("false");

    fireEvent.click(toggle);
    expect(localStorage.getItem(ADMIN_MODE_STORAGE_KEY)).toBe("true");
  });

  it("renders dropdown variant and toggles on click", async () => {
    renderWithAdminMode(<AdminModeToggle variant="dropdown" />);

    expect(await screen.findByText("view_mode")).toBeInTheDocument();
    const memberBtn = screen.getByRole("button", { name: "member_mode" });
    expect(memberBtn).toBeInTheDocument();

    fireEvent.click(memberBtn);
    expect(localStorage.getItem(ADMIN_MODE_STORAGE_KEY)).toBe("false");

    const adminBtn = screen.getByRole("button", { name: "admin_mode" });
    fireEvent.click(adminBtn);
    expect(localStorage.getItem(ADMIN_MODE_STORAGE_KEY)).toBe("true");
  });
});
