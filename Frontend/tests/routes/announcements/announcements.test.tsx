import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import i18next from "i18next";
import { describe, expect, it, vi } from "vitest";
import AnnouncementsPage, {
  clientLoader,
} from "~/routes/announcements/announcements";
import { renderWithProviders } from "~/testUtils";
import type { TokenParsed } from "~/types/TokenParsed";

// This route reads `t` directly from the bare "i18next" singleton (not the `useTranslation()`
// hook), which returns `undefined` for every key until the instance is initialized. Give it a
// minimal local-only init (no resources, no backend/network) so `t("key")` falls back to
// returning the key itself, matching the convention documented in ~/testUtils.
i18next.init({ lng: "en", resources: {} });

const { requireTokenParsed } = vi.hoisted(() => ({
  requireTokenParsed: vi.fn(),
}));
vi.mock("~/util/loaderAuth.util", () => ({ requireTokenParsed }));

const { loadAnnouncements, handleCreateAnnouncementClick } = vi.hoisted(
  () => ({
    loadAnnouncements: vi.fn(),
    handleCreateAnnouncementClick: vi.fn(),
  }),
);
vi.mock("~/routes/announcements/announcements.handlers", () => ({
  loadAnnouncements,
  handleCreateAnnouncementClick,
}));

const { useLoaderData } = vi.hoisted(() => ({ useLoaderData: vi.fn() }));
vi.mock("react-router", async (importOriginal) => ({
  ...(await importOriginal<typeof import("react-router")>()),
  useLoaderData,
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

describe("announcements clientLoader", () => {
  it("requires a token and loads announcements", async () => {
    const token = baseToken();
    requireTokenParsed.mockResolvedValue(token);
    loadAnnouncements.mockResolvedValue([{ id: 1 }]);

    await expect(clientLoader()).resolves.toEqual({
      tokenParsed: token,
      announcements: [{ id: 1 }],
    });
  });
});

describe("AnnouncementsPage", () => {
  it("renders the empty state when there are no announcements", () => {
    useLoaderData.mockReturnValue({
      tokenParsed: baseToken(),
      announcements: [],
    });

    renderWithProviders(<AnnouncementsPage />);

    expect(screen.getByText("no_announcements")).toBeInTheDocument();
  });

  it("renders the announcements list once data has loaded", () => {
    useLoaderData.mockReturnValue({
      tokenParsed: baseToken(),
      announcements: [
        {
          id: 1,
          titleDutch: "Titel",
          titleEnglish: "Title",
          contentDutch: "Inhoud",
          contentEnglish: "Content",
        },
      ],
    });

    renderWithProviders(<AnnouncementsPage />);

    expect(screen.getByText("Title")).toBeInTheDocument();
  });

  it("does not show the create button for a non-board member", () => {
    useLoaderData.mockReturnValue({
      tokenParsed: baseToken({ is_admin: false }),
      announcements: [],
    });

    renderWithProviders(<AnnouncementsPage />);

    expect(screen.queryByRole("button")).not.toBeInTheDocument();
  });

  it("shows the create button for a board member and navigates on click", async () => {
    useLoaderData.mockReturnValue({
      tokenParsed: baseToken({ is_admin: true }),
      announcements: [],
    });
    const user = userEvent.setup();

    renderWithProviders(<AnnouncementsPage />);

    const button = await screen.findByRole("button");
    await user.click(button);

    expect(handleCreateAnnouncementClick).toHaveBeenCalledTimes(1);
  });
});
