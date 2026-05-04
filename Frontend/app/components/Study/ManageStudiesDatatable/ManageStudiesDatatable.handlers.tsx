import { t } from "i18next";
import type React from "react";
import toast from "react-hot-toast";
import { getApiStudies, type Study } from "~/api";

/**
 * Fetches the list of studies from the API and updates the provided state.
 *
 * @async
 * @param {function} setLoading - State setter to track the loading status of the API request.
 * @param {React.Dispatch<React.SetStateAction<Study[]>>} setStudies - State setter to update the list of study programs.
 * @returns {Promise<void>}
 */
export const fetchStudies = async (
  setLoading: (loading: boolean) => void,
  setStudies: React.Dispatch<React.SetStateAction<Study[]>>,
) => {
  const fetchStudiesAction = async () => {
    try {
      setLoading(true);
      const response = await getApiStudies();

      if (response.error || !response.data)
        throw new Error("Failed to fetch studies");

      setStudies(response.data);
    } catch (error) {
      console.error("Error fetching studies:", error);
      toast.error(t("error_fetching_studies"));
    } finally {
      setLoading(false);
    }
  };

  fetchStudiesAction();
};

/**
 * Arguments for the handleStudyEdited function.
 * @typedef {Object} HandleStudyEditedArgs
 * @property {Study} [study] - The updated study object from the form. If undefined, signifies a deletion.
 * @property {Study} [editedStudy] - The original study object that was being edited.
 * @property {React.Dispatch<React.SetStateAction<Study[]>>} setStudies - State setter for the studies list.
 * @property {function} setIsEditModalOpen - State setter to close the edit modal.
 * @property {function} setEditedStudy - State setter to clear the current selection.
 */
type HandleStudyEditedArgs = {
  study?: Study;
  editedStudy?: Study;
  setStudies: React.Dispatch<React.SetStateAction<Study[]>>;
  setIsEditModalOpen: (open: boolean) => void;
  setEditedStudy: (study: Study | undefined) => void;
};

/**
 * Orchestrates the local state updates after a study has been created, edited, or deleted.
 * It handles three scenarios:
 * 1. **Deletion**: If `study` is missing, it removes `editedStudy` from the list.
 * 2. **Update**: If `study` has an ID, it replaces the existing item in the list.
 * 3. **Creation**: If `study` is new (no ID or matching ID), it appends it to the list.
 *
 * @param {HandleStudyEditedArgs} args - The state handlers and data objects for the update.
 */
export const handleStudyEdited = ({
  study,
  editedStudy,
  setStudies,
  setIsEditModalOpen,
  setEditedStudy,
}: HandleStudyEditedArgs) => {
  if (!study) {
    if (editedStudy) {
      setStudies((prev) => prev.filter((s) => s.id !== editedStudy.id));
    }
    setIsEditModalOpen(false);
    setEditedStudy(undefined);
    return;
  }

  setStudies((prev) => {
    const exists = prev.find((s) => s.id === study.id);
    
    if (exists) {
      return prev.map((s) => (s.id === study.id ? study : s));
    } else {
      return [...prev, study];
    }
  });

  setIsEditModalOpen(false);
  setEditedStudy(undefined);
};
