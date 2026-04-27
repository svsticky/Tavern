import type React from "react";
import { t } from "i18next";
import toast from "react-hot-toast";
import { deleteApiStudiesById, postApiStudies, putApiStudiesById, type Study, type StudyType } from "~/api";

type EditStudyFormData = {
  title: string;
  type: string;
  nominalDurationYears: number;
};

type SubmitArgs = {
  e: React.FormEvent;
  formData: EditStudyFormData;
  study?: Study;
  setLoading: (loading: boolean) => void;
  onComplete: (study?: Study) => void;
};

export const handleStudySubmit = async ({ e, formData, study, setLoading, onComplete }: SubmitArgs) => {
  e.preventDefault();
  if (!formData.title || !formData.type || !formData.nominalDurationYears) {
    return;
  }

  const editStudyProcess = async () => {
    setLoading(true);
    try {
      const response = study
        ? await putApiStudiesById({
            path: { id: study.id! },
            body: {
              title: formData.title,
              type: formData.type as StudyType,
              nominalDurationYears: formData.nominalDurationYears,
            }
          })
        : await postApiStudies({
            body: {
              title: formData.title,
              type: formData.type as StudyType,
              nominalDurationYears: formData.nominalDurationYears,
            }
          });

      if (!response.error) {
        onComplete({
          title: formData.title,
          type: formData.type as StudyType,
          nominalDurationYears: formData.nominalDurationYears,
          id: study ? study.id : (response.data as any).id
        });
      }
    } catch (error) {
      console.error("Error creating study:", error);
    } finally {
      setLoading(false);
    }
  };

  toast.promise(editStudyProcess(), {
    loading: t("creating_study"),
    success: t("study_created_successfully"),
    error: t("failed_to_create_study")
  });
};

type DeleteArgs = {
  study?: Study;
  setLoading: (loading: boolean) => void;
  onComplete: (study?: Study) => void;
};

export const handleStudyDelete = async ({ study, setLoading, onComplete }: DeleteArgs) => {
  if (!study) {
    toast.error(t("no_study_to_delete"));
    return;
  }

  const deleteStudyProcess = async () => {
    setLoading(true);
    try {
      await deleteApiStudiesById({ path: { id: study.id! } });
      onComplete();
    } catch (error) {
      console.error("Error deleting study:", error);
    } finally {
      setLoading(false);
    }
  };

  toast.promise(deleteStudyProcess(), {
    loading: t("deleting_study"),
    success: t("study_deleted_successfully"),
    error: t("failed_to_delete_study")
  });
};
