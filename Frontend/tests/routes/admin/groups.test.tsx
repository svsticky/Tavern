import { fireEvent, screen, waitFor } from "@testing-library/react";
import { data } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { GroupResponseDto } from "~/api";
import * as route from "~/routes/admin/groups";
import { renderRoute } from "~/testUtils";

const { getGroups, requireBoardOrCandidateBoard } = vi.hoisted(() => ({
  getGroups: vi.fn(),
  requireBoardOrCandidateBoard: vi.fn(),
}));

vi.mock("~/api", () => ({ getGroups }));
vi.mock("~/util/loaderAuth.util", () => ({ requireBoardOrCandidateBoard }));

vi.mock("~/components/Group/CreateGroupOverlay/CreateGroupOverlay", () => ({
  default: () => <div>create-group-overlay</div>,
}));

function makeGroup(
  overrides: Partial<GroupResponseDto> = {},
): GroupResponseDto {
  return {
    id: 1,
    name: "Board",
    type: "Committee",
    ...overrides,
  } as GroupResponseDto;
}

/** Renders the groups page, with a stub for the group detail page. */
function renderGroups() {
  return renderRoute(route, {
    path: "/admin/groups",
    otherRoutes: [
      { path: "/admin/groups/:id", Component: () => <div>group-page</div> },
    ],
  });
}

describe("Groups", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    requireBoardOrCandidateBoard.mockResolvedValue({});
    getGroups.mockResolvedValue({ data: [makeGroup()] });
  });

  it("shows the table once the groups have loaded", async () => {
    renderGroups();

    expect(await screen.findByText("Board")).toBeInTheDocument();
  });

  it("shows the error page when the groups fail to load", async () => {
    getGroups.mockResolvedValue({ error: new Error("Failed to fetch groups") });
    renderGroups();

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Failed to fetch groups",
    );
  });

  it("shows the forbidden page for members outside the board", async () => {
    requireBoardOrCandidateBoard.mockRejectedValue(
      data("Forbidden", { status: 403 }),
    );
    renderGroups();

    expect(await screen.findByRole("alert")).toHaveTextContent("403");
    expect(getGroups).not.toHaveBeenCalled();
  });

  it("filters groups by name or type as the search query changes", async () => {
    getGroups.mockResolvedValue({
      data: [
        makeGroup({ id: 1, name: "Board", type: "Committee" }),
        makeGroup({ id: 2, name: "Party Committee", type: "WorkingGroup" }),
      ],
    });
    renderGroups();

    await screen.findByText("Board");
    fireEvent.change(screen.getByLabelText("search"), {
      target: { value: "working" },
    });

    await waitFor(() =>
      expect(screen.queryByText("Board")).not.toBeInTheDocument(),
    );
    expect(screen.getByText("Party Committee")).toBeInTheDocument();
  });

  it("navigates to a group's detail page when 'view_group' is clicked", async () => {
    renderGroups();

    fireEvent.click((await screen.findAllByText("view_group"))[0]);

    expect(await screen.findByText("group-page")).toBeInTheDocument();
  });

  it("opens the create-group modal when the plus button is clicked", async () => {
    getGroups.mockResolvedValue({ data: [] });
    renderGroups();

    await screen.findByLabelText("search");
    const plusButton = document
      .querySelector("svg.lucide-plus")
      ?.closest("button");
    expect(plusButton).toBeTruthy();

    fireEvent.click(plusButton!);
    expect(await screen.findByText("create-group-overlay")).toBeInTheDocument();
  });
});
