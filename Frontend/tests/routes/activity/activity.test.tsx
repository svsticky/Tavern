import { fireEvent, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { ActivityResponseDto } from "~/api";
import ActivityPage, { clientLoader } from "~/routes/activity/activity";
import {
  fetchActivity,
  fetchOrganizerName,
  getActivityBackPath,
  handleEditActivityClick,
} from "~/routes/activity/activity.handlers";
import { renderWithProviders } from "~/testUtils";
import type { TokenParsed } from "~/types/TokenParsed";

vi.mock("~/routes/activity/activity.handlers", () => ({
  fetchActivity: vi.fn(),
  fetchOrganizerName: vi.fn(),
  getActivityBackPath: vi.fn(() => "/activities"),
  handleEditActivityClick: vi.fn(),
}));

const { requireTokenParsed } = vi.hoisted(() => ({
  requireTokenParsed: vi.fn(),
}));
vi.mock("~/util/loaderAuth.util", () => ({ requireTokenParsed }));

const { useLoaderData } = vi.hoisted(() => ({ useLoaderData: vi.fn() }));
vi.mock("react-router", async (importOriginal) => ({
  ...(await importOriginal<typeof import("react-router")>()),
  useLoaderData,
}));

const detailsTileProps = vi.hoisted(() => ({ current: null as unknown }));
vi.mock(
  "~/components/Activity/ActivityDetailsTile/ActivityDetailsTile",
  () => ({
    default: (props: { organizerName?: string | null }) => {
      detailsTileProps.current = props;
      return <div>activity-details-tile</div>;
    },
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
      enrollments: { id: string }[];
    }) => (
      <div>
        participants-tile-{title ?? "main"}-isBoard-{String(Boolean(isBoard))}-
        {enrollments.map((e) => e.id).join(",")}
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
const boardToken: TokenParsed = { ...memberToken, is_admin: true };

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

function loaderData(
  overrides: Partial<{
    tokenParsed: TokenParsed;
    activity: ActivityResponseDto;
    organizerName: string | null;
  }> = {},
) {
  return {
    tokenParsed: memberToken,
    activity: buildActivity(),
    organizerName: null,
    ...overrides,
  };
}

describe("activity clientLoader", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    requireTokenParsed.mockResolvedValue(memberToken);
  });

  it("loads the activity named by the URL, its organizer's name and the token", async () => {
    const activity = buildActivity({ id: 7, organizerId: 3 });
    vi.mocked(fetchActivity).mockResolvedValue(activity);
    vi.mocked(fetchOrganizerName).mockResolvedValue("BaCo");

    const result = await clientLoader({ params: { id: "7" } });

    expect(fetchActivity).toHaveBeenCalledWith(7);
    expect(fetchOrganizerName).toHaveBeenCalledWith(3);
    expect(result).toEqual({
      tokenParsed: memberToken,
      activity,
      organizerName: "BaCo",
    });
  });

  it("propagates a failed activity fetch to React Router's error boundary", async () => {
    vi.mocked(fetchActivity).mockRejectedValue(new Error("fail"));

    await expect(clientLoader({ params: { id: "1" } })).rejects.toThrow("fail");
  });
});

describe("ActivityPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    detailsTileProps.current = null;
  });

  it("renders the activity details and participant tiles when participants are visible", () => {
    useLoaderData.mockReturnValue(loaderData());
    renderWithProviders(<ActivityPage />);

    expect(screen.getByText("Party")).toBeInTheDocument();
    expect(screen.getByText("activity-details-tile")).toBeInTheDocument();
    expect(
      screen.getByText(/participants-tile-main-isBoard-false/),
    ).toBeInTheDocument();
    expect(
      screen.getByText(/participants-tile-waiting_list-isBoard-false/),
    ).toBeInTheDocument();
  });

  it("hands the loader's organizer name to the details tile", () => {
    useLoaderData.mockReturnValue(loaderData({ organizerName: "BaCo" }));
    renderWithProviders(<ActivityPage />);

    expect(detailsTileProps.current).toEqual(
      expect.objectContaining({ organizerName: "BaCo" }),
    );
  });

  it("forwards isBoard=true to the participant tiles for a board member", () => {
    useLoaderData.mockReturnValue(loaderData({ tokenParsed: boardToken }));
    renderWithProviders(<ActivityPage />);

    expect(
      screen.getByText(/participants-tile-main-isBoard-true/),
    ).toBeInTheDocument();
    expect(
      screen.getByText(/participants-tile-waiting_list-isBoard-true/),
    ).toBeInTheDocument();
  });

  it("still renders participant tiles when enrollment has not opened, as long as areParticipantsVisible is true", () => {
    useLoaderData.mockReturnValue(
      loaderData({
        activity: buildActivity({
          isEnrollable: false,
          enrollOpenDate: undefined,
        }),
      }),
    );
    renderWithProviders(<ActivityPage />);

    expect(
      screen.getByText(/participants-tile-main-isBoard-false/),
    ).toBeInTheDocument();
  });

  it("splits participants from the waiting list, oldest registration first", () => {
    const enrollment = (id: string, registeredOn: string, wait: boolean) =>
      ({ id, registeredOn, isOnWaitingList: wait }) as never;
    useLoaderData.mockReturnValue(
      loaderData({
        activity: buildActivity({
          enrollments: [
            enrollment("p1", "2026-01-01", false),
            enrollment("w2", "2026-01-03", true),
            enrollment("w1", "2026-01-02", true),
          ],
        }),
      }),
    );
    renderWithProviders(<ActivityPage />);

    expect(
      screen.getByText("participants-tile-main-isBoard-false-p1"),
    ).toBeInTheDocument();
    expect(
      screen.getByText("participants-tile-waiting_list-isBoard-false-w1,w2"),
    ).toBeInTheDocument();
  });

  it("does not render participant tiles when participants are not visible", () => {
    useLoaderData.mockReturnValue(
      loaderData({
        activity: buildActivity({ areParticipantsVisible: false }),
      }),
    );
    renderWithProviders(<ActivityPage />);

    expect(screen.getByText("activity-details-tile")).toBeInTheDocument();
    expect(screen.queryByText(/participants-tile/)).not.toBeInTheDocument();
  });

  it("does not show an edit button for a member without edit rights", () => {
    useLoaderData.mockReturnValue(loaderData());
    renderWithProviders(<ActivityPage />);

    expect(document.querySelector("svg.lucide-pencil")).not.toBeInTheDocument();
  });

  it("shows and wires up an edit button for a board member", () => {
    useLoaderData.mockReturnValue(loaderData({ tokenParsed: boardToken }));
    renderWithProviders(<ActivityPage />);

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
    expect(getActivityBackPath).toHaveBeenCalled();
  });
});
