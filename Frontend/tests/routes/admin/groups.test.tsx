import { fireEvent, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { GroupResponseDto } from "~/api";
import Groups from "~/routes/admin/groups";
import { createMockAuthService, renderWithProviders } from "~/testUtils";
import type { TokenParsed } from "~/types/TokenParsed";

const boardAuthService = createMockAuthService({
  getTokenParsed: vi.fn(
    async () =>
      ({
        locale: "en",
        UserId: "00000000-0000-0000-0000-000000000000" as TokenParsed["UserId"],
        access_level: "member",
        given_name: "Board",
        family_name: "Member",
        name: "Board Member",
        is_admin: true,
      }) satisfies TokenParsed,
  ),
});

const { getGroups } = vi.hoisted(() => ({
  getGroups: vi.fn(),
}));

vi.mock("~/api", () => ({ getGroups }));

const toastErrorFn = vi.fn();
vi.mock("react-hot-toast", () => ({
  default: { error: (...args: unknown[]) => toastErrorFn(...args) },
}));

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

describe("Groups", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("shows a loading state, then the table once groups have loaded", async () => {
    getGroups.mockResolvedValue({ data: [makeGroup()] });
    renderWithProviders(<Groups />);

    expect(screen.getByText("loading")).toBeInTheDocument();
    expect(await screen.findByText("Board")).toBeInTheDocument();
  });

  it("shows an error toast when groups fail to load", async () => {
    getGroups.mockResolvedValue({ error: "fail" });
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});

    renderWithProviders(<Groups />);

    await waitFor(() => expect(toastErrorFn).toHaveBeenCalled());
    consoleError.mockRestore();
  });

  it("filters groups by name or type as the search query changes", async () => {
    getGroups.mockResolvedValue({
      data: [
        makeGroup({ id: 1, name: "Board", type: "Committee" }),
        makeGroup({ id: 2, name: "Party Committee", type: "WorkingGroup" }),
      ],
    });
    renderWithProviders(<Groups />);

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
    getGroups.mockResolvedValue({ data: [makeGroup()] });
    renderWithProviders(<Groups />);

    await screen.findByText("Board");
    fireEvent.click(screen.getAllByText("view_group")[0]);
  });

  it("opens the create-group modal when the plus button is clicked", async () => {
    getGroups.mockResolvedValue({ data: [] });
    renderWithProviders(<Groups />, { authService: boardAuthService });

    await screen.findByText("loading");
    const plusButton = document
      .querySelector("svg.lucide-plus")
      ?.closest("button");
    expect(plusButton).toBeTruthy();

    fireEvent.click(plusButton!);
    expect(await screen.findByText("create-group-overlay")).toBeInTheDocument();
  });
});
