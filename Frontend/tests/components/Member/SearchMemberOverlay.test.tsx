import { fireEvent, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { MemberResponseDto } from "~/api";
import SearchMemberOverlay from "~/components/Member/SearchMemberOverlay";

const { getMembers } = vi.hoisted(() => ({ getMembers: vi.fn() }));

vi.mock("~/api", () => ({ getMembers }));
vi.mock("react-hot-toast", () => ({ default: { error: vi.fn() } }));

const member: MemberResponseDto = {
  id: "00000000-0000-0000-0000-000000000000",
  firstName: "Alice",
  lastName: "Smith",
} as MemberResponseDto;

describe("SearchMemberOverlay", () => {
  beforeEach(() => {
    getMembers.mockResolvedValue({ data: [] });
  });

  it("shows a searching indicator, then results, after the debounce delay", async () => {
    getMembers.mockResolvedValue({ data: [member] });
    render(
      <SearchMemberOverlay
        selectText="Add"
        onSelect={vi.fn()}
        loading={false}
      />,
    );

    expect(screen.getByText("searching")).toBeInTheDocument();

    // A single atomic change event (rather than per-keystroke userEvent.type) avoids racing
    // the 300ms debounce against individual keystrokes.
    fireEvent.change(screen.getByPlaceholderText("search_member_placeholder"), {
      target: { value: "Al" },
    });

    expect(
      await screen.findByText("Alice Smith", {}, { timeout: 2000 }),
    ).toBeInTheDocument();
    expect(getMembers).toHaveBeenLastCalledWith({ query: { Search: "Al" } });
  });

  it("shows a not-found message once search settles with no results", async () => {
    render(
      <SearchMemberOverlay
        selectText="Add"
        onSelect={vi.fn()}
        loading={false}
      />,
    );

    expect(await screen.findByText("no_members_found")).toBeInTheDocument();
  });

  it("calls onSelect with the chosen member", async () => {
    getMembers.mockResolvedValue({ data: [member] });
    const user = userEvent.setup();
    const onSelect = vi.fn();
    render(
      <SearchMemberOverlay
        selectText="Add"
        onSelect={onSelect}
        loading={false}
      />,
    );

    await user.click(await screen.findByRole("button", { name: "Add" }));

    expect(onSelect).toHaveBeenCalledWith(member);
  });

  it("disables the select button while loading", async () => {
    getMembers.mockResolvedValue({ data: [member] });
    render(
      <SearchMemberOverlay
        selectText="Add"
        onSelect={vi.fn()}
        loading={true}
      />,
    );

    expect(await screen.findByRole("button", { name: "Add" })).toBeDisabled();
  });

  it("shows an error message and toast when the search fails", async () => {
    getMembers.mockResolvedValue({ error: { title: "Boom" } });
    const toast = (await import("react-hot-toast")).default;

    render(
      <SearchMemberOverlay
        selectText="Add"
        onSelect={vi.fn()}
        loading={false}
      />,
    );

    await screen.findByText("no_members_found");
    expect(toast.error).toHaveBeenCalled();
  });
});
