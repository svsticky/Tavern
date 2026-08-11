import { t } from "i18next";
import toast from "react-hot-toast";
import type { NavigateFunction } from "react-router";
import { type ActivityResponseDto, getActivitiesById } from "~/api";
import type { TokenParsed } from "~/types/TokenParsed";
import { appendErrorMessage } from "~/util/error.util";
import { isBoardOrCandidateBoard } from "~/util/group.util";

/**
 * Arguments for the loadActivityData handler.
 */
type LoadActivityArgs = {
  activityId: number;
  setLoading: (loading: boolean) => void;
  setActivity: (activity: ActivityResponseDto) => void;
};

/**
 * Fetches the details of a specific activity by its ID.
 *
 * @async
 * @param {LoadActivityArgs} args - Configuration, activity ID, and state setter functions.
 */
export const loadActivityData = async ({
  activityId,
  setLoading,
  setActivity,
}: LoadActivityArgs) => {
  try {
    setLoading(true);
    const activitiesResponse = await getActivitiesById({
      path: { id: activityId },
    });

    if (activitiesResponse.error || !activitiesResponse.data)
      throw new Error("Failed to load activity");

    setActivity(activitiesResponse.data);
  } catch (error) {
    console.error("Error while loading data:", error);
    toast.error(appendErrorMessage(t("loading_failed"), error));
  } finally {
    setLoading(false);
  }
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
