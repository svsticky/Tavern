import { fireEvent, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { ActivityResponseDto } from "~/api";
import * as route from "~/routes/activity/activity";
import {
  handleEditActivityClick,
  loadActivityLoaderData,
} from "~/routes/activity/activity.handlers";
import { renderRoute } from "~/testUtils";
import type { TokenParsed } from "~/types/TokenParsed";

vi.mock("~/routes/activity/activity.handlers", () => ({
  loadActivityLoaderData: vi.fn(),
  getActivityBackPath: vi.fn(() => "/activities"),
  handleEditActivityClick: vi.fn(),
}));

vi.mock(
  "~/components/Activity/ActivityDetailsTile/ActivityDetailsTile",
  () => ({
    default: ({ organizerName }: { organizerName: string | null }) => (
      <div>activity-details-tile-{organizerName ?? "none"}</div>
    ),
  }),
);

vi.mock(
  "~/components/Activity/ActivityParticipantsTile/ActivityParticipantsTile",
  () => ({
    default: ({
      title,
      isBoard,
      enrollments,
    }: {
      title?: string;
      isBoard?: boolean;
      enrollments: { registeredOn: string }[];
    }) => (
      <div>
        participants-tile-{title ?? "main"}-isBoard-{String(Boolean(isBoard))}-
        {enrollments.map((e) => e.registeredOn).join(",")}
      </div>
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

/** Makes the loader return the given activity for the given user. */
function mockLoaderData(
  activity: ActivityResponseDto,
  tokenParsed: TokenParsed = memberToken,
) {
  vi.mocked(loadActivityLoaderData).mockResolvedValue({
    tokenParsed,
    activity,
    organizerName: "Feestcie",
  });
}

/** Renders the activity page for activity 1. */
function renderActivityPage() {
  return renderRoute(route, {
    path: "/activities/:id",
    route: "/activities/1",
  });
}

describe("ActivityPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    mockLoaderData(buildActivity());
  });

  it("loads the activity from the id in the URL", async () => {
    renderActivityPage();

    await screen.findByText("activity-details-tile-Feestcie");
    expect(loadActivityLoaderData).toHaveBeenCalledWith(
      expect.objectContaining({ id: "1" }),
    );
    expect(screen.getByText("Party")).toBeInTheDocument();
  });

  it("shows the error page when the activity fails to load", async () => {
    vi.mocked(loadActivityLoaderData).mockRejectedValue(
      new Error("Failed to load activities"),
    );
    renderActivityPage();

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Failed to load activities",
    );
  });

  it("renders the participant tiles when participants are visible", async () => {
    renderActivityPage();

    expect(
      await screen.findByText(/participants-tile-main-isBoard-false/),
    ).toBeInTheDocument();
    expect(
      screen.getByText(/participants-tile-waiting_list-isBoard-false/),
    ).toBeInTheDocument();
  });

  it("forwards isBoard=true to the participant tiles for a board member", async () => {
    mockLoaderData(buildActivity(), {
      ...memberToken,
      is_admin: true,
    } as TokenParsed);
    renderActivityPage();

    expect(
      await screen.findByText(/participants-tile-main-isBoard-true/),
    ).toBeInTheDocument();
    expect(
      screen.getByText(/participants-tile-waiting_list-isBoard-true/),
    ).toBeInTheDocument();
  });

  it("still renders participant tiles when enrollment has not opened, as long as areParticipantsVisible is true", async () => {
    mockLoaderData(
      buildActivity({
        isEnrollable: false,
        enrollOpenDate: undefined,
        areParticipantsVisible: true,
      }),
    );
    renderActivityPage();

    expect(
      await screen.findByText(/participants-tile-main/),
    ).toBeInTheDocument();
    expect(
      screen.getByText(/participants-tile-waiting_list/),
    ).toBeInTheDocument();
  });

  it("splits enrollments into participants and a waiting list sorted by registration", async () => {
    mockLoaderData(
      buildActivity({
        enrollments: [
          { isOnWaitingList: true, registeredOn: "2026-01-03" },
          { isOnWaitingList: false, registeredOn: "2026-01-01" },
          { isOnWaitingList: true, registeredOn: "2026-01-02" },
        ] as ActivityResponseDto["enrollments"],
      }),
    );
    renderActivityPage();

    expect(
      await screen.findByText(
        /participants-tile-main-isBoard-false\s*-2026-01-01$/,
      ),
    ).toBeInTheDocument();
    expect(
      screen.getByText(
        /participants-tile-waiting_list-isBoard-false\s*-2026-01-02,2026-01-03$/,
      ),
    ).toBeInTheDocument();
  });

  it("does not render participant tiles when participants are not visible", async () => {
    mockLoaderData(buildActivity({ areParticipantsVisible: false }));
    renderActivityPage();

    await screen.findByText("activity-details-tile-Feestcie");
    expect(screen.queryByText(/participants-tile/)).not.toBeInTheDocument();
  });

  it("does not show an edit button for a member without edit rights", async () => {
    renderActivityPage();

    await screen.findByText("activity-details-tile-Feestcie");
    expect(document.querySelector("svg.lucide-pencil")).not.toBeInTheDocument();
  });

  it("shows and wires up an edit button for a board member", async () => {
    mockLoaderData(buildActivity(), {
      ...memberToken,
      is_admin: true,
    } as TokenParsed);
    renderActivityPage();

    await screen.findByText("activity-details-tile-Feestcie");
    const editButton = document
      .querySelector("svg.lucide-pencil")
      ?.closest("button");
    expect(editButton).toBeTruthy();
    fireEvent.click(editButton!);
    expect(handleEditActivityClick).toHaveBeenCalledWith(
      expect.any(Function),
      expect.any(String),
      1,
    );
  });
});
