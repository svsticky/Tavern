import { t } from "i18next";
import type React from "react";
import toast from "react-hot-toast";
import {
  deleteRegistrationdocumentsById,
  postRegistrationdocuments,
  putRegistrationdocumentsById,
  type RegistrationDocumentResponseDto,
} from "~/api";
import { appendErrorMessage } from "~/util/error.util";

type EditDocumentFormData = {
  nameDutch: string;
  nameEnglish: string;
  url: string;
  sortOrder: number;
};

type SubmitArgs = {
  e: React.FormEvent;
  formData: EditDocumentFormData;
  document?: RegistrationDocumentResponseDto;
  setLoading: (loading: boolean) => void;
  onComplete: () => void;
};

export const handleDocumentSubmit = async ({
  e,
  formData,
  document,
  setLoading,
  onComplete,
}: SubmitArgs) => {
  e.preventDefault();

  const editDocumentProcess = async () => {
    setLoading(true);
    try {
      if (document) {
        const response = await putRegistrationdocumentsById({
          path: { id: document.id },
          body: formData,
        });
        if (response.error) {
          throw response.message ?? new Error("Failed to update document");
        }
      } else {
        const response = await postRegistrationdocuments({
          body: formData,
        });
        if (response.error || !response.data) {
          throw response.message ?? new Error("Failed to create document");
        }
      }
      onComplete();
    } catch (error) {
      console.error("Error submitting document:", error);
      throw error;
    } finally {
      setLoading(false);
    }
  };

  toast.promise(editDocumentProcess(), {
    loading: document ? t("saving") : t("creating_document"),
    success: document ? t("saved") : t("document_created_successfully"),
    error: (error) =>
      appendErrorMessage(
        document ? t("failed_to_save") : t("failed_to_create_document"),
        error,
      ),
  });
};

type DeleteArgs = {
  document: RegistrationDocumentResponseDto;
  setLoading: (loading: boolean) => void;
  onComplete: () => void;
};

export const handleDocumentDelete = async ({
  document,
  setLoading,
  onComplete,
}: DeleteArgs) => {
  const deleteDocumentProcess = async () => {
    setLoading(true);
    try {
      const response = await deleteRegistrationdocumentsById({
        path: { id: document.id },
      });
      if (response.error) {
        throw response.message ?? new Error("Failed to delete document");
      }
      onComplete();
    } catch (error) {
      console.error("Error deleting document:", error);
      throw error;
    } finally {
      setLoading(false);
    }
  };

  toast.promise(deleteDocumentProcess(), {
    loading: t("deleting_document"),
    success: t("document_deleted_successfully"),
    error: (error) => appendErrorMessage(t("failed_to_delete_document"), error),
  });
};
