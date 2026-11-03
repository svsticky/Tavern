import type { NavigateFunction } from "react-router";
import { type ActivityResponseDto, getActivities } from "~/api";

/** The number of activities to fetch per page for infinite scrolling. */
export const ACTIVITIES_PAGE_SIZE = 15;

/**
 * Fetches one page of the activities of an association year, for administrative purposes.
 *
 * Unlike the standard member view, this explicitly requests both past and future
 * activities so board members have a complete record for the selected year.
 *
 * @param {Object} args - The query.
 * @param {number} args.year - The association year to fetch the activities of.
 * @param {string} args.search - A search term to filter activities by name or location, applied server-side.
 * @param {number} args.page - The page to fetch (1-indexed).
 * @param {number} args.pageSize - The number of activities per page.
 * @returns {Promise<ActivityResponseDto[]>} The activities on that page.
 * @throws {Error} When the activities fail to load.
 */
export const fetchAdminActivities = async ({
  year,
  search,
  page,
  pageSize,
}: {
  year: number;
  search: string;
  page: number;
  pageSize: number;
}): Promise<ActivityResponseDto[]> => {
  const response = await getActivities({
    query: {
      IncludePast: true,
      IncludeFuture: true,
      Year: year,
      Page: page,
      PageSize: pageSize,
      Search: search || undefined,
    },
  });

  if (response.error || !response.data) {
    throw response.error ?? new Error("Failed to load activities");
  }
  return response.data;
};

/**
 * Navigates to the administrative detail view of a specific activity.
 *
 * This route typically provides additional management features like
 * manual enrollment, payment status editing, or exporting participant lists.
 *
 * @param {NavigateFunction} navigate - React Router navigation function.
 * @param {number} activityId - The unique identifier of the activity to view.
 */
export const handleViewActivity = (
  navigate: NavigateFunction,
  activityId: number,
) => {
  navigate(`/admin/activities/${activityId}`);
};
