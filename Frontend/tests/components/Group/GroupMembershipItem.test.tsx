import { fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router";
import { describe, expect, it } from "vitest";
import type { GroupMembershipResponseDto } from "~/api";
import GroupMembershipItem from "~/components/Group/GroupMembershipItem";

const membership: GroupMembershipResponseDto = {
  id: 1,
  groupId: 2,
  groupName: "Web Committee",
  membershipYear: 2026,
  roleAliasName: "Chair",
  memberName: "Alice",
} as GroupMembershipResponseDto;

describe("GroupMembershipItem", () => {
  it("renders the group name, academic year range, and role", () => {
    render(
      <GroupMembershipItem
        groupMembership={membership}
        fallbackUrl="/fallback.svg"
      />,
    );

    expect(screen.getByText("Web Committee - 2025/2026")).toBeInTheDocument();
    expect(screen.getByText("Chair")).toBeInTheDocument();
  });

  it("falls back to the provided fallback image when the group picture fails to load", () => {
    render(
      <GroupMembershipItem
        groupMembership={membership}
        fallbackUrl="/fallback.svg"
      />,
    );

    const img = screen.getByAltText("Alice Icon");
    fireEvent.error(img);

    expect(img).toHaveAttribute("src", "/fallback.svg");
  });

  it("does not render a link when linkToGroup is not set", () => {
    render(
      <GroupMembershipItem
        groupMembership={membership}
        fallbackUrl="/fallback.svg"
      />,
    );

    expect(screen.queryByRole("link")).not.toBeInTheDocument();
  });

  it("links to the group's admin page when linkToGroup is true", () => {
    render(
      <MemoryRouter>
        <GroupMembershipItem
          groupMembership={membership}
          fallbackUrl="/fallback.svg"
          linkToGroup
        />
      </MemoryRouter>,
    );

    expect(screen.getByRole("link")).toHaveAttribute("href", "/admin/groups/2");
  });
});
