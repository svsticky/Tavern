import { t } from "i18next";
import type React from "react";
import toast from "react-hot-toast";
import {
  deleteRegisterslidesById,
  postRegisterslides,
  postRegisterslidesByIdImage,
  putRegisterslidesById,
  type RegisterSlideResponseDto,
} from "~/api";
import { appendErrorMessage } from "~/util/error.util";

type SubmitArgs = {
  e: React.FormEvent;
  slideFile: File | null;
  slide?: RegisterSlideResponseDto;
  setLoading: (loading: boolean) => void;
  onComplete: () => void;
};

export const handleSlideSubmit = async ({
  e,
  slideFile,
  slide,
  setLoading,
  onComplete,
}: SubmitArgs) => {
  e.preventDefault();

  const editSlideProcess = async () => {
    setLoading(true);
    try {
      let slideId = slide?.id;

      if (slide) {
        const response = await putRegisterslidesById({
          path: { id: slide.id },
        });
        if (response.error) {
          throw response.error ?? new Error("Failed to update slide");
        }
      } else {
        if (!slideFile) {
          throw new Error("Slide image is required");
        }
        const response = await postRegisterslides({
          body: {
            Image: slideFile,
          },
        });
        if (response.error || !response.data) {
          throw response.error ?? new Error("Failed to create slide");
        }
        slideId = response.data.id;
      }

      if (slideFile && slide) {
        const uploadResponse = await postRegisterslidesByIdImage({
          path: { id: slideId! },
          body: { image: slideFile },
        });
        if (uploadResponse.error) {
          throw (
            uploadResponse.error ?? new Error("Failed to upload slide image")
          );
        }
      }

      onComplete();
    } catch (error) {
      console.error("Error submitting slide:", error);
      throw error;
    } finally {
      setLoading(false);
    }
  };

  toast.promise(editSlideProcess(), {
    loading: slide ? t("saving") : t("creating_slide"),
    success: slide ? t("saved") : t("slide_created_successfully"),
    error: (error) =>
      appendErrorMessage(
        slide ? t("failed_to_save") : t("failed_to_create_slide"),
        error,
      ),
  });
};

type DeleteArgs = {
  slide: RegisterSlideResponseDto;
  setLoading: (loading: boolean) => void;
  onComplete: () => void;
};

export const handleSlideDelete = async ({
  slide,
  setLoading,
  onComplete,
}: DeleteArgs) => {
  const deleteSlideProcess = async () => {
    setLoading(true);
    try {
      const response = await deleteRegisterslidesById({
        path: { id: slide.id },
      });
      if (response.error) {
        throw response.error ?? new Error("Failed to delete slide");
      }
      onComplete();
    } catch (error) {
      console.error("Error deleting slide:", error);
      throw error;
    } finally {
      setLoading(false);
    }
  };

  toast.promise(deleteSlideProcess(), {
    loading: t("deleting_slide"),
    success: t("slide_deleted_successfully"),
    error: (error) => appendErrorMessage(t("failed_to_delete_slide"), error),
  });
};
