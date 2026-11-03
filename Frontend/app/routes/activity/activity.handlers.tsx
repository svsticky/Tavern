import { t } from "i18next";
import toast from "react-hot-toast";
import type { NavigateFunction } from "react-router";
import { type ActivityResponseDto, getActivitiesById, getGroupsById } from "~/api";
import type { TokenParsed } from "~/types/TokenParsed";
import { appendErrorMessage } from "~/util/error.util";
import { requireTokenParsed } from "~/util/loaderAuth.util";

/**
 * The data the activity detail page's `clientLoader` provides.
 * @typedef {Object} ActivityLoaderData
 * @property {TokenParsed} tokenParsed - The parsed token of the logged-in user.
 * @property {ActivityResponseDto} activity - The activity being shown.
 * @property {string | null} organizerName - The name of the organizing group, or `null` when it has none.
 */
export type ActivityLoaderData = {
  tokenParsed: TokenParsed;
  activity: ActivityResponseDto;
  organizerName: string | null;
};

/**
 * Loads everything the activity detail page needs: the user's token, the activity
 * and the name of its organizing group.
 *
 * @param {{ id?: string }} params - The route parameters, containing the activity ID.
 * @returns {Promise<ActivityLoaderData>} The loader data for the activity page.
 */
export const loadActivityLoaderData = async (params: {
  id?: string;
}): Promise<ActivityLoaderData> => {
  const tokenParsed = await requireTokenParsed();
  const activitiesResponse = await getActivitiesById({
    path: { id: Number(params.id) },
  });
  if (activitiesResponse.error || !activitiesResponse.data)
    throw new Error("Failed to load activities");

  // A missing organizer name shouldn't break the page, so this one doesn't throw
  const organizerId = activitiesResponse.data.organizerId;
  const organizerResponse = organizerId
    ? await getGroupsById({ path: { id: organizerId } })
    : null;

  return {
    tokenParsed,
    activity: activitiesResponse.data,
    organizerName: organizerResponse?.data?.name ?? null,
  };
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
