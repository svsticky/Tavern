import { t } from "i18next";
import toast from "react-hot-toast";
import type { NavigateFunction } from "react-router";
import { type ActivityResponseDto, getActivitiesById } from "~/api";
import type { TokenParsed } from "~/types/TokenParsed";
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
    toast.error(t("loading_failed"));
  } finally {
    setLoading(false);
  }
};

/**
 * Determines if the current user has permission to edit a specific activity.
 *
 * Logic:
 * - **Board Members**: Always allowed to edit.
 * - **Organizers**: Allowed only if the activity hasn't been finalized for
 *   external systems (Website/Koala) and the event hasn't started yet.
 *
 * @param {ActivityResponseDto} activity - The activity data to check against.
 * @param {TokenParsed} tokenParsed - The parsed token containing user roles and ID.
 * @returns {boolean} True if the user is authorized to edit.
 */
export const canEditActivity = (
  activity: ActivityResponseDto,
  tokenParsed: TokenParsed,
  boardGroupId: number | null,
  candidateBoardGroupId: number | null,
) => {
  return (
    isBoardOrCandidateBoard(tokenParsed, boardGroupId, candidateBoardGroupId) ||
    Boolean(
      !activity.showInKoala &&
        !activity.showOnWebsite &&
        activity.organizerId &&
        new Date(activity.dateTimeStart) > new Date(Date.now()),
    )
  );
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
