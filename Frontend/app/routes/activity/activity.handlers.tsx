import { t } from "i18next";
import toast from "react-hot-toast";
import type { NavigateFunction } from "react-router";
import {
  type ActivityResponseDto,
  deleteActivitiesById,
  getActivitiesById,
} from "~/api";
import { appendErrorMessage } from "~/util/error.util";

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

/**
 * Deletes a specific activity by its ID after user confirmation, then navigates back to the activity listing.
 *
 * @async
 * @param {number} activityId - The unique identifier of the activity to delete.
 * @param {NavigateFunction} navigate - React Router navigation function.
 * @param {string} pathname - The current URL path to detect context.
 * @param {(message: string, options?: { title?: string; confirmLabel?: string; cancelLabel?: string; variant?: "primary" | "secondary" | "danger" }) => Promise<boolean>} confirm - Modal confirmation function.
 */
export const handleDeleteActivity = async (
  activityId: number,
  navigate: NavigateFunction,
  pathname: string,
  confirm: (
    message: string,
    options?: {
      title?: string;
      confirmLabel?: string;
      cancelLabel?: string;
      variant?: "primary" | "secondary" | "danger";
    },
  ) => Promise<boolean>,
) => {
  if (
    !(await confirm(t("delete_activity_confirmation"), {
      title: t("delete_activity"),
      variant: "danger",
    }))
  ) {
    return;
  }

  const deleteProcess = async () => {
    const response = await deleteActivitiesById({
      path: { id: activityId },
    });

    if (response.error) {
      throw response.error ?? new Error("Failed to delete activity");
    }

    navigate(getActivityBackPath(pathname));
  };

  toast.promise(deleteProcess(), {
    loading: t("deleting"),
    success: t("activity_deleted_successfully"),
    error: (error) => appendErrorMessage(t("failed_to_delete_activity"), error),
  });
};
