import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import * as route from "~/routes/announcements/announcements";
import { renderRoute } from "~/testUtils";
import type { TokenParsed } from "~/types/TokenParsed";

const { getAnnouncements, requireTokenParsed, handleCreateAnnouncementClick } =
  vi.hoisted(() => ({
    getAnnouncements: vi.fn(),
    requireTokenParsed: vi.fn(),
    handleCreateAnnouncementClick: vi.fn(),
  }));

vi.mock("~/api", () => ({ getAnnouncements }));
vi.mock("~/util/loaderAuth.util", () => ({ requireTokenParsed }));
vi.mock("~/routes/announcements/announcements.handlers", () => ({
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
    requireTokenParsed.mockResolvedValue(baseToken());
    getAnnouncements.mockResolvedValue({ data: [] });
  });

  it("renders the empty state when there are no announcements", async () => {
    renderRoute(route);

    expect(await screen.findByText("no_announcements")).toBeInTheDocument();
  });

  it("renders the announcements list once data has loaded", async () => {
    getAnnouncements.mockResolvedValue({
      data: [
        {
          id: 1,
          titleDutch: "Titel",
          titleEnglish: "Title",
          contentDutch: "Inhoud",
          contentEnglish: "Content",
        },
      ],
    });
    renderRoute(route);

    expect(await screen.findByText("Title")).toBeInTheDocument();
  });

  it.each([
    ["returns an error", { error: "boom" }],
    ["returns no data", {}],
  ])("shows the error page when the API %s", async (_, response) => {
    getAnnouncements.mockResolvedValue(response);
    renderRoute(route);

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Failed to load announcements",
    );
  });

  it("does not show the create button for a non-board member", async () => {
    renderRoute(route);

    await screen.findByText("no_announcements");
    expect(screen.queryByRole("button")).not.toBeInTheDocument();
  });

  it("shows the create button for a board member and navigates on click", async () => {
    requireTokenParsed.mockResolvedValue(baseToken({ is_admin: true }));
    const user = userEvent.setup();
    renderRoute(route);

    await user.click(await screen.findByRole("button"));

    expect(handleCreateAnnouncementClick).toHaveBeenCalledTimes(1);
  });
});
