import { screen, waitFor } from "@testing-library/react";
import { Route, Routes } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { ActivityResponseDto } from "~/api";
import {
  getEditActivityBackPath,
  loadEditActivityData,
} from "~/routes/edit-activity/edit-activity.handlers";
import { createMockAuthService, renderWithProviders } from "~/testUtils";
import type { TokenParsed } from "~/types/TokenParsed";

vi.mock("~/routes/edit-activity/edit-activity.handlers", () => ({
  loadEditActivityData: vi.fn(),
  getEditActivityBackPath: vi.fn(() => "/activities"),
}));

vi.mock("~/components/Activity/Edit/EditActivityForm/EditActivityForm", () => ({
  default: ({
    canEditStructural,
    id,
  }: {
    canEditStructural: boolean;
    canManageFinances: boolean;
    id?: string;
  }) => (
    <div>
      edit-activity-form-{canEditStructural ? "board" : "member"}-{id ?? "new"}
    </div>
  ),
}));

vi.mock(
  "~/components/Activity/Edit/SendActivityMailComponent/SendActivityMailComponent",
  () => ({
    default: () => <div>send-activity-mail</div>,
  }),
);

vi.mock(
  "~/components/Activity/Edit/EditParticipantsTile/EditParticipantsTile",
  () => ({
    default: () => <div>edit-participants-tile</div>,
  }),
);

import ActivityFormPage from "~/routes/edit-activity/edit-activity";

const memberToken: TokenParsed = {
  locale: "en",
  UserId: "00000000-0000-0000-0000-000000000000" as TokenParsed["UserId"],
  access_level: "member",
  given_name: "Test",
  family_name: "User",
  name: "Test User",
};

describe("ActivityFormPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("shows a loading state while the token is loading", () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(() => new Promise<TokenParsed | null>(() => {})),
    });
    renderWithProviders(
      <Routes>
        <Route path="/activities/create" element={<ActivityFormPage />} />
      </Routes>,
      { route: "/activities/create", authService },
    );

    expect(screen.getByText("loading")).toBeInTheDocument();
  });

  it("renders the create-activity heading in create mode", async () => {
    vi.mocked(loadEditActivityData).mockImplementation(async ({ setLoading }) =>
      setLoading(false),
    );
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => memberToken),
    });
    renderWithProviders(
      <Routes>
        <Route path="/activities/create" element={<ActivityFormPage />} />
      </Routes>,
      { route: "/activities/create", authService },
    );

    expect(await screen.findByText("create_activity")).toBeInTheDocument();
    expect(
      screen.getByText("edit-activity-form-member-new"),
    ).toBeInTheDocument();
  });

  it("shows failed_fetching when editing an activity that could not be loaded", async () => {
    vi.mocked(loadEditActivityData).mockImplementation(async ({ setLoading }) =>
      setLoading(false),
    );
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => memberToken),
    });
    renderWithProviders(
      <Routes>
        <Route path="/activities/edit/:id" element={<ActivityFormPage />} />
      </Routes>,
      { route: "/activities/edit/5", authService },
    );

    expect(await screen.findByText("failed_fetching")).toBeInTheDocument();
  });

  it("shows admin tools for a board member editing an activity", async () => {
    vi.mocked(loadEditActivityData).mockImplementation(
      async ({ setLoading, setActivity }) => {
        setActivity({ id: 5, name: "Party" } as ActivityResponseDto);
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
      <Routes>
        <Route path="/activities/edit/:id" element={<ActivityFormPage />} />
      </Routes>,
      { route: "/activities/edit/5", authService },
    );

    expect(await screen.findByText("send-activity-mail")).toBeInTheDocument();
    expect(screen.getByText("edit-participants-tile")).toBeInTheDocument();
    expect(screen.getByText("edit-activity-form-board-5")).toBeInTheDocument();
  });

  it("does not show admin tools for a non-board member editing an activity", async () => {
    vi.mocked(loadEditActivityData).mockImplementation(
      async ({ setLoading, setActivity }) => {
        setActivity({ id: 5, name: "Party" } as ActivityResponseDto);
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
      <Routes>
        <Route path="/activities/edit/:id" element={<ActivityFormPage />} />
      </Routes>,
      { route: "/activities/edit/5", authService },
    );

    await screen.findByText("edit-activity-form-member-5");
    expect(screen.queryByText("send-activity-mail")).not.toBeInTheDocument();
  });

  it("uses getEditActivityBackPath for the header back link", async () => {
    vi.mocked(loadEditActivityData).mockImplementation(async ({ setLoading }) =>
      setLoading(false),
    );
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => memberToken),
    });
    renderWithProviders(
      <Routes>
        <Route path="/activities/create" element={<ActivityFormPage />} />
      </Routes>,
      { route: "/activities/create", authService },
    );

    await waitFor(() => expect(getEditActivityBackPath).toHaveBeenCalled());
  });
});
