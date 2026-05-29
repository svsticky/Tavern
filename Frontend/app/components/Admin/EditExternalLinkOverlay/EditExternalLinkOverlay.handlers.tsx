import { t } from "i18next";
import type React from "react";
import toast from "react-hot-toast";
import {
  deleteExternallinksById,
  postExternallinks,
  postExternallinksByIdIcon,
  putExternallinksById,
  type ExternalLinkResponseDto,
} from "~/api";
import { appendErrorMessage } from "~/util/error.util";

type EditLinkFormData = {
  titleDutch: string;
  titleEnglish: string;
  descriptionDutch: string;
  descriptionEnglish: string;
  url: string;
  sortOrder: number;
};

type SubmitArgs = {
  e: React.FormEvent;
  formData: EditLinkFormData;
  iconFile: File | null;
  link?: ExternalLinkResponseDto;
  setLoading: (loading: boolean) => void;
  onComplete: () => void;
};

export const handleLinkSubmit = async ({
  e,
  formData,
  iconFile,
  link,
  setLoading,
  onComplete,
}: SubmitArgs) => {
  e.preventDefault();

  const editLinkProcess = async () => {
    setLoading(true);
    try {
      let linkId = link?.id;

      if (link) {
        const response = await putExternallinksById({
          path: { id: link.id },
          body: formData,
        });
        if (response.error) {
          throw response.error ?? new Error("Failed to update external link");
        }
      } else {
        const response = await postExternallinks({
          body: {
            titleDutch: formData.titleDutch,
            titleEnglish: formData.titleEnglish,
            descriptionDutch: formData.descriptionDutch,
            descriptionEnglish: formData.descriptionEnglish,
            url: formData.url,
            sortOrder: formData.sortOrder,
          },
        });
        if (response.error || !response.data) {
          throw response.error ?? new Error("Failed to create external link");
        }
        linkId = response.data.id;
      }

      if (iconFile && linkId !== undefined) {
        const uploadResponse = await postExternallinksByIdIcon({
          path: { id: linkId },
          body: { icon: iconFile },
        });
        if (uploadResponse.error) {
          throw uploadResponse.error ?? new Error("Failed to upload icon");
        }
      }

      onComplete();
    } catch (error) {
      console.error("Error submitting external link:", error);
      throw error;
    } finally {
      setLoading(false);
    }
  };

  toast.promise(editLinkProcess(), {
    loading: link ? t("saving") : t("creating_external_link"),
    success: link ? t("saved") : t("external_link_created_successfully"),
    error: (error) => appendErrorMessage(link ? t("failed_to_save") : t("failed_to_create_external_link"), error),
  });
};

type DeleteArgs = {
  link: ExternalLinkResponseDto;
  setLoading: (loading: boolean) => void;
  onComplete: () => void;
};

export const handleLinkDelete = async ({
  link,
  setLoading,
  onComplete,
}: DeleteArgs) => {
  const deleteLinkProcess = async () => {
    setLoading(true);
    try {
      const response = await deleteExternallinksById({
        path: { id: link.id },
      });
      if (response.error) {
        throw response.error ?? new Error("Failed to delete external link");
      }
      onComplete();
    } catch (error) {
      console.error("Error deleting external link:", error);
      throw error;
    } finally {
      setLoading(false);
    }
  };

  toast.promise(deleteLinkProcess(), {
    loading: t("deleting_external_link"),
    success: t("external_link_deleted_successfully"),
    error: (error) => appendErrorMessage(t("failed_to_delete_external_link"), error),
  });
};
