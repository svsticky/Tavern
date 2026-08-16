import { t } from "i18next";
import toast from "react-hot-toast";
import { postProfilepictureByIdProfilePicture } from "~/api";
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
