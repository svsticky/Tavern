import { type MemberResponseDto, getMembers } from "~/api";
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
