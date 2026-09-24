import { getMembers, type MemberResponseDto } from "~/api";
import type { MembersFilterDto } from "~/types/MembersFilterDto";

/** The number of members to fetch per page for infinite scrolling. */
export const PAGE_SIZE = 20;

/**
 * Fetches one page of members, optionally narrowed by search text and the
 * admin filter panel's criteria.
 *
 * Throws on failure - the route `clientLoader`'s initial-page call relies on
 * this to reach React Router's error boundary; the search/filter/load-more
 * refetch paths in the component catch it themselves to show a toast instead.
 */
export async function fetchMembersPage(
  page: number,
  search: string,
  filters: MembersFilterDto | null,
): Promise<MemberResponseDto[]> {
  const response = await getMembers({
    query: {
      Page: page,
      PageSize: PAGE_SIZE,
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
}

/** URL search param holding the admin filter panel's criteria as JSON. */
export const FILTERS_PARAM = "filters";

/** URL search param holding the search text. */
export const SEARCH_PARAM = "q";

/**
 * Serializes the filter panel's criteria for the URL, dropping unset fields.
 * Returns null when nothing is filtered, so the param can be removed.
 */
export function serializeMembersFilters(
  filters: MembersFilterDto | null,
): string | null {
  if (!filters) return null;
  const set = Object.fromEntries(
    Object.entries(filters).filter(([, v]) => v !== null && v !== undefined),
  );
  return Object.keys(set).length > 0 ? JSON.stringify(set) : null;
}

/**
 * Parses the `filters` URL param back into filter panel criteria. A missing,
 * malformed or hand-edited value is treated as "no filters" rather than
 * throwing, since it comes straight from the address bar.
 */
export function parseMembersFilters(
  value: string | null,
): MembersFilterDto | null {
  if (!value) return null;
  try {
    const parsed: unknown = JSON.parse(value);
    if (
      typeof parsed !== "object" ||
      parsed === null ||
      Array.isArray(parsed)
    ) {
      return null;
    }
    const f = parsed as Partial<MembersFilterDto>;
    return {
      studyId: f.studyId ?? null,
      gratie: f.gratie ?? null,
      lidVanVerdienste: f.lidVanVerdienste ?? null,
      ereLid: f.ereLid ?? null,
      begunstiger: f.begunstiger ?? null,
      suspended: f.suspended ?? null,
      inactive: f.inactive ?? null,
      studyType: f.studyType ?? null,
    };
  } catch {
    return null;
  }
}
