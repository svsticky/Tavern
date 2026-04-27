import type React from "react";
import type { NavigateFunction } from "react-router";
import { t } from "i18next";
import toast from "react-hot-toast";
import { deleteApiAnnouncementsById, getApiAnnouncementsById, postApiAnnouncements, putApiAnnouncementsById } from "~/api";

type LoadAnnouncementArgs = {
  isEdit: boolean;
  id: string | undefined;
  setInitialData: (value: { Title: string; Content: string }) => void;
  setLoading: (value: boolean) => void;
};

export const loadAnnouncementData = async ({ isEdit, id, setInitialData, setLoading }: LoadAnnouncementArgs) => {
  if (!isEdit || !id) return;

  await getApiAnnouncementsById({ path: { id: Number(id) } })
    .then((res) => {
      if (res.data) {
        setInitialData({ Title: res.data.title, Content: res.data.content });
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

export const handleAnnouncementSubmit = async ({ e, isEdit, id, setSaving, navigate }: SubmitAnnouncementArgs) => {
  e.preventDefault();
  const fd = new FormData(e.currentTarget);
  const body = {
    title: fd.get("Title") as string,
    content: fd.get("Content") as string
  };

  setSaving(true);

  const submitProcess = async () => {
    try {
      if (isEdit) {
        const response = await putApiAnnouncementsById({ path: { id: Number(id) }, body });
        if(response.error) throw new Error("Failed to update announcement");
      } else {
        const response = await postApiAnnouncements({ body });
        if(response.error) throw new Error("Failed to create announcement");
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
    error: isEdit ? t("update_failed") : t("creation_failed")
  });
};

export const handleDeleteAnnouncement = async (id: string, setDeleting: (value: boolean) => void, navigate: NavigateFunction) => {
  setDeleting(true);

  const deleteProcess = async () => {
    try {
      const response = await deleteApiAnnouncementsById({ path: { id: Number(id) } });
      if(response.error) throw new Error("Failed to delete announcement");
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
    error: t("deletion_failed")
  });
};