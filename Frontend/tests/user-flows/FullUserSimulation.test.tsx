import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { AdminModeProvider } from "~/context/AdminModeContext";
import { AppProvider } from "~/context/AppContext";
import AuthContext from "~/context/AuthContext";
import { ThemeProvider } from "~/context/ThemeContext";
import NavBarLayout from "~/layout/navbar";
import { createMockAuthService } from "~/testUtils";
import type { TokenParsed } from "~/types/TokenParsed";

// Mock the API calls made in NavBarLayout
vi.mock("~/api", () => ({
  getMembersByIdProfilePicture: vi.fn(async () => ({
    status: 404,
    data: null,
  })),
}));

describe("End-to-End User Simulation: Theme, Admin Mode & Member Preview", () => {
  beforeEach(() => {
    localStorage.clear();
    document.documentElement.classList.remove("dark");
    vi.restoreAllMocks();
  });

  function renderUserApp({
    isAdmin = true,
    groupMemberships = [],
    initialRoute = "/",
  }: {
    isAdmin?: boolean;
    groupMemberships?: any[];
    initialRoute?: string;
  } = {}) {
    const token: TokenParsed = {
      locale: "nl",
      UserId: "11111111-1111-1111-1111-111111111111" as TokenParsed["UserId"],
      access_level: "member",
      given_name: "Egon",
      family_name: "Ruiter",
      name: "Egon Ruiter",
      is_admin: isAdmin,
      group_memberships: groupMemberships,
    };

    const authService = createMockAuthService({
      isReady: () => true,
      isAuthenticated: () => true,
      getTokenParsed: vi.fn(async () => token),
    });

    return render(
      <MemoryRouter initialEntries={[initialRoute]}>
        <AuthContext.Provider value={authService}>
          <ThemeProvider>
            <AppProvider>
              <AdminModeProvider initialIsAdmin={isAdmin}>
                <Routes>
                  <Route element={<NavBarLayout />}>
                    <Route
                      path="/"
                      element={
                        <div data-testid="page-content">Dashboard Content</div>
                      }
                    />
                    <Route
                      path="/activities"
                      element={
                        <div data-testid="page-content">Activities Content</div>
                      }
                    />
                    <Route
                      path="/account"
                      element={
                        <div data-testid="page-content">
                          Account Settings Content
                        </div>
                      }
                    />
                  </Route>
                </Routes>
              </AdminModeProvider>
            </AppProvider>
          </ThemeProvider>
        </AuthContext.Provider>
      </MemoryRouter>,
    );
  }

  it("User Flow 1: Admin logs in, sees full admin menu, toggles Dark/Light theme seamlessly", async () => {
    renderUserApp({ isAdmin: true });

    // 1. Check user avatar and name are displayed
    const userButton = await screen.findByRole("button", {
      name: /egon ruiter/i,
    });
    expect(userButton).toBeInTheDocument();

    // 2. Open Profile Dropdown
    fireEvent.click(userButton);

    // 3. Admin links are all visible
    expect(screen.getByText("all_activities")).toBeInTheDocument();
    expect(screen.getByText("members")).toBeInTheDocument();
    expect(screen.getByText("groups")).toBeInTheDocument();
    expect(screen.getByText("roles")).toBeInTheDocument();
    expect(screen.getByText("finances")).toBeInTheDocument();
    expect(screen.getByText("koala_settings")).toBeInTheDocument();

    // 4. Test Dark Theme toggle
    const darkButton = screen.getByTitle("theme_dark");
    expect(darkButton).toBeInTheDocument();

    fireEvent.click(darkButton);
    expect(document.documentElement.classList.contains("dark")).toBe(true);
    expect(localStorage.getItem("tavern_theme")).toBe("dark");

    // 5. Test Light Theme toggle
    const lightButton = screen.getByTitle("theme_light");
    fireEvent.click(lightButton);
    expect(document.documentElement.classList.contains("dark")).toBe(false);
    expect(localStorage.getItem("tavern_theme")).toBe("light");
  });

  it("User Flow 2: Admin switches to Member View — admin options disappear, banner appears, then returns", async () => {
    renderUserApp({ isAdmin: true, groupMemberships: [] });

    // Open dropdown
    const userButton = await screen.findByRole("button", {
      name: /egon ruiter/i,
    });
    fireEvent.click(userButton);

    // Click member view button
    const memberViewButton = screen.getByRole("button", {
      name: "member_mode",
    });
    expect(memberViewButton).toBeInTheDocument();

    fireEvent.click(memberViewButton);

    // Top banner must now be rendered
    await waitFor(() => {
      expect(screen.getByText("member_view_banner_text")).toBeInTheDocument();
    });

    // In member view, verify admin links are completely hidden
    expect(screen.queryByText("all_activities")).toBeNull();
    expect(screen.queryByText("members")).toBeNull();
    expect(screen.queryByText("groups")).toBeNull();
    expect(screen.queryByText("roles")).toBeNull();
    expect(screen.queryByText("finances")).toBeNull();
    expect(screen.queryByText("koala_settings")).toBeNull();

    // Only non-admin options remain
    expect(screen.getByText("account")).toBeInTheDocument();
    expect(screen.getByText("logout")).toBeInTheDocument();

    // Now click 'switch_to_admin_mode' in the top banner
    const returnToAdminButton = screen.getByText("switch_to_admin_mode");
    fireEvent.click(returnToAdminButton);

    // Banner should be removed
    await waitFor(() => {
      expect(screen.queryByText("member_view_banner_text")).toBeNull();
    });

    // Admin links are back
    expect(screen.getByText("all_activities")).toBeInTheDocument();
    expect(screen.getByText("members")).toBeInTheDocument();
    expect(screen.getByText("koala_settings")).toBeInTheDocument();
  });

  it("User Flow 3: Regular member never sees admin options or admin mode toggle", async () => {
    renderUserApp({ isAdmin: false, groupMemberships: [] });

    const userButton = await screen.findByRole("button", {
      name: /egon ruiter/i,
    });
    fireEvent.click(userButton);

    // Regular member sees account & logout
    expect(screen.getByText("account")).toBeInTheDocument();
    expect(screen.getByText("logout")).toBeInTheDocument();

    // Regular member does NOT see admin links
    expect(screen.queryByText("all_activities")).toBeNull();
    expect(screen.queryByText("members")).toBeNull();
    expect(screen.queryByText("koala_settings")).toBeNull();

    // Regular member does NOT see view_mode / admin_mode toggle
    expect(screen.queryByText("view_mode")).toBeNull();
    expect(screen.queryByRole("button", { name: "admin_mode" })).toBeNull();
  });
});
