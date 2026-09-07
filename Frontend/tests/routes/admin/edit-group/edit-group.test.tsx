import { fireEvent, screen, waitFor, within } from "@testing-library/react";
import { useEffect } from "react";
import { Route, Routes } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { GroupMembershipResponseDto } from "~/api";
import { useApp } from "~/context/AppContext";
import { createMockAuthService, renderWithProviders } from "~/testUtils";
import type { TokenParsed } from "~/types/TokenParsed";

const { getGroupsByIdPermissions, putGroupsByIdPermissions } = vi.hoisted(
  () => ({
    getGroupsByIdPermissions: vi.fn(),
    putGroupsByIdPermissions: vi.fn(),
  }),
);

vi.mock("~/api", () => ({
  getGroupsByIdPermissions,
  putGroupsByIdPermissions,
}));

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

const {
  loadGroupData,
  loadGroupMemberships,
  handleSaveGroup,
  handleGroupProfilePictureUpload,
  handleDeleteGroupEnrollment,
  handleAddGroupEnrollment,
  handleUpdateGroupRole,
  handleRoleAliasAdded,
} = vi.hoisted(() => ({
  loadGroupData: vi.fn(async ({ setLoading }: any) => {
    setLoading(false);
  }),
  loadGroupMemberships: vi.fn(),
  handleSaveGroup: vi.fn(),
  handleGroupProfilePictureUpload: vi.fn(),
  handleDeleteGroupEnrollment: vi.fn(),
  handleAddGroupEnrollment: vi.fn(),
  handleUpdateGroupRole: vi.fn(),
  handleRoleAliasAdded: vi.fn(),
}));

vi.mock("~/routes/admin/edit-group/edit-group.handlers", () => ({
  loadGroupData,
  loadGroupMemberships,
  handleSaveGroup,
  handleGroupProfilePictureUpload,
  handleDeleteGroupEnrollment,
  handleAddGroupEnrollment,
  handleUpdateGroupRole,
  handleRoleAliasAdded,
}));

vi.mock("~/components/Member/SearchMemberOverlay", () => ({
  default: ({ onSelect }: { onSelect: (m: any) => void }) => (
    <button type="button" onClick={() => onSelect({ id: "m1" })}>
      select-member
    </button>
  ),
}));

vi.mock("~/components/Roles/CreateRoleOverlay/CreateRoleOverlay", () => ({
  default: ({
    onRoleAliasCreated,
    onRoleCreated,
  }: {
    onRoleAliasCreated: (r: any) => void;
    onRoleCreated: (r: any) => void;
  }) => (
    <>
      <button
        type="button"
        onClick={() => onRoleAliasCreated({ id: 9, name: "New Role" })}
      >
        create-role
      </button>
      <button
        type="button"
        onClick={() => onRoleCreated({ id: 10, name: "Parent Role" })}
      >
        create-parent-role
      </button>
    </>
  ),
}));

import EditGroupPage from "~/routes/admin/edit-group/edit-group";

function renderPage(id = 1, authService = createMockAuthService()) {
  return renderWithProviders(
    <Routes>
      <Route path="/admin/groups/:id" element={<EditGroupPage />} />
    </Routes>,
    { route: `/admin/groups/${id}`, authService },
  );
}

// EditGroupPage reads boardGroupId from AppContext; in the real app it's populated by the
// authenticated layout further up the tree. This sets it directly on the same AppProvider
// instance so the board-group permission-lock behavior can be exercised.
function renderPageAsBoardGroup(
  id: number,
  authService: ReturnType<typeof createMockAuthService>,
) {
  function Wrapper() {
    const { setBoardGroupId } = useApp();
    useEffect(() => {
      setBoardGroupId(id);
    }, [setBoardGroupId, id]);
    return (
      <Routes>
        <Route path="/admin/groups/:id" element={<EditGroupPage />} />
      </Routes>
    );
  }
  return renderWithProviders(<Wrapper />, {
    route: `/admin/groups/${id}`,
    authService,
  });
}

function enrollment(
  overrides: Partial<GroupMembershipResponseDto> = {},
): GroupMembershipResponseDto {
  return {
    id: 1,
    memberName: "Jane Doe",
    memberId: "m1",
    groupId: 1,
    groupName: "Board",
    groupType: "Committee",
    membershipYear: 2024,
    roleAliasId: null,
    ...overrides,
  } as GroupMembershipResponseDto;
}

describe("EditGroupPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    loadGroupData.mockImplementation(
      async ({ setLoading, setRoleAliases }: any) => {
        setRoleAliases([{ id: 2, name: "Chair" }]);
        setLoading(false);
      },
    );
    loadGroupMemberships.mockImplementation(
      async (
        _id: number,
        _year: number,
        setLoadingMemberships: (v: boolean) => void,
        setEnrollments: (v: GroupMembershipResponseDto[]) => void,
      ) => {
        setEnrollments([enrollment()]);
        setLoadingMemberships(false);
      },
    );
  });

  it("shows a loading indicator while loading, then renders the form", async () => {
    let resolveLoad: (() => void) | undefined;
    loadGroupData.mockImplementation(
      ({ setLoading }: any) =>
        new Promise<void>((resolve) => {
          resolveLoad = () => {
            setLoading(false);
            resolve();
          };
        }),
    );

    renderPage(1);

    expect(screen.getByText("loading")).toBeInTheDocument();

    resolveLoad?.();

    await waitFor(() =>
      expect(screen.queryByText("loading")).not.toBeInTheDocument(),
    );
  });

  it("loads group data and memberships for the given id", async () => {
    renderPage(7);

    await waitFor(() => expect(loadGroupData).toHaveBeenCalled());
    expect(loadGroupData.mock.calls[0][0]).toMatchObject({ id: 7 });
    expect(loadGroupMemberships).toHaveBeenCalledWith(
      7,
      expect.any(Number),
      expect.any(Function),
      expect.any(Function),
    );
  });

  it("renders enrollments in the memberships table", async () => {
    renderPage(1);

    expect(await screen.findByText("Jane Doe")).toBeInTheDocument();
  });

  it("saves the group when the save button is clicked", async () => {
    renderPage(1);

    const saveButton = await screen.findByRole("button", { name: "save" });
    fireEvent.click(saveButton);

    expect(handleSaveGroup).toHaveBeenCalledWith(
      1,
      expect.any(Object),
      expect.any(Function),
    );
  });

  it("deletes an enrollment when remove is clicked", async () => {
    renderPage(1);

    const removeButtons = await screen.findAllByText("remove");
    fireEvent.click(removeButtons[0]);

    expect(handleDeleteGroupEnrollment).toHaveBeenCalledWith(
      1,
      expect.any(Function),
      expect.any(Function),
    );
  });

  it("opens the add-enrollment modal and adds a member", async () => {
    renderPage(1);

    await screen.findByText("Jane Doe");
    // The plus-icon button has no accessible name; find it via its container.
    const plusButton = document
      .querySelector("svg.lucide-plus")
      ?.closest("button");
    expect(plusButton).toBeTruthy();
    fireEvent.click(plusButton as HTMLButtonElement);

    const selectMemberButton = await screen.findByText("select-member");
    fireEvent.click(selectMemberButton);

    expect(handleAddGroupEnrollment).toHaveBeenCalledWith(
      1,
      { id: "m1" },
      expect.any(Number),
      expect.any(Function),
      expect.any(Function),
      expect.any(Function),
    );
  });

  it("updates a member's role when the role select changes", async () => {
    renderPage(1);

    await screen.findByText("Jane Doe");
    // The year selector in the header also renders as a <select> (twice, for the
    // desktop/mobile DataTableTile variants) - find the row's role select specifically by its
    // "Chair" option.
    const roleSelect = screen
      .getAllByRole("combobox")
      .find((el) => within(el).queryByText("Chair"));
    expect(roleSelect).toBeTruthy();
    fireEvent.change(roleSelect as HTMLSelectElement, {
      target: { value: "2" },
    });

    expect(handleUpdateGroupRole).toHaveBeenCalledWith(
      1,
      2,
      expect.any(Function),
      expect.any(Function),
    );
  });

  it("creates a role alias via the add-role modal", async () => {
    renderPage(1, boardAuthService);

    const addRoleButton = await screen.findByText("add_role");
    fireEvent.click(addRoleButton);

    const createRoleButton = await screen.findByText("create-role");
    fireEvent.click(createRoleButton);

    expect(handleRoleAliasAdded).toHaveBeenCalledWith(
      { id: 9, name: "New Role" },
      expect.any(Function),
      expect.any(Function),
    );
  });

  it("uploads a profile picture when a file is chosen", async () => {
    renderPage(1);

    await waitFor(() => expect(loadGroupData).toHaveBeenCalled());
    const fileInput = document.querySelector(
      'input[type="file"]',
    ) as HTMLInputElement;
    const file = new File(["x"], "pic.png", { type: "image/png" });
    fireEvent.change(fileInput, { target: { files: [file] } });

    expect(handleGroupProfilePictureUpload).toHaveBeenCalled();
  });

  it("opens the file picker when the profile picture is clicked", async () => {
    renderPage(1);

    await waitFor(() => expect(loadGroupData).toHaveBeenCalled());
    const fileInput = document.querySelector(
      'input[type="file"]',
    ) as HTMLInputElement;
    const clickSpy = vi.spyOn(fileInput, "click");

    fireEvent.click(screen.getByAltText("Profile").closest("div")!);

    expect(clickSpy).toHaveBeenCalled();
  });

  it("changes the membership year and reloads memberships", async () => {
    renderPage(1);

    await screen.findByText("Jane Doe");
    loadGroupMemberships.mockClear();

    const yearSelect = screen
      .getAllByRole("combobox")
      .find((el) =>
        Array.from((el as HTMLSelectElement).options).some((o) =>
          /^\d{4}-\d{4}$/.test(o.textContent ?? ""),
        ),
      ) as HTMLSelectElement;
    expect(yearSelect).toBeTruthy();
    const otherOption = Array.from(yearSelect.options).find(
      (o) => o.value !== yearSelect.value,
    );
    fireEvent.change(yearSelect, {
      target: { value: otherOption?.value ?? "" },
    });

    await waitFor(() => expect(loadGroupMemberships).toHaveBeenCalled());
  });

  it("updates group info fields as the user edits them", async () => {
    renderPage(1);
    // group_name is a required field, so its accessible label text also includes the
    // asterisk RequiredAsterisk renders ("group_name*") - match loosely instead of exactly.
    await screen.findByLabelText("group_name", { exact: false });

    fireEvent.change(screen.getByLabelText("group_name", { exact: false }), {
      target: { value: "New name" },
    });
    expect(screen.getByLabelText("group_name", { exact: false })).toHaveValue(
      "New name",
    );

    fireEvent.change(screen.getByLabelText("group_type"), {
      target: { value: "Dispute" },
    });
    expect(screen.getByLabelText("group_type")).toHaveValue("Dispute");

    fireEvent.change(screen.getByLabelText("gl_account_id"), {
      target: { value: "GL1" },
    });
    expect(screen.getByLabelText("gl_account_id")).toHaveValue("GL1");

    fireEvent.change(screen.getByLabelText("cost_unit_id"), {
      target: { value: "CU1" },
    });
    expect(screen.getByLabelText("cost_unit_id")).toHaveValue("CU1");

    const activeCheckbox = document.querySelector(
      'input[type="checkbox"]',
    ) as HTMLInputElement;
    const before = activeCheckbox.checked;
    fireEvent.click(activeCheckbox);
    expect(activeCheckbox.checked).toBe(!before);
  });

  it("shows a numeric role select value when the enrollment has a role alias", async () => {
    loadGroupMemberships.mockImplementation(
      async (
        _id: number,
        _year: number,
        setLoadingMemberships: (v: boolean) => void,
        setEnrollments: (v: GroupMembershipResponseDto[]) => void,
      ) => {
        setEnrollments([enrollment({ roleAliasId: 2 })]);
        setLoadingMemberships(false);
      },
    );
    renderPage(1);

    await screen.findByText("Jane Doe");
    const roleSelect = screen
      .getAllByRole("combobox")
      .find((el) => within(el).queryByText("Chair")) as HTMLSelectElement;
    expect(roleSelect.value).toBe("2");
  });

  it("shows a full-size profile picture once one has loaded", async () => {
    loadGroupData.mockImplementation(
      async ({ setLoading, setRoleAliases, setGroupPictureSrc }: any) => {
        setRoleAliases([{ id: 2, name: "Chair" }]);
        setGroupPictureSrc("blob:group-picture");
        setLoading(false);
      },
    );
    renderPage(1);

    const img = await screen.findByAltText("Profile");
    expect(img).toHaveAttribute("src", "blob:group-picture");
    expect(img.className).toContain("object-contain");
  });

  it("shows a saving label on the save button while saving", async () => {
    handleSaveGroup.mockImplementation(
      (_id: number, _data: any, setSaving: (v: boolean) => void) => {
        setSaving(true);
      },
    );
    renderPage(1);

    const saveButton = await screen.findByRole("button", { name: "save" });
    fireEvent.click(saveButton);

    expect(await screen.findByText("saving")).toBeInTheDocument();
  });

  it("shows an empty memberships table while memberships are loading", async () => {
    let resolveMemberships: (() => void) | undefined;
    loadGroupMemberships.mockImplementation(
      (
        _id: number,
        _year: number,
        setLoadingMemberships: (v: boolean) => void,
      ) =>
        new Promise<void>((resolve) => {
          setLoadingMemberships(true);
          resolveMemberships = () => {
            setLoadingMemberships(false);
            resolve();
          };
        }),
    );
    renderPage(1);

    await waitFor(() =>
      expect(screen.getByText("no_enrollments_found")).toBeInTheDocument(),
    );
    resolveMemberships?.();
  });

  it("closes the add-enrollment modal without adding a member", async () => {
    renderPage(1);

    await screen.findByText("Jane Doe");
    const plusButton = document
      .querySelector("svg.lucide-plus")
      ?.closest("button");
    fireEvent.click(plusButton!);
    expect(await screen.findByText("select-member")).toBeInTheDocument();

    fireEvent.keyDown(window, { key: "Escape" });

    await waitFor(() =>
      expect(screen.queryByText("select-member")).not.toBeInTheDocument(),
    );
  });

  it("closes the add-role modal after creating a parent role", async () => {
    renderPage(1, boardAuthService);

    await screen.findByText("Jane Doe");
    fireEvent.click(screen.getByText("add_role"));
    fireEvent.click(await screen.findByText("create-parent-role"));

    await waitFor(() =>
      expect(screen.queryByText("create-parent-role")).not.toBeInTheDocument(),
    );
  });

  it("renders a link to the member's admin details page for each enrolled member", async () => {
    renderPage(1);

    const memberLink = await screen.findByRole("link", { name: "Jane Doe" });
    expect(memberLink).toHaveAttribute("href", "/admin/members/m1");
  });

  it("hides the permissions section without ManageGroupPermissions", async () => {
    renderPage(1);

    await screen.findByText("Jane Doe");
    expect(screen.queryByText("permissions")).not.toBeInTheDocument();
  });

  it("hides the add_role button without ManageRoles", async () => {
    renderPage(1);

    await screen.findByText("Jane Doe");
    expect(screen.queryByText("add_role")).not.toBeInTheDocument();
  });

  it("loads and saves group permissions for a board member", async () => {
    getGroupsByIdPermissions.mockResolvedValue({ data: ["ManageGroups"] });
    putGroupsByIdPermissions.mockResolvedValue({});

    renderPage(1, boardAuthService);

    await screen.findByText("Jane Doe");
    expect(await screen.findByText("permissions")).toBeInTheDocument();
    expect(await screen.findByLabelText("Manage Groups")).toBeChecked();
    expect(getGroupsByIdPermissions).toHaveBeenCalledWith({
      path: { id: 1 },
    });

    fireEvent.click(screen.getByText("save_permissions"));

    await waitFor(() =>
      expect(putGroupsByIdPermissions).toHaveBeenCalledWith({
        path: { id: 1 },
        body: ["ManageGroups"],
      }),
    );
  });

  it("locks all known permissions on for the board group and excludes them from the save payload", async () => {
    getGroupsByIdPermissions.mockResolvedValue({ data: [] });
    putGroupsByIdPermissions.mockResolvedValue({});

    renderPageAsBoardGroup(1, boardAuthService);

    await screen.findByText("Jane Doe");
    const checkbox = await screen.findByLabelText("Manage Groups");
    expect(checkbox).toBeChecked();
    expect(checkbox).toBeDisabled();
    expect(
      screen.getByText("all_permissions_granted_note"),
    ).toBeInTheDocument();

    fireEvent.click(screen.getByText("save_permissions"));

    await waitFor(() =>
      expect(putGroupsByIdPermissions).toHaveBeenCalledWith({
        path: { id: 1 },
        body: [],
      }),
    );
  });
});
