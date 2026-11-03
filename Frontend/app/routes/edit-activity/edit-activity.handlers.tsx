import { getActivitiesById, getGroups, type ActivityResponseDto, type GroupResponseDto } from "~/api";
import type { TokenParsed } from "~/types/TokenParsed";
import { requireTokenParsed } from "~/util/loaderAuth.util";

/**
 * The data the activity editor's `clientLoader` provides.
 * @typedef {Object} EditActivityLoaderData
 * @property {TokenParsed} tokenParsed - The parsed token of the logged-in user.
 * @property {ActivityResponseDto | null} activity - The activity being edited, or `null` when creating a new one.
 * @property {GroupResponseDto[]} groups - All groups that can organize the activity.
 */
export type EditActivityLoaderData = {
  tokenParsed: TokenParsed;
  activity: ActivityResponseDto | null;
  groups: GroupResponseDto[];
}

/**
 * Loads everything the activity editor needs: the user's token, all groups and,
 * in edit mode, the activity being edited.
 * Redirects to the login page when the user isn't logged in.
 *
 * @param {{ id?: string }} params - The route parameters; `id` is set when editing an existing activity.
 * @returns {Promise<EditActivityLoaderData>} The loader data for the activity editor.
 * @throws {Error} When the groups or the activity fail to load, which shows the route's error page.
 */
export const loadEditActivityLoaderData = async (params: { id?: string }) => {
  const tokenParsed = await requireTokenParsed();
  const groupsResponse = await getGroups();
  if (groupsResponse.error || !groupsResponse.data) throw new Error("Failed to load groups");

  if (!params.id) {
    return { tokenParsed, activity: null, groups: groupsResponse.data };
  }

  const activitiesResponse = await getActivitiesById({
      path: { id: Number(params.id) },
  });
  if (activitiesResponse.error || !activitiesResponse.data) throw new Error("Failed to load activities");

  return { tokenParsed, activity: activitiesResponse.data, groups: groupsResponse.data };
}

/**
 * Calculates the appropriate 'Back' navigation path based on the user's current context.
 *
 * This utility ensures that administrators are returned to the admin dashboard,
 * while standard members are returned to the public activity list or specific detail view.
 *
 * @param {string} pathname - The current URL path from the window location.
 * @param {boolean} isEdit - Whether the user is currently editing an existing activity.
 * @param {string | undefined} id - The ID of the activity (used if returning from edit mode to detail view).
 * @returns {string} The relative URL string for navigation.
 */
export const getEditActivityBackPath = (
  pathname: string,
  isEdit: boolean,
  id: string | undefined,
) =>
  `${pathname.startsWith("/admin") ? "/admin" : ""}${isEdit ? `/activities/${id}` : "/activities"}`;
