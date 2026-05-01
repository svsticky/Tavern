import type { StudyType } from "~/api";

/**
 * DTO for filtering members based on various criteria.
 */
export type MembersFilterDto = {
  studyId: number | null;
  gratie: boolean | null;
  lidVanVerdienste: boolean | null;
  ereLid: boolean | null;
  begunstiger: boolean | null;
  suspended: boolean | null;
  inactive: boolean | null;
  studyType: StudyType | null;
};
