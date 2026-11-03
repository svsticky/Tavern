import { t } from "i18next";
import type React from "react";
import toast from "react-hot-toast";
import { data } from "react-router";
import {
  deleteGroupmembershipsById,
  type GroupMembershipResponseDto,
  getGroupmemberships,
  getGroupsById,
  getGroupsByIdGroupPicture,
  getRolealiases,
  type MemberResponseDto,
  patchGroupmembershipsById,
  patchGroupsById,
  postGroupmemberships,
  postGroupsByIdGroupPicture,
  type RoleAlias,
} from "~/api";
import { appendErrorMessage, getFriendlyErrorMessage } from "~/util/error.util";
import { loadCommitteeYear } from "~/util/loaderAuth.util";

/**
 * Interface representing the editable fields of a group.
 * @typedef {Object} EditGroupFormData
 * @property {string} Name - The group's name.
 * @property {string} Type - The group's type, e.g. committee or working group.
 * @property {string} DefaultGLAccount - The default general ledger account for the group's activities.
 * @property {string} DefaultCostCenter - The default cost center for the group's activities.
 * @property {string} DefaultCostUnit - The default cost unit for the group's activities.
 * @property {boolean} Active - Whether the group is active.
 */
export type EditGroupFormData = {
  Name: string;
  Type: string;
  DefaultGLAccount: string;
  DefaultCostCenter: string;
  DefaultCostUnit: string;
  Active: boolean;
};

/**
 * The data the group editor's `clientLoader` provides.
 * @typedef {Object} EditGroupLoaderData
 * @property {number} groupId - The ID of the group being edited.
 * @property {EditGroupFormData} formData - The group's editable fields.
 * @property {RoleAlias[]} roleAliases - All role aliases that can be assigned to members.
 * @property {string | null} groupPictureSrc - The group picture's object URL, or `null` when it has none.
 * @property {number} committeeYear - The current committee year, the default year for the memberships table.
 * @property {GroupMembershipResponseDto[]} enrollments - The group's memberships in `committeeYear`.
 */
export type EditGroupLoaderData = {
  groupId: number;
  formData: EditGroupFormData;
  roleAliases: RoleAlias[];
  groupPictureSrc: string | null;
  committeeYear: number;
  enrollments: GroupMembershipResponseDto[];
};

/**
 * Loads a group's picture as an object URL.
 *
 * @param {number} id - The ID of the group.
 * @returns {Promise<string | null>} The picture's object URL, or `null` when the group has none or it fails to load.
 */
const loadGroupPictureSrc = async (id: number): Promise<string | null> => {
  try {
    const response = await getGroupsByIdGroupPicture({
      path: { id },
      responseType: "blob",
    });
    if (response.error || !(response.data instanceof Blob)) {
      console.warn("Failed to load group picture, using default avatar");
      return null;
    }
    return URL.createObjectURL(response.data);
  } catch (error) {
    console.warn("Failed to load group picture, using default avatar", error);
    return null;
  }
};

/**
 * Loads everything the group editor needs: the group's details and picture, the
 * role aliases, and the group's memberships in the current committee year.
 *
 * @param {{ id?: string }} params - The route parameters, containing the group ID.
 * @returns {Promise<EditGroupLoaderData>} The loader data for the group editor.
 * @throws {Response} A 404 response when the ID isn't a number.
 * @throws {Error} When the group, role aliases or memberships fail to load, which shows the route's error page.
 */
export const loadEditGroupLoaderData = async (params: {
  id?: string;
}): Promise<EditGroupLoaderData> => {
  const groupId = Number(params.id);
  if (!params.id || !Number.isInteger(groupId)) {
    throw data("Not Found", { status: 404 });
  }

  const committeeYear = await loadCommitteeYear();

  const [
    groupResponse,
    roleAliasesResponse,
    membershipsResponse,
    groupPictureSrc,
  ] = await Promise.all([
    getGroupsById({ path: { id: groupId } }),
    getRolealiases(),
    getGroupmemberships({
      query: { GroupId: groupId, MembershipYear: committeeYear },
    }),
    loadGroupPictureSrc(groupId),
  ]);

  if (groupResponse.error || !groupResponse.data)
    throw groupResponse.error ?? new Error("Failed to load group data");
  if (roleAliasesResponse.error || !roleAliasesResponse.data)
    throw roleAliasesResponse.error ?? new Error("Failed to load role aliases");
  if (membershipsResponse.error || !membershipsResponse.data)
    throw (
      membershipsResponse.error ?? new Error("Failed to load group memberships")
    );

  const group = groupResponse.data;
  return {
    groupId,
    formData: {
      Name: group.name,
      Type: group.type,
      Active: group.active,
      DefaultGLAccount: group.glAccountId ?? "",
      DefaultCostCenter: group.costCenterId ?? "",
      DefaultCostUnit: group.costUnitId ?? "",
    },
    roleAliases: roleAliasesResponse.data,
    groupPictureSrc,
    committeeYear,
    enrollments: membershipsResponse.data,
  };
};

/**
 * Fetches memberships for a specific group filtered by association year.
 *
 * @async
 * @param {number | null} id - The ID of the group for which to load memberships.
 * @param {number} selectedYear - The association year to filter memberships by.
 * @param {Function} setLoadingMemberships - Setter for the membership-specific loading state.
 * @param {Function} setEnrollments - Setter for the list of group memberships.
 */
export const loadGroupMemberships = async (
  id: number | null,
  selectedYear: number,
  setLoadingMemberships: (value: boolean) => void,
  setEnrollments: React.Dispatch<
    React.SetStateAction<GroupMembershipResponseDto[]>
  >,
) => {
  if (!id) return;
  try {
    setLoadingMemberships(true);
    const groupMembershipsResponse = await getGroupmemberships({
      query: {
        GroupId: id,
        MembershipYear: selectedYear,
      },
    });

    if (groupMembershipsResponse.error || !groupMembershipsResponse.data)
      throw new Error("Failed to load group memberships");

    setEnrollments(groupMembershipsResponse.data);
  } catch (err) {
    console.log("Failed to load group data:", err);
    toast.error(appendErrorMessage(t("loading_failed"), err));
  } finally {
    setLoadingMemberships(false);
  }
};

/**
 * Updates the group metadata (Name, Type, Active status) using JSON Patch.
 *
 * @async
 * @param {number | null} id - The ID of the group to save.
 * @param {EditGroupFormData} formData - The current form data to persist.
 * @param {Function} setSaving - Setter to track the asynchronous saving progress.
 */
export const handleSaveGroup = async (
  id: number | null,
  formData: EditGroupFormData,
  setSaving: (saving: boolean) => void,
) => {
  if (!id) return;

  if (!formData.Name.trim()) {
    toast.error(t("please_fill_all_fields"));
    return;
  }

  const saveProcess = async () => {
    try {
      setSaving(true);

      const patchDoc = Object.keys(formData).map((key) => ({
        op: "replace",
        path: `/${key}`,
        value: formData[key as keyof typeof formData],
      }));

      const response = await patchGroupsById({
        path: { id },
        body: patchDoc as any,
      });

      if (response.error) {
        throw response.error ?? new Error("Failed to save group data");
      }
    } catch (err) {
      console.error("Failed to save group data:", err);
      throw err;
    } finally {
      setSaving(false);
    }
  };

  toast
    .promise(saveProcess(), {
      loading: t("saving"),
      success: t("save_success"),
      error: (error) => appendErrorMessage(t("save_error"), error),
    })
    .finally(() => setSaving(false));
};

/**
 * Uploads a new profile picture for the group.
 *
 * @async
 * @param {React.ChangeEvent<HTMLInputElement>} e - The input change event containing the file.
 * @param {number | null} id - The ID of the group receiving the new picture.
 * @param {Function} setSaving - Setter to track the upload progress.
 */
export const handleGroupProfilePictureUpload = async (
  e: React.ChangeEvent<HTMLInputElement>,
  id: number | null,
  setSaving: (saving: boolean) => void,
) => {
  const file = e.target.files?.[0];
  if (!file || !id) return;

  setSaving(true);

  const saveProcess = async () => {
    try {
      const response = await postGroupsByIdGroupPicture({
        path: { id },
        body: { image: file },
      });

      if (response.error) {
        throw response.error ?? new Error("Failed to upload group picture");
      }

      window.location.reload();
    } catch (err) {
      console.error("Failed to upload group picture:", err);
      throw err;
    } finally {
      setSaving(false);
    }
  };

  toast.promise(saveProcess(), {
    loading: t("uploading"),
    success: t("upload_success"),
    error: (error) => appendErrorMessage(t("upload_error"), error),
  });
};

/**
 * Removes a member's enrollment from the group.
 *
 * @async
 * @param {number} id - The unique ID of the group membership record to delete.
 * @param {Function} setLoading - Setter to track the deletion progress.
 * @param {Function} setEnrollments - Setter to update the local membership list.
 */
export const handleDeleteGroupEnrollment = async (
  id: number,
  setLoading: (loading: boolean) => void,
  setEnrollments: React.Dispatch<
    React.SetStateAction<GroupMembershipResponseDto[]>
  >,
) => {
  const deleteProcess = async () => {
    try {
      setLoading(true);
      const response = await deleteGroupmembershipsById({ path: { id } });

      if (response.error) {
        throw response.error ?? new Error("Failed to delete enrollment");
      }

      setEnrollments((prev) => prev.filter((e) => e.id !== id));
    } catch (err) {
      console.error("Failed to delete enrollment:", err);
      throw err;
    } finally {
      setLoading(false);
    }
  };

  toast.promise(deleteProcess(), {
    loading: t("deleting"),
    success: t("delete_success"),
    error: (error) => appendErrorMessage(t("delete_error"), error),
  });
};

/**
 * Adds a new member to the group for a specific association year.
 *
 * @async
 * @param {number | null} id - The ID of the group receiving a new member.
 * @param {MemberResponseDto} member - The member data to be enrolled.
 * @param {number} selectedYear - The association year for the new membership.
 * @param {Function} setLoading - Setter to track the creation progress.
 * @param {Function} setEnrollments - Setter to update the local membership list.
 * @param {Function} setAddEnrollmentModalIsOpen - Setter to close the enrollment modal on success.
 */
export const handleAddGroupEnrollment = async (
  id: number | null,
  member: MemberResponseDto,
  selectedYear: number,
  setLoading: (loading: boolean) => void,
  setEnrollments: React.Dispatch<
    React.SetStateAction<GroupMembershipResponseDto[]>
  >,
  setAddEnrollmentModalIsOpen: (open: boolean) => void,
) => {
  if (!id || !member || !member.id) return;
  const executeProcess = async () => {
    try {
      setLoading(true);
      const res = await postGroupmemberships({
        body: {
          memberId: member.id!,
          groupId: id,
          membershipYear: selectedYear,
        },
      });

      if (res.error || !res.data) {
        throw res.error ?? new Error("Failed to add enrollment");
      }

      setEnrollments((prev) => [
        ...prev,
        {
          membershipYear: selectedYear,
          memberId: member.id!,
          groupId: id,
          memberName: `${member.firstName} ${member.lastName}`,
          groupName: res.data.group!.name,
          groupType: res.data.group!.type!,
          id: res.data.id!,
        },
      ]);
      setAddEnrollmentModalIsOpen(false);
    } catch (err) {
      console.error("Failed to add enrollment:", err);
      throw err;
    } finally {
      setLoading(false);
    }
  };

  toast.promise(executeProcess(), {
    loading: t("adding"),
    success: t("add_success"),
    error: (error) => getFriendlyErrorMessage(t("add_error"), error),
  });
};

/**
 * Updates the specific role alias assigned to a group membership.
 *
 * @async
 * @param {number} enrollmentId - The ID of the group membership record to update.
 * @param {number | null} newRoleAliasId - The ID of the new role alias (or null to clear).
 * @param {Function} setLoadingChangeRole - Setter to track the role update progress.
 * @param {Function} setEnrollments - Setter to update the local membership list.
 */
export const handleUpdateGroupRole = async (
  enrollmentId: number,
  newRoleAliasId: number | null,
  setLoadingChangeRole: (loading: boolean) => void,
  setEnrollments: React.Dispatch<
    React.SetStateAction<GroupMembershipResponseDto[]>
  >,
) => {
  const saveProcess = async () => {
    try {
      setLoadingChangeRole(true);
      const response = await patchGroupmembershipsById({
        path: { id: enrollmentId },
        body: [
          { op: "replace", path: "/roleAliasId", value: newRoleAliasId },
        ] as any,
      });

      if (response.error) {
        throw response.error ?? new Error("Failed to update role");
      }

      setEnrollments((prev) =>
        prev.map((e) =>
          e.id === enrollmentId
            ? { ...e, roleAliasId: newRoleAliasId as any }
            : e,
        ),
      );
    } catch (err) {
      console.error("Failed to update enrollment role:", err);
      throw err;
    } finally {
      setLoadingChangeRole(false);
    }
  };

  toast.promise(saveProcess(), {
    loading: t("updating_role"),
    success: t("role_updated"),
    error: (error) => appendErrorMessage(t("role_update_failed"), error),
  });
};

/**
 * Updates the local list of role aliases when a new one is created via a modal.
 *
 * @param {RoleAlias} roleAlias - The newly created role alias object.
 * @param {Function} setRoleAliases - Setter to update the local list of available roles.
 * @param {Function} setAddRoleModalIsOpen - Setter to close the role creation modal.
 */
export const handleRoleAliasAdded = (
  roleAlias: RoleAlias,
  setRoleAliases: React.Dispatch<React.SetStateAction<RoleAlias[]>>,
  setAddRoleModalIsOpen: (open: boolean) => void,
) => {
  setRoleAliases((prev) => [...prev, roleAlias]);
  setAddRoleModalIsOpen(false);
};
