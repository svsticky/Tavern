import { t } from "i18next";
import toast from "react-hot-toast";
import {
  deleteMembersByIdProfilePicture,
  postProfilepictureByIdProfilePicture,
} from "~/api";
import { appendErrorMessage } from "~/util/error.util";

/**
 * Handles the profile picture upload process.
 * @param {React.ChangeEvent<HTMLInputElement>} e - The change event from the file input.
 * @param {string} userId - The ID of the user whose profile picture is being uploaded.
 */
export const handleProfilePictureUpload = async (
  e: React.ChangeEvent<HTMLInputElement>,
  userId: string,
) => {
  const file = e.target.files?.[0];
  if (!file) return;

  const saveProcess = async (userId: string) => {
    try {
      const response = await postProfilepictureByIdProfilePicture({
        path: { id: userId },
        body: { image: file },
      });
      if (response.error) {
        throw response.error ?? new Error("Failed to upload profile picture");
      }
      window.location.reload();
    } catch (err) {
      console.error("Failed to upload profile picture:", err);
      throw err;
    }
  };

  toast.promise(saveProcess(userId), {
    loading: t("uploading_profile_picture"),
    success: t("upload_successful"),
    error: (error) => appendErrorMessage(t("upload_failed"), error),
  });
};

/**
 * Handles removing the user's profile picture.
 * @param {string} userId - The ID of the member whose profile picture should be deleted.
 * @param {() => void} [onSuccess] - Optional callback to invoke after successful deletion.
 */
export const handleProfilePictureDelete = async (
  userId: string,
  onSuccess?: () => void,
) => {
  const deleteProcess = async (userId: string) => {
    try {
      const response = await deleteMembersByIdProfilePicture({
        path: { id: userId },
      });
      if (response.error) {
        throw response.error ?? new Error("Failed to delete profile picture");
      }
      if (onSuccess) {
        onSuccess();
      } else {
        window.location.reload();
      }
    } catch (err) {
      console.error("Failed to delete profile picture:", err);
      throw err;
    }
  };

  toast.promise(deleteProcess(userId), {
    loading: t("deleting_profile_picture"),
    success: t("profile_picture_deleted"),
    error: (error) =>
      appendErrorMessage(t("delete_profile_picture_failed"), error),
  });
};
