import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { RoleAlias } from "~/api";
import CreateRoleOverlay from "~/components/Roles/CreateRoleOverlay/CreateRoleOverlay";
import {
  fetchRoles,
  handleCreateRoleSubmit,
} from "~/components/Roles/CreateRoleOverlay/CreateRoleOverlay.handlers";

vi.mock(
  "~/components/Roles/CreateRoleOverlay/CreateRoleOverlay.handlers",
  () => ({
    fetchRoles: vi.fn(),
    handleCreateRoleSubmit: vi.fn(),
  }),
);

describe("CreateRoleOverlay", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("fetches roles on mount and populates the parent-role select", async () => {
    vi.mocked(fetchRoles).mockImplementation(
      async (setLoadingRoles, setRoles) => {
        setRoles([{ id: 1, name: "Chair" } as RoleAlias]);
        setLoadingRoles(false);
      },
    );
    render(
      <CreateRoleOverlay
        onRoleAliasCreated={vi.fn()}
        onRoleCreated={vi.fn()}
      />,
    );

    await waitFor(() => expect(screen.getByText("Chair")).toBeInTheDocument());
  });

  it("hides the parent-role select when creating a ParentRole", async () => {
    vi.mocked(fetchRoles).mockImplementation(
      async (setLoadingRoles, setRoles) => {
        setRoles([]);
        setLoadingRoles(false);
      },
    );
    render(
      <CreateRoleOverlay
        onRoleAliasCreated={vi.fn()}
        onRoleCreated={vi.fn()}
      />,
    );

    await waitFor(() => expect(fetchRoles).toHaveBeenCalled());
    fireEvent.change(screen.getByLabelText("type"), {
      target: { value: "ParentRole" },
    });
    expect(screen.queryByLabelText("parent_role")).not.toBeInTheDocument();
  });

  it("disables the create button until a name is entered", async () => {
    vi.mocked(fetchRoles).mockImplementation(
      async (setLoadingRoles, setRoles) => {
        setRoles([]);
        setLoadingRoles(false);
      },
    );
    render(
      <CreateRoleOverlay
        onRoleAliasCreated={vi.fn()}
        onRoleCreated={vi.fn()}
      />,
    );

    await waitFor(() => expect(fetchRoles).toHaveBeenCalled());
    fireEvent.change(screen.getByLabelText("type"), {
      target: { value: "ParentRole" },
    });
    expect(screen.getByText("create")).toBeDisabled();

    fireEvent.change(screen.getByPlaceholderText("name"), {
      target: { value: "Chair" },
    });
    expect(screen.getByText("create")).not.toBeDisabled();
  });

  it("keeps create disabled for RoleAlias until a parent role is selected", async () => {
    vi.mocked(fetchRoles).mockImplementation(
      async (setLoadingRoles, setRoles) => {
        setRoles([{ id: 1, name: "Chair" } as RoleAlias]);
        setLoadingRoles(false);
      },
    );
    render(
      <CreateRoleOverlay
        onRoleAliasCreated={vi.fn()}
        onRoleCreated={vi.fn()}
      />,
    );

    await waitFor(() => expect(fetchRoles).toHaveBeenCalled());
    fireEvent.change(screen.getByPlaceholderText("name"), {
      target: { value: "Vice Chair" },
    });
    expect(screen.getByText("create")).toBeDisabled();

    fireEvent.change(screen.getByLabelText("parent_role"), {
      target: { value: "1" },
    });
    expect(screen.getByText("create")).not.toBeDisabled();
  });

  it("calls handleCreateRoleSubmit on form submission", async () => {
    vi.mocked(fetchRoles).mockImplementation(
      async (setLoadingRoles, setRoles) => {
        setRoles([{ id: 1, name: "Chair" } as RoleAlias]);
        setLoadingRoles(false);
      },
    );
    render(
      <CreateRoleOverlay
        onRoleAliasCreated={vi.fn()}
        onRoleCreated={vi.fn()}
      />,
    );

    await waitFor(() => expect(fetchRoles).toHaveBeenCalled());
    fireEvent.change(screen.getByPlaceholderText("name"), {
      target: { value: "Vice Chair" },
    });
    fireEvent.change(screen.getByLabelText("parent_role"), {
      target: { value: "1" },
    });
    fireEvent.click(screen.getByText("create"));

    expect(handleCreateRoleSubmit).toHaveBeenCalledWith(
      expect.objectContaining({
        selectedType: "RoleAlias",
        name: "Vice Chair",
        selectedRoleId: "1",
      }),
    );
  });
});
