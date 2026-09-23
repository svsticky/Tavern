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

const { useLoaderData, revalidate, useRevalidator } = vi.hoisted(() => {
  const revalidate = vi.fn();
  return {
    useLoaderData: vi.fn(),
    revalidate,
    useRevalidator: vi.fn(() => ({ revalidate, state: "idle" })),
  };
});
vi.mock("react-router", async (importOriginal) => ({
  ...(await importOriginal<typeof import("react-router")>()),
  useLoaderData,
  useRevalidator,
}));

vi.mock("~/components/Group/CreateGroupOverlay/CreateGroupOverlay", () => ({
  default: ({ onSuccess }: { onSuccess: () => void }) => (
    <div>
      create-group-overlay
      <button type="button" onClick={onSuccess}>
        simulate-create-success
      </button>
    </div>
  ),
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

  it("revalidates the loader and closes the modal after a group is created, instead of reloading the page", async () => {
    useLoaderData.mockReturnValue({ groups: [] });
    renderWithProviders(<Groups />);

    const plusButton = document
      .querySelector("svg.lucide-plus")
      ?.closest("button");
    fireEvent.click(plusButton!);
    fireEvent.click(await screen.findByText("simulate-create-success"));

    expect(revalidate).toHaveBeenCalledTimes(1);
    expect(screen.queryByText("create-group-overlay")).not.toBeInTheDocument();
  });
});
