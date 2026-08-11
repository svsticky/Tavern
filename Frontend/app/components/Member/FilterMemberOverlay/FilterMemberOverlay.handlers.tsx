import { t } from "i18next";
import toast from "react-hot-toast";
import { getStudies, type Study, type StudyType } from "~/api";
import type { MembersFilterDto } from "~/types/MembersFilterDto";
import { appendErrorMessage } from "~/util/error.util";

/**
 * Fetches the list of available studies from the API.
 *
 * @async
 * @function loadStudies
 * @param {Function} setLoading - State setter to track the loading status.
 * @param {Function} setStudies - State setter to store the retrieved array of {@link Study} objects.
 * @throws Will display a toast notification and log to console if the API request fails.
 * @returns {Promise<void>}
 */
export const loadStudies = async (
  setLoading: (loading: boolean) => void,
  setStudies: (studies: Study[]) => void,
) => {
  try {
    setLoading(true);
    const response = await getStudies();

    if (response.error || !response.data) {
      throw response.message ?? new Error("Failed to load studies");
    }

    setStudies(response.data);
  } catch (error) {
    console.error("Failed to load studies", error);
    toast.error(appendErrorMessage(t("failed_to_load_studies"), error));
  } finally {
    setLoading(false);
  }
};

/**
 * Arguments for the {@link handleApplyFilters} function.
 */
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

/**
 * Compiles individual filter states into a single DTO and triggers the filter callback.
 *
 * @function handleApplyFilters
 * @param {ApplyFiltersArgs} args - The filter state values and callback.
 * @returns {void}
 */
export const handleApplyFilters = ({
  onFilter,
  studyId,
  gratie,
  lidVanVerdienste,
  ereLid,
  begunstiger,
  suspended,
  inactive,
  studyType,
}: ApplyFiltersArgs) => {
  onFilter({
    studyId,
    gratie,
    lidVanVerdienste,
    ereLid,
    begunstiger,
    suspended,
    inactive,
    studyType,
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
  setStudyType,
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
