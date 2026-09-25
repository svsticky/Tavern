import type { NavigateFunction } from "react-router";
import { type ActivityResponseDto, getActivities } from "~/api";

/** Includes past and future activities, so board members get the full record for the year. */
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
