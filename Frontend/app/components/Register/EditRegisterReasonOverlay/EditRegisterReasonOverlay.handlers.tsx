import { t } from "i18next";
import type React from "react";
import toast from "react-hot-toast";
import {
  deleteRegisterreasonsById,
  postRegisterreasons,
  postRegisterreasonsByIdIcon,
  putRegisterreasonsById,
  type RegisterReasonResponseDto,
} from "~/api";
import { appendErrorMessage } from "~/util/error.util";

type EditReasonFormData = {
  titleDutch: string;
  titleEnglish: string;
  descriptionDutch: string;
  descriptionEnglish: string;
  sortOrder: number;
};

type SubmitArgs = {
  e: React.FormEvent;
  formData: EditReasonFormData;
  iconFile: File | null;
  reason?: RegisterReasonResponseDto;
  setLoading: (loading: boolean) => void;
  onComplete: () => void;
};

export const handleReasonSubmit = async ({
  e,
  formData,
  iconFile,
  reason,
  setLoading,
  onComplete,
}: SubmitArgs) => {
  e.preventDefault();

  const editReasonProcess = async () => {
    setLoading(true);
    try {
      let reasonId = reason?.id;

      if (reason) {
        const response = await putRegisterreasonsById({
          path: { id: reason.id },
          body: formData,
        });
        if (response.error) {
          throw response.message ?? new Error("Failed to update reason");
        }
      } else {
        const { ...postData } = formData;
        const response = await postRegisterreasons({
          body: postData,
        });
        if (response.error || !response.data) {
          throw response.message ?? new Error("Failed to create reason");
        }
        reasonId = response.data.id;
      }

      if (iconFile && reasonId !== undefined) {
        const uploadResponse = await postRegisterreasonsByIdIcon({
          path: { id: reasonId },
          body: { icon: iconFile },
        });
        if (uploadResponse.error) {
          throw uploadResponse.error ?? new Error("Failed to upload icon");
        }
      }

      onComplete();
    } catch (error) {
      console.error("Error submitting reason:", error);
      throw error;
    } finally {
      setLoading(false);
    }
  };

  toast.promise(editReasonProcess(), {
    loading: reason ? t("saving") : t("creating_reason"),
    success: reason ? t("saved") : t("reason_created_successfully"),
    error: (error) =>
      appendErrorMessage(
        reason ? t("failed_to_save") : t("failed_to_create_reason"),
        error,
      ),
  });
};

type DeleteArgs = {
  reason: RegisterReasonResponseDto;
  setLoading: (loading: boolean) => void;
  onComplete: () => void;
};

export const handleReasonDelete = async ({
  reason,
  setLoading,
  onComplete,
}: DeleteArgs) => {
  const deleteReasonProcess = async () => {
    setLoading(true);
    try {
      const response = await deleteRegisterreasonsById({
        path: { id: reason.id },
      });
      if (response.error) {
        throw response.message ?? new Error("Failed to delete reason");
      }
      onComplete();
    } catch (error) {
      console.error("Error deleting reason:", error);
      throw error;
    } finally {
      setLoading(false);
    }
  };

  toast.promise(deleteReasonProcess(), {
    loading: t("deleting_reason"),
    success: t("reason_deleted_successfully"),
    error: (error) => appendErrorMessage(t("failed_to_delete_reason"), error),
  });
};
