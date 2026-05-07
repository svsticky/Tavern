import { t } from "i18next";
import type React from "react";
import toast from "react-hot-toast";
import {
  deleteApiGroupmembershipsById,
  type GroupMembershipResponseDto,
  getApiGroupmemberships,
  getApiGroupsById,
  getApiGroupsByIdGroupPicture,
  getApiRolealiases,
  type MemberResponseDto,
  patchApiGroupmembershipsById,
  patchApiGroupsById,
  postApiGroupmemberships,
  postApiGroupsByIdGroupPicture,
  type RoleAlias,
} from "~/api";

/**
 * Interface representing the editable fields of a group.
 */
type EditGroupFormData = {
  Name: string;
  Type: string;
  Active: boolean;
};

/**
 * Arguments for the loadGroupData handler.
 */
type LoadGroupArgs = {
  id: number | null;
  setFormData: React.Dispatch<React.SetStateAction<EditGroupFormData>>;
  setGroupPictureSrc: (value: string | null) => void;
  setRoleAliases: React.Dispatch<React.SetStateAction<RoleAlias[]>>;
  setLoading: (value: boolean) => void;
};

/**
 * Initializes the edit page by fetching the group profile, group picture, and available role aliases.
 *
 * @async
 * @param {LoadGroupArgs} args - Configuration object containing:
 * @param {number | null} args.id - The ID of the group to load.
 * @param {Function} args.setFormData - Setter for the group's basic info form state.
 * @param {Function} args.setGroupPictureSrc - Setter for the group's profile image source URL.
 * @param {Function} args.setRoleAliases - Setter for the global list of selectable role aliases.
 * @param {Function} args.setLoading - Setter for the component's main loading state.
 * @returns {Promise<Function | undefined>} A cleanup function to revoke the generated Object URL for the image.
 */
export const loadGroupData = async ({
  id,
  setFormData,
  setGroupPictureSrc,
  setRoleAliases,
  setLoading,
}: LoadGroupArgs) => {
  if (!id) return;
  let url = null as string | null;

  try {
    const groupResponse = await getApiGroupsById({ path: { id } });

    if (groupResponse.error || !groupResponse.data)
      throw new Error("Failed to load group data");

    setFormData({
      Name: groupResponse.data.name,
      Type: groupResponse.data.type,
      Active: groupResponse.data.active,
    });

    const roleAliasesResponse = await getApiRolealiases();
    if (roleAliasesResponse.error || !roleAliasesResponse.data)
      throw new Error("Failed to load role aliases");
    setRoleAliases(roleAliasesResponse.data);

    const groupPictureResponse = await getApiGroupsByIdGroupPicture({
      path: { id },
      responseType: "blob",
    });

    if (
      groupPictureResponse.error ||
      !(groupPictureResponse.data instanceof Blob)
    )
    {
      console.warn("Failed to load group picture, using default avatar");
      return;
    }

    url = URL.createObjectURL(groupPictureResponse.data);
    setGroupPictureSrc(url);
  } catch (err) {
    console.log("Failed to load group data:", err);
    toast.error(t("loading_failed"));
  } finally {
    setLoading(false);
  }

  return () => {
    if (url) URL.revokeObjectURL(url);
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
    const groupMembershipsResponse = await getApiGroupmemberships({
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
    toast.error(t("loading_failed"));
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
  const saveProcess = async () => {
    try {
      setSaving(true);

      const patchDoc = Object.keys(formData).map((key) => ({
        op: "replace",
        path: `/${key}`,
        value: formData[key as keyof typeof formData],
      }));

      const response = await patchApiGroupsById({
        path: { id },
        body: patchDoc as any,
      });

      if (response.error) throw new Error("Failed to save group data");
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
      error: t("save_error"),
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
      const response = await postApiGroupsByIdGroupPicture({
        path: { id },
        body: { image: file },
      });

      if (response.error) throw new Error("Failed to upload group picture");

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
    error: t("upload_error"),
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
      const response = await deleteApiGroupmembershipsById({ path: { id } });

      if (response.error) throw new Error("Failed to delete enrollment");

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
    error: t("delete_error"),
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
      const res = await postApiGroupmemberships({
        body: {
          memberId: member.id!,
          groupId: id,
          membershipYear: selectedYear,
        },
      });

      if (res.error || !res.data) throw new Error("Failed to add enrollment");

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
      toast.success(t("enrollment_added"));
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
    error: t("add_error"),
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
      const response = await patchApiGroupmembershipsById({
        path: { id: enrollmentId },
        body: [
          { op: "replace", path: "/roleAliasId", value: newRoleAliasId },
        ] as any,
      });

      if (response.error) throw new Error("Failed to update role");

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
    error: t("role_update_failed"),
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
