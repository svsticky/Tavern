import type { Study, StudyType } from "~/api"

export type MembersFilterDto = {
    studyId: number | null,
    gratie: boolean | null,
    lidVanVerdienste: boolean | null,
    ereLid: boolean | null,
    begunstiger: boolean | null,
    suspended: boolean | null,
    studyType: StudyType | null
}