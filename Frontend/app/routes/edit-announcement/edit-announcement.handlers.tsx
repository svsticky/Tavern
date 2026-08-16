import { t } from "i18next";
import type React from "react";
import toast from "react-hot-toast";
import type { NavigateFunction } from "react-router";
import {
  deleteAnnouncementsById,
  getAnnouncementsById,
  postAnnouncements,
  putAnnouncementsById,
} from "~/api";
import { appendErrorMessage } from "~/util/error.util";

/**
 * Arguments for the loadAnnouncementData handler.
 */
type LoadAnnouncementArgs = {
  isEdit: boolean;
  id: string | undefined;
  setInitialData: (value: {
    TitleDutch: string;
    TitleEnglish: string;
    ContentDutch: string;
    ContentEnglish: string;
  }) => void;
  setLoading: (value: boolean) => void;
};

export const loadAnnouncementData = async ({
  isEdit,
  id,
  setInitialData,
  setLoading,
}: LoadAnnouncementArgs) => {
  if (!isEdit || !id) return;

  await getAnnouncementsById({ path: { id: Number(id) } })
    .then((res) => {
      if (res.data) {
        setInitialData({
          TitleDutch: res.data.titleDutch,
          TitleEnglish: res.data.titleEnglish,
          ContentDutch: res.data.contentDutch,
          ContentEnglish: res.data.contentEnglish,
        });
      }
    })
    .finally(() => setLoading(false));
};

type SubmitAnnouncementArgs = {
  e: React.FormEvent<HTMLFormElement>;
  isEdit: boolean;
  id: string | undefined;
  setSaving: (value: boolean) => void;
  navigate: NavigateFunction;
};

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
