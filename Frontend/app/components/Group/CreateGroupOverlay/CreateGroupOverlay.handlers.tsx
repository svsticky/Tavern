import type React from "react";
import { t } from "i18next";
import toast from "react-hot-toast";
import { postApiGroups, type GroupType } from "~/api";

type CreateGroupFormData = {
  name: string;
  type: string;
  groupPicture: File | null;
};

export const handleFileChange = (
  e: React.ChangeEvent<HTMLInputElement>,
  formData: CreateGroupFormData,
  setFormData: React.Dispatch<React.SetStateAction<CreateGroupFormData>>,
  setImagePreview: (preview: string | null) => void
) => {
  const file = e.target.files?.[0];
  if (file) {
    setFormData({ ...formData, groupPicture: file });
    setImagePreview(URL.createObjectURL(file));
  }
};

export const resetCreateGroupForm = (
  setFormData: React.Dispatch<React.SetStateAction<CreateGroupFormData>>,
  setImagePreview: (preview: string | null) => void
) => {
  setFormData({ name: "", type: "Committee", groupPicture: null });
  setImagePreview(null);
};

type SubmitArgs = {
  e: React.FormEvent;
  formData: CreateGroupFormData;
  setLoading: (loading: boolean) => void;
  onSuccess: () => void;
  resetForm: () => void;
};

export const handleCreateGroupSubmit = async ({ e, formData, setLoading, onSuccess, resetForm }: SubmitArgs) => {
  e.preventDefault();
  if (!formData.name || !formData.groupPicture) {
    toast.error(t("please_fill_all_fields"));
    return;
  }

  setLoading(true);
  try {
    await postApiGroups({
      body: {
        Name: formData.name,
        Type: formData.type as GroupType,
        GroupPicture: formData.groupPicture,
      }
    });

    toast.success(t("group_created_successfully"));
    onSuccess();
    resetForm();
  } catch (err) {
    toast.error(t("failed_to_create_group"));
    console.error(err);
  } finally {
    setLoading(false);
  }
};
