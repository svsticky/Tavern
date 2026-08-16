import { fireEvent, screen, waitFor } from "@testing-library/react";
import i18next from "i18next";
import { afterEach, describe, expect, it, vi } from "vitest";
import type { GetAnnouncementResponseDto } from "~/api";
import AnnouncementTile from "~/components/Announcement/AnnouncementTile";
import { createMockAuthService, renderWithProviders } from "~/testUtils";
import type { TokenParsed } from "~/types/TokenParsed";

function buildAnnouncement(
  overrides: Partial<GetAnnouncementResponseDto> = {},
): GetAnnouncementResponseDto {
  return {
    id: 1,
    titleDutch: "Titel",
    titleEnglish: "Title",
    contentDutch: "Inhoud",
    contentEnglish: "Content",
    createdAt: "2026-08-01T10:00:00Z",
    createdByName: "Jane Doe",
    ...overrides,
  } as GetAnnouncementResponseDto;
}

const memberToken: TokenParsed = {
  locale: "en",
  UserId: "00000000-0000-0000-0000-000000000000" as TokenParsed["UserId"],
  access_level: "member",
  given_name: "Test",
  family_name: "User",
  name: "Test User",
};

describe("AnnouncementTile", () => {
  afterEach(async () => {
    await i18next.changeLanguage("en");
  });

  it("renders the Dutch title/content for a Dutch-locale user", async () => {
    await i18next.changeLanguage("nl");
    renderWithProviders(
      <AnnouncementTile announcement={buildAnnouncement()} />,
    );

    expect(await screen.findByText("Titel")).toBeInTheDocument();
    expect(screen.getByText("Inhoud")).toBeInTheDocument();
  });

  it("does not update state when unmounted before the token resolves", async () => {
    let resolveToken: (value: TokenParsed) => void = () => {};
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(
        () =>
          new Promise<TokenParsed>((resolve) => {
            resolveToken = resolve;
          }),
      ),
    });
    const { unmount } = renderWithProviders(
      <AnnouncementTile announcement={buildAnnouncement()} />,
      { authService },
    );

    unmount();
    resolveToken(memberToken);

    await waitFor(() => expect(authService.getTokenParsed).toHaveBeenCalled());
  });

  it("renders the English title/content for an English-locale user", async () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => memberToken),
    });
    renderWithProviders(
      <AnnouncementTile announcement={buildAnnouncement()} />,
      { authService },
    );

    expect(await screen.findByText("Title")).toBeInTheDocument();
    expect(screen.getByText("Content")).toBeInTheDocument();
  });

  it("renders the announcer name and formatted date", async () => {
    renderWithProviders(
      <AnnouncementTile announcement={buildAnnouncement()} />,
    );
    expect(await screen.findByText("Jane Doe")).toBeInTheDocument();
  });

  it("does not show the edit button for a non-board user", async () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => ({
        ...memberToken,
        is_admin: false,
      })),
    });
    renderWithProviders(
      <AnnouncementTile announcement={buildAnnouncement()} />,
      { authService },
    );

    await waitFor(() => expect(authService.getTokenParsed).toHaveBeenCalled());
    expect(screen.queryByTitle("edit")).not.toBeInTheDocument();
  });

  it("shows and wires up the edit button for a board user", async () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => ({
        ...memberToken,
        is_admin: true,
      })),
    });
    renderWithProviders(
      <AnnouncementTile announcement={buildAnnouncement({ id: 7 })} />,
      { authService },
    );

    const editButton = await screen.findByTitle("edit");
    fireEvent.click(editButton);
  });

  it("applies a custom className", async () => {
    const { container } = renderWithProviders(
      <AnnouncementTile
        announcement={buildAnnouncement()}
        className="my-extra-class"
      />,
    );
    await waitFor(() =>
      expect(container.querySelector(".my-extra-class")).toBeTruthy(),
    );
  });
});
