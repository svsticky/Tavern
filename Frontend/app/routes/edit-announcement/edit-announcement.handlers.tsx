import { t } from "i18next";
import type React from "react";
import toast from "react-hot-toast";
import type { NavigateFunction } from "react-router";
import {
  deleteAnnouncementsById,
  postAnnouncements,
  putAnnouncementsById,
} from "~/api";
import { appendErrorMessage } from "~/util/error.util";

/**
 * Arguments for the handleAnnouncementSubmit handler.
 * @typedef {Object} SubmitAnnouncementArgs
 * @property {React.FormEvent<HTMLFormElement>} e - The submit event of the announcement form.
 * @property {boolean} isEdit - Whether an existing announcement is being edited.
 * @property {string | undefined} id - The ID of the announcement being edited.
 * @property {(value: boolean) => void} setSaving - A function to update the saving state.
 * @property {NavigateFunction} navigate - The navigate function from react-router.
 */
type SubmitAnnouncementArgs = {
  e: React.FormEvent<HTMLFormElement>;
  isEdit: boolean;
  id: string | undefined;
  setSaving: (value: boolean) => void;
  navigate: NavigateFunction;
};

/**
 * Creates a new announcement or updates the existing one from the submitted form,
 * shows a toast with the result and navigates back to the announcements overview.
 *
 * @param {SubmitAnnouncementArgs} args - The form event, edit mode, announcement ID, saving state setter and navigate function.
 */
export const handleAnnouncementSubmit = async ({
  e,
  isEdit,
  id,
  setSaving,
  navigate,
}: SubmitAnnouncementArgs) => {
  e.preventDefault();
  const fd = new FormData(e.currentTarget);
  const body = {
    titleDutch: fd.get("TitleDutch") as string,
    titleEnglish: fd.get("TitleEnglish") as string,
    contentDutch: fd.get("ContentDutch") as string,
    contentEnglish: fd.get("ContentEnglish") as string,
  };

  setSaving(true);

  const submitProcess = async () => {
    try {
      if (isEdit) {
        const response = await putAnnouncementsById({
          path: { id: Number(id) },
          body,
        });
        if (response.error) {
          throw response.error ?? new Error("Failed to update announcement");
        }
      } else {
        const response = await postAnnouncements({ body });
        if (response.error) {
          throw response.error ?? new Error("Failed to create announcement");
        }
      }
      navigate("/announcements");
    } catch (error) {
      console.error(error);
      throw error;
    } finally {
      setSaving(false);
    }
  };

  toast.promise(submitProcess(), {
    loading: isEdit ? t("updating") : t("creating"),
    success: isEdit ? t("update_successful") : t("creation_successful"),
    error: (error) =>
      appendErrorMessage(
        isEdit ? t("update_failed") : t("creation_failed"),
        error,
      ),
  });
};

/**
 * Deletes an announcement from the system and redirects to the list view.
 *
 * @async
 * @param {string} id - The unique identifier of the announcement to be deleted.
 * @param {Function} setDeleting - State setter to track the deletion process.
 * @param {NavigateFunction} navigate - Function to redirect the user after successful deletion.
 */
export const handleDeleteAnnouncement = async (
  id: string,
  setDeleting: (value: boolean) => void,
  navigate: NavigateFunction,
) => {
  setDeleting(true);

  const deleteProcess = async () => {
    try {
      const response = await deleteAnnouncementsById({
        path: { id: Number(id) },
      });
      if (response.error) {
        throw response.error ?? new Error("Failed to delete announcement");
      }
      navigate("/announcements");
    } catch (error) {
      console.error(error);
      throw error;
    } finally {
      setDeleting(false);
    }
  };

  toast.promise(deleteProcess(), {
    loading: t("deleting"),
    success: t("deletion_successful"),
    error: (error) => appendErrorMessage(t("deletion_failed"), error),
  });
};
