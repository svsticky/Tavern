import { getMembers, type MemberResponseDto } from "~/api";
import type { MembersFilterDto } from "~/types/MembersFilterDto";

/** The number of members to fetch per page for infinite scrolling. */
export const PAGE_SIZE = 20;

/** Throws on failure, so loaders reach the error boundary; load-more and search callers catch it to show a toast. */
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

export const FILTERS_PARAM = "filters";

export const SEARCH_PARAM = "q";

/** Returns null when nothing is filtered, so the param can be dropped. */
export function serializeMembersFilters(
  filters: MembersFilterDto | null,
): string | null {
  if (!filters) return null;
  const set = Object.fromEntries(
    Object.entries(filters).filter(([, v]) => v !== null && v !== undefined),
  );
  return Object.keys(set).length > 0 ? JSON.stringify(set) : null;
}

/** A missing or malformed value (it comes from the address bar) means "no filters". */
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
