import { fireEvent, screen, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { ActivityResponseDto } from "~/api";
import ActivityTile from "~/components/Activity/ActivityTile/ActivityTile";
import { createMockAuthService, renderWithProviders } from "~/testUtils";
import type { TokenParsed } from "~/types/TokenParsed";

function buildActivity(
  overrides: Partial<ActivityResponseDto> = {},
): ActivityResponseDto {
  return {
    id: 1,
    name: "Party",
    price: 0,
    location: "Enschede",
    dateTimeStart: "2099-08-01T10:00:00Z",
    dateTimeEnd: "2099-08-01T12:00:00Z",
    posterFileName: null,
    showInKoala: false,
    showOnWebsite: false,
    organizerId: 7,
    participantLimit: null,
    enrollments: [],
    isEnrollable: true,
    ...overrides,
  } as ActivityResponseDto;
}

const boardToken: TokenParsed = {
  locale: "en",
  UserId: "00000000-0000-0000-0000-000000000000" as TokenParsed["UserId"],
  access_level: "member",
  given_name: "Board",
  family_name: "Member",
  name: "Board Member",
  is_admin: true,
};

describe("ActivityTile", () => {
  it("renders the activity name, location, and 'free' when price is 0", () => {
    renderWithProviders(<ActivityTile activity={buildActivity()} />);

    expect(screen.getByText("Party")).toBeInTheDocument();
    expect(screen.getByText("Enschede")).toBeInTheDocument();
    expect(screen.getByText("free")).toBeInTheDocument();
  });

  it("renders a formatted price when the activity is not free", () => {
    renderWithProviders(
      <ActivityTile activity={buildActivity({ price: 12.5 })} />,
    );
    expect(screen.getByText("€12.50")).toBeInTheDocument();
  });

  it("shows the no-poster placeholder when there is no poster", () => {
    renderWithProviders(<ActivityTile activity={buildActivity()} />);
    expect(screen.getByText("no_poster")).toBeInTheDocument();
  });

  it("shows an img tag when a poster file name is set", () => {
    renderWithProviders(
      <ActivityTile
        activity={buildActivity({ posterFileName: "poster.jpg" })}
      />,
    );
    expect(screen.getByRole("img", { name: "Party" })).toBeInTheDocument();
  });

  it("shows participant count when there is no participant limit", () => {
    renderWithProviders(
      <ActivityTile
        activity={buildActivity({
          enrollments: [
            { isOnWaitingList: false },
            { isOnWaitingList: false },
            { isOnWaitingList: true },
          ] as ActivityResponseDto["enrollments"],
        })}
      />,
    );
    expect(screen.getByText("2 participants")).toBeInTheDocument();
  });

  it("shows remaining spots when a participant limit is set", () => {
    renderWithProviders(
      <ActivityTile
        activity={buildActivity({
          participantLimit: 10,
          enrollments: [
            { isOnWaitingList: false },
            { isOnWaitingList: false },
          ] as ActivityResponseDto["enrollments"],
        })}
      />,
    );
    expect(screen.getByText("8 places_available")).toBeInTheDocument();
  });

  it("shows participant count instead of places available when enrollment has closed", () => {
    renderWithProviders(
      <ActivityTile
        activity={buildActivity({
          participantLimit: 10,
          dateTimeStart: "2020-01-01T10:00:00Z",
          dateTimeEnd: "2020-01-01T12:00:00Z",
          enrollments: [
            { isOnWaitingList: false },
            { isOnWaitingList: false },
          ] as ActivityResponseDto["enrollments"],
        })}
      />,
    );
    expect(screen.getByText("2 participants")).toBeInTheDocument();
    expect(screen.queryByText("places_available")).not.toBeInTheDocument();
  });

  it("does not show participant count or places available when enrollment has not opened", () => {
    renderWithProviders(
      <ActivityTile
        activity={buildActivity({
          isEnrollable: false,
          enrollOpenDate: undefined,
          enrollments: [
            { isOnWaitingList: false },
          ] as ActivityResponseDto["enrollments"],
        })}
      />,
    );
    expect(screen.queryByText("1 participants")).not.toBeInTheDocument();
    expect(screen.queryByText("participants")).not.toBeInTheDocument();
  });

  it("does not show the edit button for a non-board, non-organizer user", async () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => ({ ...boardToken, is_admin: false })),
    });
    renderWithProviders(<ActivityTile activity={buildActivity()} />, {
      authService,
    });

    await waitFor(() => expect(authService.getTokenParsed).toHaveBeenCalled());
    expect(screen.queryByRole("button")).not.toBeInTheDocument();
  });

  it("shows the edit button for a board member and navigates without following the card link", async () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => boardToken),
    });
    renderWithProviders(<ActivityTile activity={buildActivity()} />, {
      authService,
    });

    const editButton = await screen.findByRole("button");
    const clickEvent = { preventDefault: () => {}, stopPropagation: () => {} };
    fireEvent.click(editButton, clickEvent);

    expect(editButton).toBeInTheDocument();
  });

  it("shows the end date when the activity spans multiple days", () => {
    renderWithProviders(
      <ActivityTile
        activity={buildActivity({
          dateTimeStart: "2026-08-01T10:00:00Z",
          dateTimeEnd: "2026-08-03T12:00:00Z",
        })}
      />,
    );
    expect(screen.getByText("Party")).toBeInTheDocument();
  });

  it("does not fetch the token when unmounted before it resolves", async () => {
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
      <ActivityTile activity={buildActivity()} />,
      { authService },
    );

    unmount();
    resolveToken(boardToken);

    await waitFor(() => expect(authService.getTokenParsed).toHaveBeenCalled());
  });

  it("updates the poster's opacity once the image finishes loading", () => {
    renderWithProviders(
      <ActivityTile
        activity={buildActivity({ posterFileName: "poster.jpg" })}
      />,
    );
    const img = screen.getByRole("img", { name: "Party" });
    expect(img).toHaveClass("opacity-0");

    fireEvent.load(img);

    expect(img).toHaveClass("opacity-100");
  });

  it("keeps the poster at full opacity when the image fails to load", () => {
    renderWithProviders(
      <ActivityTile
        activity={buildActivity({ posterFileName: "poster.jpg" })}
      />,
    );
    const img = screen.getByRole("img", { name: "Party" });

    fireEvent.error(img);

    expect(img).toHaveClass("opacity-100");
  });
});
