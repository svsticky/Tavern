import type { NavigateFunction } from "react-router";
import { type ActivityResponseDto, getActivities } from "~/api";

/**
 * Fetches one page of activities for a specific year for administrative
 * purposes.
 *
 * Unlike the standard member view, this explicitly requests both past and
 * future activities to ensure board members have a complete historical and
 * upcoming record for the selected year.
 *
 * @async
 * @param {number} year - The calendar year for which to retrieve activities.
 * @param {number} page - The page number to fetch.
 * @param {number} pageSize - The number of activities to fetch per page.
 * @param {string} [search] - A search term to filter activities by name or location, applied server-side.
 * @throws Throws when the request fails, for the caller (route `clientLoader` or the
 *   infinite-scroll "load more" handler) to handle.
 */
export const fetchAdminActivitiesPage = async (
  year: number,
  page: number,
  pageSize: number,
  search?: string,
): Promise<ActivityResponseDto[]> => {
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
