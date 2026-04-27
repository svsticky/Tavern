import type React from "react";
import { t } from "i18next";
import toast from "react-hot-toast";
import { getApiStudies, type Study } from "~/api";

export const fetchStudies = async (
  setLoading: (loading: boolean) => void,
  setStudies: React.Dispatch<React.SetStateAction<Study[]>>
) => {
  const fetchStudiesAction = async () => {
    try {
      setLoading(true);
      const response = await getApiStudies();

      if(response.error || !response.data) throw new Error("Failed to fetch studies");

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

type HandleStudyEditedArgs = {
  study?: Study;
  editedStudy?: Study;
  setStudies: React.Dispatch<React.SetStateAction<Study[]>>;
  setIsEditModalOpen: (open: boolean) => void;
  setEditedStudy: (study: Study | undefined) => void;
};

export const handleStudyEdited = ({ study, editedStudy, setStudies, setIsEditModalOpen, setEditedStudy }: HandleStudyEditedArgs) => {
  if (!study) {
    if (editedStudy) {
      setStudies((prev) => prev.filter((s) => s.id !== editedStudy.id));
    }
    setIsEditModalOpen(false);
    setEditedStudy(undefined);
    return;
  }

  if (study.id) {
    setStudies((prev) => prev.map((s) => (s.id === study.id ? study : s)));
    setIsEditModalOpen(false);
    setEditedStudy(undefined);
    return;
  }

  setStudies((prev) => [...prev, { ...study }]);
  setIsEditModalOpen(false);
};
