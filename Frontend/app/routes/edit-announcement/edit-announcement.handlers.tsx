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
  setInitialData: (value: { Title: string; Content: string }) => void;
  setLoading: (value: boolean) => void;
};

/**
 * Fetches existing announcement data from the API to hydrate the edit form.
 *
 * @async
 * @param {LoadAnnouncementArgs} args - Configuration object containing:
 * @param {boolean} args.isEdit - Whether the handler should perform a fetch for existing data.
 * @param {string | undefined} args.id - The ID of the announcement to retrieve.
 * @param {Function} args.setInitialData - Function to update the local form state with fetched data.
 * @param {Function} args.setLoading - Function to update the loading indicator state.
 */
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
        setInitialData({ Title: res.data.title, Content: res.data.content });
      }
    })
    .finally(() => setLoading(false));
};

/**
 * Arguments for the handleAnnouncementSubmit handler.
 */
type SubmitAnnouncementArgs = {
  e: React.FormEvent<HTMLFormElement>;
  isEdit: boolean;
  id: string | undefined;
  setSaving: (value: boolean) => void;
  navigate: NavigateFunction;
};

/**
 * Processes the announcement form submission for both creating and updating records.
 *
 * Extracts data from the form event, determines the correct API method (POST vs PUT),
 * and provides visual feedback using toast promises.
 *
 * @async
 * @param {SubmitAnnouncementArgs} args - Configuration object containing:
 * @param {React.FormEvent<HTMLFormElement>} args.e - The form submission event.
 * @param {boolean} args.isEdit - Determines whether to call the create or update endpoint.
 * @param {string | undefined} args.id - The ID of the announcement to update.
 * @param {Function} args.setSaving - Function to update the saving state.
 * @param {NavigateFunction} args.navigate - Function to redirect the user upon success.
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
    title: fd.get("Title") as string,
    content: fd.get("Content") as string,
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
