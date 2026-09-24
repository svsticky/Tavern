import { screen } from "@testing-library/react";
import { Route, Routes } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { ActivityResponseDto, GroupResponseDto } from "~/api";
import { fetchGroups } from "~/components/Activity/Edit/EditActivityForm/EditActivityForm.handlers";
import ActivityFormPage, {
  clientLoader,
} from "~/routes/edit-activity/edit-activity";
import {
  fetchEditActivity,
  getEditActivityBackPath,
} from "~/routes/edit-activity/edit-activity.handlers";
import { renderWithProviders } from "~/testUtils";
import type { TokenParsed } from "~/types/TokenParsed";

vi.mock("~/routes/edit-activity/edit-activity.handlers", () => ({
  fetchEditActivity: vi.fn(),
  getEditActivityBackPath: vi.fn(() => "/activities"),
}));

vi.mock(
  "~/components/Activity/Edit/EditActivityForm/EditActivityForm.handlers",
  () => ({ fetchGroups: vi.fn() }),
);

const { requireTokenParsed } = vi.hoisted(() => ({
  requireTokenParsed: vi.fn(),
}));
vi.mock("~/util/loaderAuth.util", () => ({ requireTokenParsed }));

const { useLoaderData } = vi.hoisted(() => ({ useLoaderData: vi.fn() }));
vi.mock("react-router", async (importOriginal) => ({
  ...(await importOriginal<typeof import("react-router")>()),
  useLoaderData,
}));

vi.mock("~/components/Activity/Edit/EditActivityForm/EditActivityForm", () => ({
  default: ({
    isBoard,
    id,
    groups,
  }: {
    isBoard: boolean;
    id?: string;
    groups: { name: string }[];
  }) => (
    <div>
      edit-activity-form-{isBoard ? "board" : "member"}-{id ?? "new"}-groups-
      {groups.map((g) => g.name).join(",")}
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
const boardToken: TokenParsed = { ...memberToken, is_admin: true };
const groups = [{ id: 1, name: "BaCo" } as GroupResponseDto];

function renderCreate(loaderData: object) {
  useLoaderData.mockReturnValue(loaderData);
  return renderWithProviders(
    <Routes>
      <Route path="/activities/create" element={<ActivityFormPage />} />
    </Routes>,
    { route: "/activities/create" },
  );
}

function renderEdit(loaderData: object) {
  useLoaderData.mockReturnValue(loaderData);
  return renderWithProviders(
    <Routes>
      <Route path="/activities/edit/:id" element={<ActivityFormPage />} />
    </Routes>,
    { route: "/activities/edit/5" },
  );
}

describe("edit activity clientLoader", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    requireTokenParsed.mockResolvedValue(memberToken);
    vi.mocked(fetchGroups).mockResolvedValue(groups);
  });

  it("loads the activity to edit together with the groups", async () => {
    const activity = { id: 5, name: "Party" } as ActivityResponseDto;
    vi.mocked(fetchEditActivity).mockResolvedValue(activity);

    const result = await clientLoader({ params: { id: "5" } });

    expect(fetchEditActivity).toHaveBeenCalledWith("5");
    expect(result).toEqual({ tokenParsed: memberToken, activity, groups });
  });

  it("loads no activity when creating, but still the groups", async () => {
    const result = await clientLoader({ params: {} });

    expect(fetchEditActivity).not.toHaveBeenCalled();
    expect(result.activity).toBeNull();
    expect(result.groups).toBe(groups);
  });

  it("propagates a failed activity load to React Router's error boundary", async () => {
    vi.mocked(fetchEditActivity).mockRejectedValue(new Error("fail"));

    await expect(clientLoader({ params: { id: "5" } })).rejects.toThrow("fail");
  });

  it("propagates a failed groups load to React Router's error boundary", async () => {
    vi.mocked(fetchGroups).mockRejectedValue(new Error("no groups"));

    await expect(clientLoader({ params: {} })).rejects.toThrow("no groups");
  });
});

describe("ActivityFormPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders the create-activity heading and form in create mode", () => {
    renderCreate({ tokenParsed: memberToken, activity: null, groups });

    expect(screen.getByText("create_activity")).toBeInTheDocument();
    expect(
      screen.getByText("edit-activity-form-member-new-groups-BaCo"),
    ).toBeInTheDocument();
  });

  it("shows admin tools for a board member editing an activity", () => {
    renderEdit({
      tokenParsed: boardToken,
      activity: { id: 5, name: "Party" } as ActivityResponseDto,
      groups,
    });

    expect(
      screen.getByText("edit-activity-form-board-5-groups-BaCo"),
    ).toBeInTheDocument();
    expect(screen.getByText("send-activity-mail")).toBeInTheDocument();
    expect(screen.getByText("edit-participants-tile")).toBeInTheDocument();
  });

  it("does not show admin tools for a non-board member editing an activity", () => {
    renderEdit({
      tokenParsed: memberToken,
      activity: { id: 5, name: "Party" } as ActivityResponseDto,
      groups,
    });

    expect(
      screen.getByText("edit-activity-form-member-5-groups-BaCo"),
    ).toBeInTheDocument();
    expect(screen.queryByText("send-activity-mail")).not.toBeInTheDocument();
  });

  it("uses getEditActivityBackPath for the header back link", () => {
    renderCreate({ tokenParsed: memberToken, activity: null, groups });

    expect(getEditActivityBackPath).toHaveBeenCalled();
  });
});
