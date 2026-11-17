import { screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { ActivityResponseDto, GroupResponseDto } from "~/api";
import * as route from "~/routes/edit-activity/edit-activity";
import {
  getEditActivityBackPath,
  loadEditActivityLoaderData,
} from "~/routes/edit-activity/edit-activity.handlers";
import { renderRoute } from "~/testUtils";
import type { TokenParsed } from "~/types/TokenParsed";

vi.mock("~/routes/edit-activity/edit-activity.handlers", () => ({
  loadEditActivityLoaderData: vi.fn(),
  getEditActivityBackPath: vi.fn(() => "/activities"),
}));

vi.mock("~/components/Activity/Edit/EditActivityForm/EditActivityForm", () => ({
  default: ({
    isBoard,
    id,
    groups,
  }: {
    isBoard: boolean;
    id?: string;
    groups: GroupResponseDto[];
  }) => (
    <div>
      edit-activity-form-{isBoard ? "board" : "member"}-{id ?? "new"}-groups-
      {groups.length}
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

const memberToken: TokenParsed = {
  locale: "en",
  UserId: "00000000-0000-0000-0000-000000000000" as TokenParsed["UserId"],
  access_level: "member",
  given_name: "Test",
  family_name: "User",
  name: "Test User",
};

const boardToken = { ...memberToken, is_admin: true } as TokenParsed;
const groups = [{ id: 1 }] as GroupResponseDto[];
const activity = { id: 5, name: "Party" } as ActivityResponseDto;

function renderCreate() {
  return renderRoute(route, { path: "/activities/create" });
}

function renderEdit() {
  return renderRoute(route, {
    path: "/activities/edit/:id",
    route: "/activities/edit/5",
  });
}

describe("ActivityFormPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(loadEditActivityLoaderData).mockResolvedValue({
      tokenParsed: memberToken,
      activity: null,
      groups,
    });
  });

  it("renders the create-activity heading in create mode", async () => {
    renderCreate();

    expect(await screen.findByText("create_activity")).toBeInTheDocument();
    expect(
      screen.getByText(/edit-activity-form-member-new-groups-\s*1/),
    ).toBeInTheDocument();
  });

  it("passes the id from the URL to the loader", async () => {
    vi.mocked(loadEditActivityLoaderData).mockResolvedValue({
      tokenParsed: memberToken,
      activity,
      groups,
    });
    renderEdit();

    await screen.findByText("edit_activity");
    expect(loadEditActivityLoaderData).toHaveBeenCalledWith(
      expect.objectContaining({ id: "5" }),
    );
  });

  it("shows the error page when the data fails to load", async () => {
    vi.mocked(loadEditActivityLoaderData).mockRejectedValue(
      new Error("Failed to load activities"),
    );
    renderEdit();

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Failed to load activities",
    );
  });

  it("shows admin tools for a board member editing an activity", async () => {
    vi.mocked(loadEditActivityLoaderData).mockResolvedValue({
      tokenParsed: boardToken,
      activity,
      groups,
    });
    renderEdit();

    expect(await screen.findByText("send-activity-mail")).toBeInTheDocument();
    expect(screen.getByText("edit-participants-tile")).toBeInTheDocument();
    expect(screen.getByText(/edit-activity-form-board-5/)).toBeInTheDocument();
  });

  it("does not show admin tools for a board member creating an activity", async () => {
    vi.mocked(loadEditActivityLoaderData).mockResolvedValue({
      tokenParsed: boardToken,
      activity: null,
      groups,
    });
    renderCreate();

    await screen.findByText(/edit-activity-form-board-new/);
    expect(screen.queryByText("send-activity-mail")).not.toBeInTheDocument();
  });

  it("does not show admin tools for a non-board member editing an activity", async () => {
    vi.mocked(loadEditActivityLoaderData).mockResolvedValue({
      tokenParsed: memberToken,
      activity,
      groups,
    });
    renderEdit();

    await screen.findByText(/edit-activity-form-member-5/);
    expect(screen.queryByText("send-activity-mail")).not.toBeInTheDocument();
  });

  it("uses getEditActivityBackPath for the header back link", async () => {
    renderCreate();

    await screen.findByText("create_activity");
    expect(getEditActivityBackPath).toHaveBeenCalledWith(
      "/activities/create",
      false,
      undefined,
    );
  });
});
