import type { NavigateFunction } from "react-router";
import {
  type ActivityResponseDto,
  getActivitiesById,
  getGroupsById,
} from "~/api";

/**
 * Fetches the details of a specific activity by its ID, for the route's
 * `clientLoader`. Throws on failure so React Router's error boundary handles it.
 */
export const fetchActivity = async (
  activityId: number,
): Promise<ActivityResponseDto> => {
  const response = await getActivitiesById({ path: { id: activityId } });

  if (response.error || !response.data)
    throw response.error ?? new Error("Failed to load activity");

  return response.data;
};

/**
 * Looks up the name of the group organizing an activity. The name is only
 * decoration, so a failed lookup yields `null` rather than failing the page.
 */
export const fetchOrganizerName = async (
  organizerId: number | null | undefined,
): Promise<string | null> => {
  if (!organizerId) return null;

  const response = await getGroupsById({ path: { id: organizerId } });
  return response.data?.name ?? null;
};

/**
 * Generates the appropriate "back" path based on the user's current routing context.
 *
 * @param {string} pathname - The current URL path.
 * @returns {string} Either the administrative or standard activity listing path.
 */
export const getActivityBackPath = (pathname: string) =>
  `${pathname.startsWith("/admin") ? "/admin" : ""}/activities`;

/**
 * Navigates to the edit form for the specific activity, maintaining administrative context if applicable.
 *
 * @param {NavigateFunction} navigate - React Router navigation function.
 * @param {string} pathname - The current URL path to detect context.
 * @param {number} activityId - The ID of the activity to edit.
 */
export const handleEditActivityClick = (
  navigate: NavigateFunction,
  pathname: string,
  activityId: number,
) => {
  navigate(
    `${pathname.startsWith("/admin") ? "/admin" : ""}/activities/edit/${activityId}`,
  );
};
