import type React from "react";
import { t } from "i18next";
import toast from "react-hot-toast";
import { deleteApiStudiesById, postApiStudies, putApiStudiesById, type Study, type StudyType } from "~/api";

/**
 * Data structure for the study creation and edition form.
 * @typedef {Object} EditStudyFormData
 * @property {string} title - The name of the study program.
 * @property {string} type - The degree type (e.g., Bachelor, Master).
 * @property {number} nominalDurationYears - The standard duration of the study in years.
 */
type EditStudyFormData = {
  title: string;
  type: string;
  nominalDurationYears: number;
};

/**
 * Arguments for the handleStudySubmit function.
 * @typedef {Object} SubmitArgs
 * @property {React.FormEvent} e - The form submission event.
 * @property {EditStudyFormData} formData - The current state of the form data.
 * @property {Study} [study] - The existing study object (if in edit mode).
 * @property {function} setLoading - Callback to update the loading state.
 * @property {function} onComplete - Callback executed after a successful API response.
 */
type SubmitArgs = {
  e: React.FormEvent;
  formData: EditStudyFormData;
  study?: Study;
  setLoading: (loading: boolean) => void;
  onComplete: (study?: Study) => void;
};

/**
 * Handles the submission for both creating a new study or updating an existing one.
 * It selects the appropriate API method (POST or PUT) based on the presence of a study object.
 * 
 * @async
 * @param {SubmitArgs} args - The submission configuration and handlers.
 * @returns {Promise<void>}
 */
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

      if(response.error) throw new Error(study ? "Failed to update study" : "Failed to create study");

      onComplete({
        title: formData.title,
        type: formData.type as StudyType,
        nominalDurationYears: formData.nominalDurationYears,
        id: study ? study.id : (response.data as any).id
      });
    } catch (error) {
      console.error("Error creating study:", error);
      throw error;
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
      const response = await deleteApiStudiesById({ path: { id: study.id! } });
      if(response.error) throw new Error("Failed to delete study");
      onComplete();
    } catch (error) {
      console.error("Error deleting study:", error);
      throw error;
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
