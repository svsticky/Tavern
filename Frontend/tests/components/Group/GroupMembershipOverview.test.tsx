import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router";
import { describe, expect, it } from "vitest";
import type { GroupMembershipResponseDto } from "~/api";
import GroupMembershipOverview from "~/components/Group/GroupMembershipOverview";

function membership(
  overrides: Partial<GroupMembershipResponseDto>,
): GroupMembershipResponseDto {
  return {
    id: 1,
    groupId: 1,
    groupName: "Group",
    membershipYear: 2026,
    roleAliasName: "Member",
    memberName: "Alice",
    ...overrides,
  } as GroupMembershipResponseDto;
}

describe("GroupMembershipOverview", () => {
  it("shows a no-content message when there are no memberships", () => {
    render(<GroupMembershipOverview groupMemberships={[]} />);
    expect(screen.getByText("no_group_enrollments")).toBeInTheDocument();
  });

  it("renders each membership, most recent first", () => {
    render(
      <GroupMembershipOverview
        groupMemberships={[
          membership({ id: 1, groupName: "Older Group", membershipYear: 2024 }),
          membership({ id: 2, groupName: "Newer Group", membershipYear: 2026 }),
        ]}
      />,
    );

    const names = screen
      .getAllByText(/Group - \d{4}\/\d{4}/)
      .map((el) => el.textContent);
    expect(names[0]).toContain("Newer Group");
    expect(names[1]).toContain("Older Group");
  });

  it("renders links when isClickable is true", () => {
    render(
      <MemoryRouter>
        <GroupMembershipOverview
          groupMemberships={[
            membership({
              id: 1,
              groupId: 10,
              groupName: "Board",
              membershipYear: 2026,
            }),
          ]}
          isClickable={true}
        />
      </MemoryRouter>,
    );

    const link = screen.getByRole("link");
    expect(link).toHaveAttribute("href", "/admin/groups/10");
  });
});
