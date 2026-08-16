import { t } from "i18next";
import type React from "react";
import toast from "react-hot-toast";
import { type GroupType, postGroups } from "~/api";
import { appendErrorMessage } from "~/util/error.util";

type CreateGroupFormData = {
  name: string;
  type: string;
  groupPicture: File | null;
};

/**
 * Processes a file input change event to update the form state and generate a local image preview.
 *
 * @param e - The change event from the file input element.
 * @param formData - The current state of the group creation form.
 * @param setFormData - State setter to update the form data with the selected file.
 * @param setImagePreview - State setter to store the Object URL for the image preview.
 */
export const handleFileChange = (
  e: React.ChangeEvent<HTMLInputElement>,
  formData: CreateGroupFormData,
  setFormData: React.Dispatch<React.SetStateAction<CreateGroupFormData>>,
  setImagePreview: (preview: string | null) => void,
) => {
  const file = e.target.files?.[0];
  if (file) {
    setFormData({ ...formData, groupPicture: file });
    setImagePreview(URL.createObjectURL(file));
  }
};

/**
 * Resets the group creation form to its initial default values.
 *
 * @param setFormData - State setter to clear form fields.
 * @param setImagePreview - State setter to remove the local image preview and revoke URL objects.
 */
export const resetCreateGroupForm = (
  setFormData: React.Dispatch<React.SetStateAction<CreateGroupFormData>>,
  setImagePreview: (preview: string | null) => void,
) => {
  setFormData({ name: "", type: "Committee", groupPicture: null });
  setImagePreview(null);
};

/**
 * Arguments for the `handleCreateGroupSubmit` function.
 */
type SubmitArgs = {
  e: React.FormEvent;
  formData: CreateGroupFormData;
  setLoading: (loading: boolean) => void;
  onSuccess: () => void;
  resetForm: () => void;
};

/**
 * Handles the submission of the group creation form.
 *
 * Validates the presence of required fields (Name and Picture), performs the
 * API request, and manages user feedback via toast notifications.
 *
 * @async
 * @param {SubmitArgs} args - Configuration for the submission process.
 */
export const handleCreateGroupSubmit = ({
  e,
  formData,
  setLoading,
  onSuccess,
  resetForm,
}: SubmitArgs) => {
  e.preventDefault();

  const createProcess = async () => {
    try {
      setLoading(true);
      if (!formData.name || !formData.groupPicture) {
        toast.error(appendErrorMessage(t("please_fill_all_fields")));
        return;
      }

      const response = await postGroups({
        body: {
          Name: formData.name,
          Type: formData.type as GroupType,
          GroupPicture: formData.groupPicture,
        },
      });

      if (response.error) {
        throw response.error ?? new Error("Failed to create group");
      }

      onSuccess();
      resetForm();
    } catch (err) {
      console.error(err);
      throw err;
    } finally {
      setLoading(false);
    }
  };

  toast.promise(createProcess(), {
    loading: t("creating_group"),
    success: t("group_created_successfully"),
    error: (error) => appendErrorMessage(t("failed_to_create_group"), error),
  });
};
