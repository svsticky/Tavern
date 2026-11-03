import { getMembers, type MemberResponseDto, type StudyType } from "~/api";
import type { MembersFilterDto } from "~/types/MembersFilterDto";

/** The number of members to fetch per page for infinite scrolling. */
export const MEMBERS_PAGE_SIZE = 20;

/** The filters that are stored as yes/no search parameters. */
const BOOLEAN_FILTERS = [
  "gratie",
  "lidVanVerdienste",
  "ereLid",
  "begunstiger",
  "suspended",
  "inactive",
] as const;

/**
 * Reads the member filters from the URL's search parameters.
 *
 * @param {URLSearchParams} params - The search parameters.
 * @returns {MembersFilterDto | null} The filters, or `null` when none are set.
 */
export const membersFilterFromSearchParams = (
  params: URLSearchParams,
): MembersFilterDto | null => {
  const readBoolean = (name: string) => {
    const value = params.get(name);
    return value === "true" ? true : value === "false" ? false : null;
  };
  const studyId = Number.parseInt(params.get("studyId") ?? "", 10);
  const studyType = params.get("studyType");

  const filters: MembersFilterDto = {
    studyId: Number.isInteger(studyId) ? studyId : null,
    gratie: readBoolean("gratie"),
    lidVanVerdienste: readBoolean("lidVanVerdienste"),
    ereLid: readBoolean("ereLid"),
    begunstiger: readBoolean("begunstiger"),
    suspended: readBoolean("suspended"),
    inactive: readBoolean("inactive"),
    studyType:
      studyType === "Bachelor" || studyType === "Master"
        ? (studyType as StudyType)
        : null,
  };

  return Object.values(filters).some((value) => value !== null)
    ? filters
    : null;
};

/**
 * Writes the member filters to search parameters, removing the ones that aren't set.
 *
 * @param {URLSearchParams} params - The search parameters to update.
 * @param {MembersFilterDto} filters - The filters to store.
 */
export const membersFilterToSearchParams = (
  params: URLSearchParams,
  filters: MembersFilterDto,
) => {
  const set = (name: string, value: string | number | boolean | null) => {
    if (value === null) params.delete(name);
    else params.set(name, String(value));
  };

  set("studyId", filters.studyId);
  set("studyType", filters.studyType);
  for (const name of BOOLEAN_FILTERS) set(name, filters[name]);
};

/**
 * Fetches one page of members matching the search and filters.
 *
 * @param {Object} args - The query.
 * @param {string} args.search - The search term, matched server-side.
 * @param {MembersFilterDto | null} args.filters - The filters, or `null` for none.
 * @param {number} args.page - The page to fetch (1-indexed).
 * @param {number} args.pageSize - The number of members per page.
 * @returns {Promise<MemberResponseDto[]>} The members on that page.
 * @throws {Error} When the members fail to load.
 */
export const fetchAdminMembers = async ({
  search,
  filters,
  page,
  pageSize,
}: {
  search: string;
  filters: MembersFilterDto | null;
  page: number;
  pageSize: number;
}): Promise<MemberResponseDto[]> => {
  const response = await getMembers({
    query: {
      Page: page,
      PageSize: pageSize,
      Search: search,
      StudyId: filters?.studyId || undefined,
      Gratie: filters?.gratie || undefined,
      LidVanVerdienste: filters?.lidVanVerdienste || undefined,
      EreLid: filters?.ereLid || undefined,
      Begunstiger: filters?.begunstiger || undefined,
      Suspended: filters?.suspended || undefined,
      Inactive: filters?.inactive || undefined,
      StudyType: filters?.studyType || undefined,
    },
  });

  if (response.error || !response.data) {
    throw response.error ?? new Error("Failed to fetch members");
  }
  return response.data;
};
