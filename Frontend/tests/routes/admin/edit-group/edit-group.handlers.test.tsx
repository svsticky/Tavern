import { beforeEach, describe, expect, it, vi } from "vitest";
import type {
  GroupMembershipResponseDto,
  MemberResponseDto,
  RoleAlias,
} from "~/api";

const {
  deleteGroupmembershipsById,
  getGroupmemberships,
  getGroupsById,
  getGroupsByIdGroupPicture,
  getRolealiases,
  patchGroupmembershipsById,
  patchGroupsById,
  postGroupmemberships,
  postGroupsByIdGroupPicture,
} = vi.hoisted(() => ({
  deleteGroupmembershipsById: vi.fn(),
  getGroupmemberships: vi.fn(),
  getGroupsById: vi.fn(),
  getGroupsByIdGroupPicture: vi.fn(),
  getRolealiases: vi.fn(),
  patchGroupmembershipsById: vi.fn(),
  patchGroupsById: vi.fn(),
  postGroupmemberships: vi.fn(),
  postGroupsByIdGroupPicture: vi.fn(),
}));

vi.mock("~/api", () => ({
  deleteGroupmembershipsById,
  getGroupmemberships,
  getGroupsById,
  getGroupsByIdGroupPicture,
  getRolealiases,
  patchGroupmembershipsById,
  patchGroupsById,
  postGroupmemberships,
  postGroupsByIdGroupPicture,
}));

vi.mock("react-hot-toast", () => ({
  default: {
    success: vi.fn(),
    error: vi.fn(),
    promise: vi.fn((p, opts: any) =>
      p
        .then((res: any) => {
          if (typeof opts?.success === "function") opts.success(res);
          return res;
        })
        .catch((err: any) => {
          if (typeof opts?.error === "function") opts.error(err);
        }),
    ),
  },
}));

import toast from "react-hot-toast";
import {
  handleAddGroupEnrollment,
  handleDeleteGroupEnrollment,
  handleGroupProfilePictureUpload,
  handleRoleAliasAdded,
  handleSaveGroup,
  handleUpdateGroupRole,
  loadGroupData,
  loadGroupMemberships,
} from "~/routes/admin/edit-group/edit-group.handlers";

describe("loadGroupData", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.stubGlobal("URL", {
      ...URL,
      createObjectURL: vi.fn(() => "blob:mock-url"),
      revokeObjectURL: vi.fn(),
    });
  });

  it("returns immediately when id is null", async () => {
    const setLoading = vi.fn();
    await loadGroupData({
      id: null,
      setFormData: vi.fn(),
      setGroupPictureSrc: vi.fn(),
      setRoleAliases: vi.fn(),
      setLoading,
    });
    expect(setLoading).not.toHaveBeenCalled();
  });

  it("loads group data, role aliases, and picture on success", async () => {
    getGroupsById.mockResolvedValue({
      data: {
        name: "Board",
        type: "Committee",
        active: true,
        glAccountId: "GL1",
        costUnitId: "CU1",
      },
    });
    getRolealiases.mockResolvedValue({ data: [{ id: 1, name: "Chair" }] });
    getGroupsByIdGroupPicture.mockResolvedValue({ data: new Blob(["x"]) });

    const setFormData = vi.fn();
    const setGroupPictureSrc = vi.fn();
    const setRoleAliases = vi.fn();
    const setLoading = vi.fn();

    const cleanup = await loadGroupData({
      id: 1,
      setFormData,
      setGroupPictureSrc,
      setRoleAliases,
      setLoading,
    });

    expect(setFormData).toHaveBeenCalledWith({
      Name: "Board",
      Type: "Committee",
      Active: true,
      DefaultGLAccount: "GL1",
      DefaultCostCenter: "CU1",
    });
    expect(setRoleAliases).toHaveBeenCalledWith([{ id: 1, name: "Chair" }]);
    expect(setGroupPictureSrc).toHaveBeenCalledWith("blob:mock-url");
    expect(setLoading).toHaveBeenCalledWith(false);

    cleanup?.();
    expect(URL.revokeObjectURL).toHaveBeenCalledWith("blob:mock-url");
  });

  it("defaults DefaultGLAccount/DefaultCostCenter to empty string when missing", async () => {
    getGroupsById.mockResolvedValue({
      data: { name: "Board", type: "Committee", active: true },
    });
    getRolealiases.mockResolvedValue({ data: [] });
    getGroupsByIdGroupPicture.mockResolvedValue({ data: new Blob(["x"]) });

    const setFormData = vi.fn();

    await loadGroupData({
      id: 1,
      setFormData,
      setGroupPictureSrc: vi.fn(),
      setRoleAliases: vi.fn(),
      setLoading: vi.fn(),
    });

    expect(setFormData).toHaveBeenCalledWith(
      expect.objectContaining({ DefaultGLAccount: "", DefaultCostCenter: "" }),
    );
  });

  it("continues without a picture when the picture request fails", async () => {
    getGroupsById.mockResolvedValue({
      data: { name: "Board", type: "Committee", active: true },
    });
    getRolealiases.mockResolvedValue({ data: [] });
    getGroupsByIdGroupPicture.mockResolvedValue({ error: "not found" });

    const setGroupPictureSrc = vi.fn();
    const setLoading = vi.fn();

    await loadGroupData({
      id: 1,
      setFormData: vi.fn(),
      setGroupPictureSrc,
      setRoleAliases: vi.fn(),
      setLoading,
    });

    expect(setGroupPictureSrc).not.toHaveBeenCalled();
    expect(setLoading).toHaveBeenCalledWith(false);
  });

  it("shows an error toast when the group request fails", async () => {
    getGroupsById.mockResolvedValue({ error: "bad" });
    const setLoading = vi.fn();

    await loadGroupData({
      id: 1,
      setFormData: vi.fn(),
      setGroupPictureSrc: vi.fn(),
      setRoleAliases: vi.fn(),
      setLoading,
    });

    expect(toast.error).toHaveBeenCalledWith(
      "loading_failed: Failed to load group data",
    );
    expect(setLoading).toHaveBeenCalledWith(false);
  });

  it("shows an error toast when role aliases fail to load", async () => {
    getGroupsById.mockResolvedValue({
      data: { name: "Board", type: "Committee", active: true },
    });
    getRolealiases.mockResolvedValue({ error: "bad roles" });

    await loadGroupData({
      id: 1,
      setFormData: vi.fn(),
      setGroupPictureSrc: vi.fn(),
      setRoleAliases: vi.fn(),
      setLoading: vi.fn(),
    });

    expect(toast.error).toHaveBeenCalledWith(
      "loading_failed: Failed to load role aliases",
    );
  });
});

describe("loadGroupMemberships", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("returns immediately when id is null", async () => {
    const setLoadingMemberships = vi.fn();
    await loadGroupMemberships(null, 2024, setLoadingMemberships, vi.fn());
    expect(setLoadingMemberships).not.toHaveBeenCalled();
  });

  it("fetches memberships for the given group and year", async () => {
    const memberships: GroupMembershipResponseDto[] = [
      { id: 1, memberName: "A" } as GroupMembershipResponseDto,
    ];
    getGroupmemberships.mockResolvedValue({ data: memberships });
    const setLoadingMemberships = vi.fn();
    const setEnrollments = vi.fn();

    await loadGroupMemberships(1, 2024, setLoadingMemberships, setEnrollments);

    expect(getGroupmemberships).toHaveBeenCalledWith({
      query: { GroupId: 1, MembershipYear: 2024 },
    });
    expect(setEnrollments).toHaveBeenCalledWith(memberships);
    expect(setLoadingMemberships).toHaveBeenNthCalledWith(1, true);
    expect(setLoadingMemberships).toHaveBeenNthCalledWith(2, false);
  });

  it("shows a toast error when the request fails", async () => {
    getGroupmemberships.mockResolvedValue({ error: "bad" });
    const setLoadingMemberships = vi.fn();

    await loadGroupMemberships(1, 2024, setLoadingMemberships, vi.fn());

    expect(toast.error).toHaveBeenCalledWith(
      "loading_failed: Failed to load group memberships",
    );
    expect(setLoadingMemberships).toHaveBeenLastCalledWith(false);
  });
});

describe("handleSaveGroup", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("returns immediately when id is null", async () => {
    const setSaving = vi.fn();
    await handleSaveGroup(null, {} as any, setSaving);
    expect(setSaving).not.toHaveBeenCalled();
  });

  it("saves the group as a JSON patch document", async () => {
    patchGroupsById.mockResolvedValue({});
    const setSaving = vi.fn();

    await handleSaveGroup(
      1,
      {
        Name: "Board",
        Type: "Committee",
        DefaultGLAccount: "",
        DefaultCostCenter: "",
        Active: true,
      },
      setSaving,
    );

    await vi.waitFor(() => expect(setSaving).toHaveBeenLastCalledWith(false));
    expect(patchGroupsById).toHaveBeenCalledWith({
      path: { id: 1 },
      body: expect.arrayContaining([
        { op: "replace", path: "/Name", value: "Board" },
      ]),
    });
  });

  it("sends DefaultGLAccount/DefaultCostCenter patch paths matching the backend's Group entity", async () => {
    patchGroupsById.mockResolvedValue({});
    const setSaving = vi.fn();

    await handleSaveGroup(
      1,
      {
        Name: "Board",
        Type: "Committee",
        DefaultGLAccount: "8000",
        DefaultCostCenter: "TRX",
        Active: true,
      },
      setSaving,
    );

    await vi.waitFor(() => expect(setSaving).toHaveBeenLastCalledWith(false));
    expect(patchGroupsById).toHaveBeenCalledWith({
      path: { id: 1 },
      body: expect.arrayContaining([
        { op: "replace", path: "/DefaultGLAccount", value: "8000" },
        { op: "replace", path: "/DefaultCostCenter", value: "TRX" },
      ]),
    });
  });

  it("throws and reports an error when the save fails", async () => {
    patchGroupsById.mockResolvedValue({ error: true, message: "nope" });
    const setSaving = vi.fn();

    await handleSaveGroup(
      1,
      // Name must be non-empty, otherwise handleSaveGroup returns before ever calling
      // patchGroupsById or setSaving (the "please_fill_all_fields" validation guard).
      {
        Name: "Board",
        Type: "",
        DefaultGLAccount: "",
        DefaultCostCenter: "",
        Active: false,
      },
      setSaving,
    );

    await vi.waitFor(() => expect(setSaving).toHaveBeenLastCalledWith(false));
    expect(toast.promise).toHaveBeenCalled();
  });
});

describe("handleGroupProfilePictureUpload", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.stubGlobal("location", { reload: vi.fn() });
  });

  it("does nothing when there is no file or no id", async () => {
    const setSaving = vi.fn();
    await handleGroupProfilePictureUpload(
      {
        target: { files: [] },
      } as unknown as React.ChangeEvent<HTMLInputElement>,
      1,
      setSaving,
    );
    expect(setSaving).not.toHaveBeenCalled();

    await handleGroupProfilePictureUpload(
      {
        target: { files: [new File(["x"], "a.png")] },
      } as unknown as React.ChangeEvent<HTMLInputElement>,
      null,
      setSaving,
    );
    expect(setSaving).not.toHaveBeenCalled();
  });

  it("uploads the picture and reloads the page on success", async () => {
    postGroupsByIdGroupPicture.mockResolvedValue({});
    const setSaving = vi.fn();
    const file = new File(["x"], "a.png");

    await handleGroupProfilePictureUpload(
      {
        target: { files: [file] },
      } as unknown as React.ChangeEvent<HTMLInputElement>,
      1,
      setSaving,
    );

    await vi.waitFor(() => expect(window.location.reload).toHaveBeenCalled());
    expect(postGroupsByIdGroupPicture).toHaveBeenCalledWith({
      path: { id: 1 },
      body: { image: file },
    });
  });

  it("throws when the upload fails", async () => {
    postGroupsByIdGroupPicture.mockResolvedValue({
      error: true,
      message: "bad",
    });
    const setSaving = vi.fn();
    const file = new File(["x"], "a.png");

    await handleGroupProfilePictureUpload(
      {
        target: { files: [file] },
      } as unknown as React.ChangeEvent<HTMLInputElement>,
      1,
      setSaving,
    );

    await vi.waitFor(() => expect(setSaving).toHaveBeenLastCalledWith(false));
    expect(window.location.reload).not.toHaveBeenCalled();
  });
});

describe("handleDeleteGroupEnrollment", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("removes the enrollment from local state on success", async () => {
    deleteGroupmembershipsById.mockResolvedValue({});
    const setLoading = vi.fn();
    const setEnrollments = vi.fn();

    await handleDeleteGroupEnrollment(5, setLoading, setEnrollments);

    await vi.waitFor(() => expect(setLoading).toHaveBeenLastCalledWith(false));
    const updater = setEnrollments.mock.calls[0][0];
    expect(
      updater([{ id: 5 }, { id: 6 }] as GroupMembershipResponseDto[]),
    ).toEqual([{ id: 6 }]);
  });

  it("throws when the delete fails", async () => {
    deleteGroupmembershipsById.mockResolvedValue({
      error: true,
      message: "bad",
    });
    const setLoading = vi.fn();
    const setEnrollments = vi.fn();

    await handleDeleteGroupEnrollment(5, setLoading, setEnrollments);

    await vi.waitFor(() => expect(setLoading).toHaveBeenLastCalledWith(false));
    expect(setEnrollments).not.toHaveBeenCalled();
  });
});

describe("handleAddGroupEnrollment", () => {
  const member: MemberResponseDto = {
    id: "m1",
    firstName: "Jane",
    lastName: "Doe",
  } as MemberResponseDto;

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("does nothing when id or member/member.id is missing", async () => {
    const setLoading = vi.fn();
    await handleAddGroupEnrollment(
      null,
      member,
      2024,
      setLoading,
      vi.fn(),
      vi.fn(),
    );
    expect(setLoading).not.toHaveBeenCalled();

    await handleAddGroupEnrollment(
      1,
      { ...member, id: undefined } as unknown as MemberResponseDto,
      2024,
      setLoading,
      vi.fn(),
      vi.fn(),
    );
    expect(setLoading).not.toHaveBeenCalled();
  });

  it("adds the new enrollment to local state and closes the modal", async () => {
    postGroupmemberships.mockResolvedValue({
      data: {
        id: 10,
        group: { name: "Board", type: "Committee" },
      },
    });
    const setEnrollments = vi.fn();
    const setAddEnrollmentModalIsOpen = vi.fn();

    await handleAddGroupEnrollment(
      1,
      member,
      2024,
      vi.fn(),
      setEnrollments,
      setAddEnrollmentModalIsOpen,
    );

    await vi.waitFor(() =>
      expect(setAddEnrollmentModalIsOpen).toHaveBeenCalledWith(false),
    );
    const updater = setEnrollments.mock.calls[0][0];
    expect(updater([])).toEqual([
      {
        membershipYear: 2024,
        memberId: "m1",
        groupId: 1,
        memberName: "Jane Doe",
        groupName: "Board",
        groupType: "Committee",
        id: 10,
      },
    ]);
  });

  it("throws when the add request fails", async () => {
    postGroupmemberships.mockResolvedValue({ error: "bad" });
    const setAddEnrollmentModalIsOpen = vi.fn();

    await handleAddGroupEnrollment(
      1,
      member,
      2024,
      vi.fn(),
      vi.fn(),
      setAddEnrollmentModalIsOpen,
    );

    await vi.waitFor(() => expect(toast.promise).toHaveBeenCalled());
    expect(setAddEnrollmentModalIsOpen).not.toHaveBeenCalled();
  });
});

describe("handleUpdateGroupRole", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("updates the role alias on the matching enrollment", async () => {
    patchGroupmembershipsById.mockResolvedValue({});
    const setEnrollments = vi.fn();

    await handleUpdateGroupRole(5, 2, vi.fn(), setEnrollments);

    await vi.waitFor(() =>
      expect(patchGroupmembershipsById).toHaveBeenCalledWith({
        path: { id: 5 },
        body: [{ op: "replace", path: "/roleAliasId", value: 2 }],
      }),
    );
    const updater = setEnrollments.mock.calls[0][0];
    expect(
      updater([
        { id: 5, roleAliasId: null },
      ] as unknown as GroupMembershipResponseDto[]),
    ).toEqual([{ id: 5, roleAliasId: 2 }]);
  });

  it("throws when the update fails", async () => {
    patchGroupmembershipsById.mockResolvedValue({
      error: true,
      message: "bad",
    });
    const setLoadingChangeRole = vi.fn();

    await handleUpdateGroupRole(5, null, setLoadingChangeRole, vi.fn());

    await vi.waitFor(() =>
      expect(setLoadingChangeRole).toHaveBeenLastCalledWith(false),
    );
  });
});

describe("handleRoleAliasAdded", () => {
  it("appends the role alias and closes the modal", () => {
    const setRoleAliases = vi.fn();
    const setAddRoleModalIsOpen = vi.fn();
    const roleAlias: RoleAlias = { id: 1, name: "Chair" } as RoleAlias;

    handleRoleAliasAdded(roleAlias, setRoleAliases, setAddRoleModalIsOpen);

    const updater = setRoleAliases.mock.calls[0][0];
    expect(updater([])).toEqual([roleAlias]);
    expect(setAddRoleModalIsOpen).toHaveBeenCalledWith(false);
  });
});
