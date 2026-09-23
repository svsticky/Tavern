import { fireEvent, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { GroupResponseDto } from "~/api";
import Groups, { clientLoader } from "~/routes/admin/groups";
import { renderWithProviders } from "~/testUtils";

const { getGroups } = vi.hoisted(() => ({
  getGroups: vi.fn(),
}));
vi.mock("~/api", () => ({ getGroups }));

const { requireTokenParsed } = vi.hoisted(() => ({
  requireTokenParsed: vi.fn(),
}));
vi.mock("~/util/loaderAuth.util", () => ({ requireTokenParsed }));

const { useLoaderData } = vi.hoisted(() => ({ useLoaderData: vi.fn() }));
vi.mock("react-router", async (importOriginal) => ({
  ...(await importOriginal<typeof import("react-router")>()),
  useLoaderData,
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

describe("admin groups clientLoader", () => {
  it("waits for auth and returns the fetched groups", async () => {
    requireTokenParsed.mockResolvedValue({ UserId: "user-1" });
    getGroups.mockResolvedValue({ data: [makeGroup()] });

    await expect(clientLoader()).resolves.toEqual({ groups: [makeGroup()] });
    expect(requireTokenParsed).toHaveBeenCalled();
  });

  it("throws when groups fail to load", async () => {
    requireTokenParsed.mockResolvedValue({ UserId: "user-1" });
    getGroups.mockResolvedValue({ error: "fail" });

    await expect(clientLoader()).rejects.toBe("fail");
  });
});

describe("Groups", () => {
  it("renders the table with loaded groups", () => {
    useLoaderData.mockReturnValue({ groups: [makeGroup()] });
    renderWithProviders(<Groups />);

    expect(screen.getByText("Board")).toBeInTheDocument();
  });

  it("filters groups by name or type as the search query changes", () => {
    useLoaderData.mockReturnValue({
      groups: [
        makeGroup({ id: 1, name: "Board", type: "Committee" }),
        makeGroup({ id: 2, name: "Party Committee", type: "WorkingGroup" }),
      ],
    });
    renderWithProviders(<Groups />);

    fireEvent.change(screen.getByLabelText("search"), {
      target: { value: "working" },
    });

    expect(screen.queryByText("Board")).not.toBeInTheDocument();
    expect(screen.getByText("Party Committee")).toBeInTheDocument();
  });

  it("navigates to a group's detail page when 'view_group' is clicked", () => {
    useLoaderData.mockReturnValue({ groups: [makeGroup()] });
    renderWithProviders(<Groups />);

    fireEvent.click(screen.getAllByText("view_group")[0]);
  });

  it("opens the create-group modal when the plus button is clicked", async () => {
    useLoaderData.mockReturnValue({ groups: [] });
    renderWithProviders(<Groups />);

    const plusButton = document
      .querySelector("svg.lucide-plus")
      ?.closest("button");
    expect(plusButton).toBeTruthy();

    fireEvent.click(plusButton!);
    expect(await screen.findByText("create-group-overlay")).toBeInTheDocument();
  });
});
