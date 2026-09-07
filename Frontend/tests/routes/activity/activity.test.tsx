import { fireEvent, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { ActivityResponseDto } from "~/api";
import ActivityPage from "~/routes/activity/activity";
import {
  getActivityBackPath,
  handleDeleteActivity,
  handleEditActivityClick,
  loadActivityData,
} from "~/routes/activity/activity.handlers";
import { createMockAuthService, renderWithProviders } from "~/testUtils";
import type { TokenParsed } from "~/types/TokenParsed";

vi.mock("~/routes/activity/activity.handlers", () => ({
  loadActivityData: vi.fn(),
  getActivityBackPath: vi.fn(() => "/activities"),
  handleEditActivityClick: vi.fn(),
  handleDeleteActivity: vi.fn(),
}));

vi.mock(
  "~/components/Activity/ActivityDetailsTile/ActivityDetailsTile",
  () => ({
    default: () => <div>activity-details-tile</div>,
  }),
);

vi.mock(
  "~/components/Activity/ActivityParticipantsTile/ActivityParticipantsTile",
  () => ({
    default: ({ title }: { title?: string }) => (
      <div>participants-tile-{title ?? "main"}</div>
    ),
  }),
);

const memberToken: TokenParsed = {
  locale: "en",
  UserId: "00000000-0000-0000-0000-000000000000" as TokenParsed["UserId"],
  access_level: "member",
  given_name: "Test",
  family_name: "User",
  name: "Test User",
};

function buildActivity(
  overrides: Partial<ActivityResponseDto> = {},
): ActivityResponseDto {
  return {
    id: 1,
    name: "Party",
    isEnrollable: true,
    enrollments: [],
    areParticipantsVisible: true,
    ...overrides,
  } as ActivityResponseDto;
}

describe("ActivityPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("shows a loading state while the token or activity has not loaded", () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(() => new Promise<TokenParsed | null>(() => {})),
    });
    renderWithProviders(
      <ActivityPage params={{ id: "1" }} {...({} as any)} />,
      {
        authService,
      },
    );
    expect(screen.getByText("loading")).toBeInTheDocument();
  });

  it("logs an error when there is no parsed token", async () => {
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => null),
    });
    renderWithProviders(
      <ActivityPage params={{ id: "1" }} {...({} as any)} />,
      {
        authService,
      },
    );

    await waitFor(() => expect(consoleError).toHaveBeenCalled());
    consoleError.mockRestore();
  });

  it("shows a failed-fetching message when the activity could not be loaded", async () => {
    vi.mocked(loadActivityData).mockImplementation(async ({ setLoading }) => {
      setLoading(false);
    });
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => memberToken),
    });
    renderWithProviders(
      <ActivityPage params={{ id: "1" }} {...({} as any)} />,
      {
        authService,
      },
    );

    expect(await screen.findByText("failed_fetching")).toBeInTheDocument();
  });

  it("renders the activity details and participant tiles when participants are visible", async () => {
    vi.mocked(loadActivityData).mockImplementation(
      async ({ setLoading, setActivity }) => {
        setActivity(buildActivity());
        setLoading(false);
      },
    );
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => memberToken),
    });
    renderWithProviders(
      <ActivityPage params={{ id: "1" }} {...({} as any)} />,
      {
        authService,
      },
    );

    expect(
      await screen.findByText("activity-details-tile"),
    ).toBeInTheDocument();
    expect(screen.getByText("participants-tile-main")).toBeInTheDocument();
    expect(
      screen.getByText("participants-tile-waiting_list"),
    ).toBeInTheDocument();
  });

  it("does not render participant tiles when enrollment has not opened even if areParticipantsVisible is true", async () => {
    vi.mocked(loadActivityData).mockImplementation(
      async ({ setLoading, setActivity }) => {
        setActivity(
          buildActivity({
            isEnrollable: false,
            enrollOpenDate: undefined,
            areParticipantsVisible: true,
          }),
        );
        setLoading(false);
      },
    );
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => memberToken),
    });
    renderWithProviders(
      <ActivityPage params={{ id: "1" }} {...({} as any)} />,
      {
        authService,
      },
    );

    expect(
      await screen.findByText("activity-details-tile"),
    ).toBeInTheDocument();
    expect(
      screen.queryByText("participants-tile-main"),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByText("participants-tile-waiting_list"),
    ).not.toBeInTheDocument();
  });

  it("renders participant tiles when enrollment has closed after closing date if areParticipantsVisible is true", async () => {
    vi.mocked(loadActivityData).mockImplementation(
      async ({ setLoading, setActivity }) => {
        setActivity(
          buildActivity({
            isEnrollable: true,
            enrollmentDeadline: "2020-01-01T00:00:00Z",
            areParticipantsVisible: true,
            enrollments: [
              {
                id: 1,
                memberId: "m1",
                isOnWaitingList: false,
                registeredOn: "2020-01-01T00:00:00Z",
              } as any,
            ],
          }),
        );
        setLoading(false);
      },
    );
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => memberToken),
    });
    renderWithProviders(
      <ActivityPage params={{ id: "1" }} {...({} as any)} />,
      {
        authService,
      },
    );

    expect(
      await screen.findByText("activity-details-tile"),
    ).toBeInTheDocument();
    expect(screen.getByText("participants-tile-main")).toBeInTheDocument();
  });

  it("sorts the waiting list by registration order when there are enrollments", async () => {
    vi.mocked(loadActivityData).mockImplementation(
      async ({ setLoading, setActivity }) => {
        setActivity(
          buildActivity({
            enrollments: [
              {
                isOnWaitingList: true,
                registeredOn: "2026-01-02T00:00:00Z",
              },
              { isOnWaitingList: false, registeredOn: "2026-01-01T00:00:00Z" },
              {
                isOnWaitingList: true,
                registeredOn: "2026-01-01T00:00:00Z",
              },
            ] as ActivityResponseDto["enrollments"],
          }),
        );
        setLoading(false);
      },
    );
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => memberToken),
    });
    renderWithProviders(
      <ActivityPage params={{ id: "1" }} {...({} as any)} />,
      {
        authService,
      },
    );

    expect(
      await screen.findByText("participants-tile-main"),
    ).toBeInTheDocument();
    expect(
      screen.getByText("participants-tile-waiting_list"),
    ).toBeInTheDocument();
  });

  it("does not render participant tiles when participants are not visible", async () => {
    vi.mocked(loadActivityData).mockImplementation(
      async ({ setLoading, setActivity }) => {
        setActivity(buildActivity({ areParticipantsVisible: false }));
        setLoading(false);
      },
    );
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => memberToken),
    });
    renderWithProviders(
      <ActivityPage params={{ id: "1" }} {...({} as any)} />,
      {
        authService,
      },
    );

    await screen.findByText("activity-details-tile");
    expect(
      screen.queryByText("participants-tile-main"),
    ).not.toBeInTheDocument();
  });

  it("does not show an edit button for a member without edit rights", async () => {
    vi.mocked(loadActivityData).mockImplementation(
      async ({ setLoading, setActivity }) => {
        setActivity(buildActivity());
        setLoading(false);
      },
    );
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => ({
        ...memberToken,
        is_admin: false,
      })),
    });
    renderWithProviders(
      <ActivityPage params={{ id: "1" }} {...({} as any)} />,
      {
        authService,
      },
    );

    await screen.findByText("activity-details-tile");
    expect(document.querySelector("svg.lucide-pencil")).not.toBeInTheDocument();
  });

  it("shows and wires up an edit button for a board member", async () => {
    vi.mocked(loadActivityData).mockImplementation(
      async ({ setLoading, setActivity }) => {
        setActivity(buildActivity());
        setLoading(false);
      },
    );
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => ({
        ...memberToken,
        is_admin: true,
      })),
    });
    renderWithProviders(
      <ActivityPage params={{ id: "1" }} {...({} as any)} />,
      {
        authService,
      },
    );

    await screen.findByText("activity-details-tile");
    await waitFor(() =>
      expect(document.querySelector("svg.lucide-pencil")).toBeTruthy(),
    );
    const editButton = document
      .querySelector("svg.lucide-pencil")
      ?.closest("button");
    expect(editButton).toBeTruthy();
    fireEvent.click(editButton!);
    expect(handleEditActivityClick).toHaveBeenCalled();
    expect(getActivityBackPath).toHaveBeenCalled();
  });

  it("does not show a delete button for a non-board member", async () => {
    vi.mocked(loadActivityData).mockImplementation(
      async ({ setLoading, setActivity }) => {
        setActivity(buildActivity());
        setLoading(false);
      },
    );
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => ({
        ...memberToken,
        is_admin: false,
      })),
    });
    renderWithProviders(
      <ActivityPage params={{ id: "1" }} {...({} as any)} />,
      {
        authService,
      },
    );

    await screen.findByText("activity-details-tile");
    expect(
      document.querySelector("svg.lucide-trash-2"),
    ).not.toBeInTheDocument();
  });

  it("shows and wires up a delete button for a board member", async () => {
    vi.mocked(loadActivityData).mockImplementation(
      async ({ setLoading, setActivity }) => {
        setActivity(buildActivity());
        setLoading(false);
      },
    );
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => ({
        ...memberToken,
        is_admin: true,
      })),
    });
    renderWithProviders(
      <ActivityPage params={{ id: "1" }} {...({} as any)} />,
      {
        authService,
      },
    );

    await screen.findByText("activity-details-tile");
    await waitFor(() =>
      expect(document.querySelector("svg.lucide-trash-2")).toBeTruthy(),
    );
    const deleteButton = document
      .querySelector("svg.lucide-trash-2")
      ?.closest("button");
    expect(deleteButton).toBeTruthy();
    fireEvent.click(deleteButton!);
    expect(handleDeleteActivity).toHaveBeenCalledWith(
      1,
      expect.any(Function),
      expect.any(String),
      expect.any(Function),
    );
  });
});
