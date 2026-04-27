import type React from "react";
import { t } from "i18next";
import toast from "react-hot-toast";
import {
  deleteApiGroupmembershipsById,
  getApiGroupmemberships,
  getApiGroupsById,
  getApiGroupsByIdGroupPicture,
  getApiRolealiases,
  patchApiGroupmembershipsById,
  patchApiGroupsById,
  postApiGroupmemberships,
  postApiGroupsByIdGroupPicture,
  type GroupMembershipResponseDto,
  type MemberResponseDto,
  type RoleAlias
} from "~/api";

type EditGroupFormData = {
  Name: string;
  Type: string;
  Active: boolean;
};

type LoadGroupArgs = {
  id: number | null;
  setFormData: React.Dispatch<React.SetStateAction<EditGroupFormData>>;
  setGroupPictureSrc: (value: string | null) => void;
  setRoleAliases: React.Dispatch<React.SetStateAction<RoleAlias[]>>;
  setLoading: (value: boolean) => void;
};

export const loadGroupData = async ({
  id,
  setFormData,
  setGroupPictureSrc,
  setRoleAliases,
  setLoading
}: LoadGroupArgs) => {
  if (!id) return;
  let url = null as string | null;

  try {
    const groupResponse = await getApiGroupsById({ path: { id } });
    if (groupResponse.data) {
      setFormData({
        Name: groupResponse.data.name,
        Type: groupResponse.data.type,
        Active: groupResponse.data.active
      });
    }

    const groupPictureResponse = await getApiGroupsByIdGroupPicture({ path: { id }, responseType: "blob" });
    if (groupPictureResponse.data instanceof Blob) {
      url = URL.createObjectURL(groupPictureResponse.data);
      setGroupPictureSrc(url);
    }

    const roleAliasesResponse = await getApiRolealiases();
    if (roleAliasesResponse.data) {
      setRoleAliases(roleAliasesResponse.data);
    }
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

export const loadGroupMemberships = async (
  id: number | null,
  selectedYear: number,
  setLoadingMemberships: (value: boolean) => void,
  setEnrollments: React.Dispatch<React.SetStateAction<GroupMembershipResponseDto[]>>
) => {
  if (!id) return;
  try {
    setLoadingMemberships(true);
    const groupMembershipsResponse = await getApiGroupmemberships({
      query: {
        GroupId: id,
        MembershipYear: selectedYear
      }
    });
    if (groupMembershipsResponse.data) {
      setEnrollments(groupMembershipsResponse.data);
    }
  } catch (err) {
    console.log("Failed to load group data:", err);
    toast.error(t("loading_failed"));
  } finally {
    setLoadingMemberships(false);
  }
};

export const handleSaveGroup = async (
  id: number | null,
  formData: EditGroupFormData,
  setSaving: (saving: boolean) => void
) => {
  if (!id) return;
  const saveProcess = async () => {
    try {
      setSaving(true);

      const patchDoc = Object.keys(formData).map((key) => ({
        op: "replace",
        path: `/${key}`,
        value: formData[key as keyof typeof formData]
      }));

      await patchApiGroupsById({
        path: { id },
        body: patchDoc as any
      });
    } catch (err) {
      console.error("Failed to save group data:", err);
      throw err;
    } finally {
      setSaving(false);
    }
  };

  toast.promise(saveProcess(), {
    loading: t("saving"),
    success: t("save_success"),
    error: t("save_error")
  }).finally(() => setSaving(false));
};

export const handleGroupProfilePictureUpload = async (
  e: React.ChangeEvent<HTMLInputElement>,
  id: number | null,
  setSaving: (saving: boolean) => void
) => {
  const file = e.target.files?.[0];
  if (!file || !id) return;

  setSaving(true);

  const saveProcess = async () => {
    try {
      await postApiGroupsByIdGroupPicture({
        path: { id },
        body: { image: file }
      });

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
    error: t("upload_error")
  });
};

export const handleDeleteGroupEnrollment = async (
  id: number,
  setLoading: (loading: boolean) => void,
  setEnrollments: React.Dispatch<React.SetStateAction<GroupMembershipResponseDto[]>>
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
    error: t("delete_error")
  });
};

export const handleAddGroupEnrollment = async (
  id: number | null,
  member: MemberResponseDto,
  selectedYear: number,
  setLoading: (loading: boolean) => void,
  setEnrollments: React.Dispatch<React.SetStateAction<GroupMembershipResponseDto[]>>,
  setAddEnrollmentModalIsOpen: (open: boolean) => void
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
        }
      });
      if (res.data) {
        setEnrollments((prev) => [
          ...prev,
          {
            membershipYear: selectedYear,
            memberId: member.id!,
            groupId: id,
            memberName: `${member.firstName} ${member.lastName}`,
            groupName: res.data.group!.name,
            groupType: res.data.group!.type!,
            id: res.data.id!
          }
        ]);
        toast.success(t("enrollment_added"));
        setAddEnrollmentModalIsOpen(false);
      }
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
    error: t("add_error")
  });
};

export const handleUpdateGroupRole = async (
  enrollmentId: number,
  newRoleAliasId: number | null,
  setLoadingChangeRole: (loading: boolean) => void,
  setEnrollments: React.Dispatch<React.SetStateAction<GroupMembershipResponseDto[]>>
) => {
  const saveProcess = async () => {
    try {
      setLoadingChangeRole(true);
      const response = await patchApiGroupmembershipsById({
        path: { id: enrollmentId },
        body: [
          { op: "replace", path: "/roleAliasId", value: newRoleAliasId }
        ] as any
      });

      if (response.error) throw new Error("Failed to update role");

      setEnrollments((prev) =>
        prev.map((e) =>
          e.id === enrollmentId
            ? { ...e, roleAliasId: newRoleAliasId as any }
            : e
        )
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
    error: t("role_update_failed")
  });
};

export const handleRoleAliasAdded = (
  roleAlias: RoleAlias,
  setRoleAliases: React.Dispatch<React.SetStateAction<RoleAlias[]>>,
  setAddRoleModalIsOpen: (open: boolean) => void
) => {
  setRoleAliases((prev) => [...prev, roleAlias]);
  setAddRoleModalIsOpen(false);
};
