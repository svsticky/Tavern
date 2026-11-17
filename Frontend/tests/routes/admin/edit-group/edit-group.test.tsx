import { fireEvent, screen, waitFor, within } from "@testing-library/react";
import { useParams } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { GroupMembershipResponseDto } from "~/api";
import type { EditGroupLoaderData } from "~/routes/admin/edit-group/edit-group.handlers";
import { renderRoute } from "~/testUtils";

const {
  loadEditGroupLoaderData,
  requireBoardOrCandidateBoard,
  loadGroupMemberships,
  handleSaveGroup,
  handleGroupProfilePictureUpload,
  handleDeleteGroupEnrollment,
  handleAddGroupEnrollment,
  handleUpdateGroupRole,
  handleRoleAliasAdded,
} = vi.hoisted(() => ({
  loadEditGroupLoaderData: vi.fn(),
  requireBoardOrCandidateBoard: vi.fn(),
  loadGroupMemberships: vi.fn(),
  handleSaveGroup: vi.fn(),
  handleGroupProfilePictureUpload: vi.fn(),
  handleDeleteGroupEnrollment: vi.fn(),
  handleAddGroupEnrollment: vi.fn(),
  handleUpdateGroupRole: vi.fn(),
  handleRoleAliasAdded: vi.fn(),
}));

vi.mock("~/util/loaderAuth.util", () => ({ requireBoardOrCandidateBoard }));

vi.mock("~/routes/admin/edit-group/edit-group.handlers", () => ({
  loadEditGroupLoaderData,
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

import * as route from "~/routes/admin/edit-group/edit-group";

function MemberPageStub() {
  const { id } = useParams();
  return <div>member-page-{id}</div>;
}

function renderPage(id = 1) {
  return renderRoute(route, {
    path: "/admin/groups/:id",
    route: `/admin/groups/${id}`,
    otherRoutes: [{ path: "/admin/members/:id", Component: MemberPageStub }],
  });
}

/** Makes the loader return group 1 with the given changes. */
function mockLoaderData(overrides: Partial<EditGroupLoaderData> = {}) {
  loadEditGroupLoaderData.mockResolvedValue({
    groupId: 1,
    formData: {
      Name: "Board",
      Type: "Committee",
      Active: true,
      DefaultGLAccount: "",
      DefaultCostCenter: "",
      DefaultCostUnit: "",
    },
    roleAliases: [{ id: 2, name: "Chair" }],
    groupPictureSrc: null,
    committeeYear: 2026,
    enrollments: [enrollment()],
    ...overrides,
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
    requireBoardOrCandidateBoard.mockResolvedValue({});
    mockLoaderData();
    loadGroupMemberships.mockImplementation(
      async (
        _id: number,
        _year: number,
        setLoadingMemberships: (v: boolean) => void,
        setEnrollments: (v: GroupMembershipResponseDto[]) => void,
      ) => {
        setEnrollments([enrollment({ memberName: "John Smith" })]);
        setLoadingMemberships(false);
      },
    );
  });

  it("loads the group from the id in the URL", async () => {
    renderPage(7);

    await screen.findByText("Jane Doe");
    expect(loadEditGroupLoaderData).toHaveBeenCalledWith(
      expect.objectContaining({ id: "7" }),
    );
    // The loader already includes this year's memberships.
    expect(loadGroupMemberships).not.toHaveBeenCalled();
  });

  it("fills the form with the group's details", async () => {
    renderPage(1);

    expect(
      await screen.findByLabelText("group_name", { exact: false }),
    ).toHaveValue("Board");
  });

  it("shows the error page when the group fails to load", async () => {
    loadEditGroupLoaderData.mockRejectedValue(
      new Error("Failed to load group data"),
    );
    renderPage(1);

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Failed to load group data",
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

  it("navigates to the member's admin page when a membership row is clicked", async () => {
    renderPage(1);

    const nameCell = await screen.findByText("Jane Doe");
    fireEvent.click(nameCell.closest("tr") as HTMLElement);

    expect(await screen.findByText("member-page-m1")).toBeInTheDocument();
  });

  it("does not navigate away when the role select inside a row is clicked", async () => {
    renderPage(1);

    await screen.findByText("Jane Doe");
    const roleSelect = screen
      .getAllByRole("combobox")
      .find((el) => within(el).queryByText("Chair"));
    fireEvent.click(roleSelect as HTMLSelectElement);

    expect(screen.queryByText(/member-page-/)).not.toBeInTheDocument();
  });

  it("creates a role alias via the add-role modal", async () => {
    renderPage(1);

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

    await screen.findByText("Jane Doe");
    const fileInput = document.querySelector(
      'input[type="file"]',
    ) as HTMLInputElement;
    const file = new File(["x"], "pic.png", { type: "image/png" });
    fireEvent.change(fileInput, { target: { files: [file] } });

    expect(handleGroupProfilePictureUpload).toHaveBeenCalled();
  });

  it("opens the file picker when the profile picture is clicked", async () => {
    renderPage(1);

    await screen.findByText("Jane Doe");
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

    await waitFor(() =>
      expect(loadGroupMemberships).toHaveBeenCalledWith(
        1,
        Number(otherOption?.value),
        expect.any(Function),
        expect.any(Function),
      ),
    );
    expect(await screen.findByText("John Smith")).toBeInTheDocument();
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
    mockLoaderData({ enrollments: [enrollment({ roleAliasId: 2 })] });
    renderPage(1);

    await screen.findByText("Jane Doe");
    const roleSelect = screen
      .getAllByRole("combobox")
      .find((el) => within(el).queryByText("Chair")) as HTMLSelectElement;
    expect(roleSelect.value).toBe("2");
  });

  it("shows a full-size profile picture once one has loaded", async () => {
    mockLoaderData({ groupPictureSrc: "blob:group-picture" });
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

  it("shows an empty memberships table while another year's memberships are loading", async () => {
    loadGroupMemberships.mockImplementation(
      (
        _id: number,
        _year: number,
        setLoadingMemberships: (v: boolean) => void,
      ) => {
        setLoadingMemberships(true);
        return new Promise<void>(() => {});
      },
    );
    renderPage(1);

    await screen.findByText("Jane Doe");
    const yearSelect = screen
      .getAllByRole("combobox")
      .find((el) =>
        Array.from((el as HTMLSelectElement).options).some((o) =>
          /^\d{4}-\d{4}$/.test(o.textContent ?? ""),
        ),
      ) as HTMLSelectElement;
    fireEvent.change(yearSelect, { target: { value: "2024" } });

    expect(await screen.findAllByText("no_enrollments_found")).not.toHaveLength(
      0,
    );
    expect(screen.queryByText("Jane Doe")).not.toBeInTheDocument();
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
    renderPage(1);

    await screen.findByText("Jane Doe");
    fireEvent.click(screen.getByText("add_role"));
    fireEvent.click(await screen.findByText("create-parent-role"));

    await waitFor(() =>
      expect(screen.queryByText("create-parent-role")).not.toBeInTheDocument(),
    );
  });
});
