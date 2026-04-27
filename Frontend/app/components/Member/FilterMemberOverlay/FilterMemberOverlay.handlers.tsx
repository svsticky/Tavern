import { t } from "i18next";
import toast from "react-hot-toast";
import { getApiStudies, type Study, type StudyType } from "~/api";
import { type MembersFilterDto } from "~/types/MembersFilterDto";

export const loadStudies = async (
  setLoading: (loading: boolean) => void,
  setStudies: (studies: Study[]) => void
) => {
  try {
    setLoading(true);
    const response = await getApiStudies();

    if (response.data) {
      setStudies(response.data);
    }

    if (response.error) {
      throw new Error("Failed to load studies");
    }
  } catch (error) {
    console.error("Failed to load studies", error);
    toast.error(t("failed_to_load_studies"));
  } finally {
    setLoading(false);
  }
};

type ApplyFiltersArgs = {
  onFilter: (filters: MembersFilterDto) => void;
  studyId: number | null;
  gratie: boolean | null;
  lidVanVerdienste: boolean | null;
  ereLid: boolean | null;
  begunstiger: boolean | null;
  suspended: boolean | null;
  inactive: boolean | null;
  studyType: StudyType | null;
};

export const handleApplyFilters = ({
  onFilter,
  studyId,
  gratie,
  lidVanVerdienste,
  ereLid,
  begunstiger,
  suspended,
  inactive,
  studyType
}: ApplyFiltersArgs) => {
  onFilter({
    studyId,
    gratie,
    lidVanVerdienste,
    ereLid,
    begunstiger,
    suspended,
    inactive,
    studyType
  });
};

type ResetFiltersArgs = {
  setStudy: (study: Study | null) => void;
  setGratie: (value: boolean | null) => void;
  setLidVanVerdienste: (value: boolean | null) => void;
  setEreLid: (value: boolean | null) => void;
  setBegunstiger: (value: boolean | null) => void;
  setSuspended: (value: boolean | null) => void;
  setInactive: (value: boolean | null) => void;
  setStudyType: (value: StudyType | null) => void;
};

export const handleResetFilters = ({
  setStudy,
  setGratie,
  setLidVanVerdienste,
  setEreLid,
  setBegunstiger,
  setSuspended,
  setInactive,
  setStudyType
}: ResetFiltersArgs) => {
  setStudy(null);
  setGratie(null);
  setLidVanVerdienste(null);
  setEreLid(null);
  setBegunstiger(null);
  setSuspended(null);
  setInactive(null);
  setStudyType(null);
};
