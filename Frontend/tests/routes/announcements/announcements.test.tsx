import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import i18next from "i18next";
import { beforeEach, describe, expect, it, vi } from "vitest";
import AnnouncementsPage from "~/routes/announcements/announcements";
import { createMockAuthService, renderWithProviders } from "~/testUtils";
import type { TokenParsed } from "~/types/TokenParsed";

// This route reads `t` directly from the bare "i18next" singleton (not the `useTranslation()`
// hook), which returns `undefined` for every key until the instance is initialized. Give it a
// minimal local-only init (no resources, no backend/network) so `t("key")` falls back to
// returning the key itself, matching the convention documented in ~/testUtils.
i18next.init({ lng: "en", resources: {} });

const { loadAnnouncements, handleCreateAnnouncementClick } = vi.hoisted(() => ({
  loadAnnouncements: vi.fn(),
  handleCreateAnnouncementClick: vi.fn(),
}));

vi.mock("~/routes/announcements/announcements.handlers", () => ({
  loadAnnouncements,
  handleCreateAnnouncementClick,
}));

function baseToken(overrides: Partial<TokenParsed> = {}): TokenParsed {
  return {
    locale: "en",
    UserId: "00000000-0000-0000-0000-000000000000" as TokenParsed["UserId"],
    access_level: "member",
    given_name: "Test",
    family_name: "User",
    name: "Test User",
    ...overrides,
  };
}

describe("AnnouncementsPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("shows the loading text while waiting for the announcements to load", () => {
    loadAnnouncements.mockImplementation(() => new Promise(() => {}));
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => baseToken()),
    });

    renderWithProviders(<AnnouncementsPage />, { authService });

    expect(screen.getByText("loading")).toBeInTheDocument();
  });

  it("renders the empty state when there are no announcements", async () => {
    loadAnnouncements.mockImplementation(async ({ setLoading }) => {
      setLoading(false);
    });
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => baseToken()),
    });

    renderWithProviders(<AnnouncementsPage />, { authService });

    await waitFor(() =>
      expect(screen.getByText("no_announcements")).toBeInTheDocument(),
    );
  });

  it("renders the announcements list once data has loaded", async () => {
    loadAnnouncements.mockImplementation(
      async ({ setLoading, setAnnouncements }) => {
        setAnnouncements([
          {
            id: 1,
            titleDutch: "Titel",
            titleEnglish: "Title",
            contentDutch: "Inhoud",
            contentEnglish: "Content",
          },
        ]);
        setLoading(false);
      },
    );
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => baseToken()),
    });

    renderWithProviders(<AnnouncementsPage />, { authService });

    await waitFor(() => expect(screen.getByText("Title")).toBeInTheDocument());
  });

  it("does not show the create button for a non-board member", async () => {
    loadAnnouncements.mockImplementation(async ({ setLoading }) => {
      setLoading(false);
    });
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => baseToken({ is_admin: false })),
    });

    renderWithProviders(<AnnouncementsPage />, { authService });

    await waitFor(() =>
      expect(screen.getByText("no_announcements")).toBeInTheDocument(),
    );
    expect(screen.queryByRole("button")).not.toBeInTheDocument();
  });

  it("shows the create button for a board member and navigates on click", async () => {
    loadAnnouncements.mockImplementation(async ({ setLoading }) => {
      setLoading(false);
    });
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => baseToken({ is_admin: true })),
    });
    const user = userEvent.setup();

    renderWithProviders(<AnnouncementsPage />, { authService });

    const button = await screen.findByRole("button");
    await user.click(button);

    expect(handleCreateAnnouncementClick).toHaveBeenCalledTimes(1);
  });

  it("logs an error and does not load announcements when the user is not authenticated", async () => {
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => null),
    });

    renderWithProviders(<AnnouncementsPage />, { authService });

    await waitFor(() =>
      expect(consoleError).toHaveBeenCalledWith("User not authenticated"),
    );
    expect(loadAnnouncements).not.toHaveBeenCalled();

    consoleError.mockRestore();
  });
});
