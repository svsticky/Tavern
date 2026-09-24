import { type ActivityResponseDto, getActivitiesById } from "~/api";

/**
 * Fetches the activity to edit, for the route's `clientLoader`. Throws on
 * failure so React Router's error boundary handles it.
 */
export const fetchEditActivity = async (
  id: string,
): Promise<ActivityResponseDto> => {
  const response = await getActivitiesById({ path: { id: Number(id) } });
  if (response.error || !response.data)
    throw response.error ?? new Error("Failed to load activity");
  return response.data;
};

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
