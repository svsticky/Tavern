import { t } from "i18next";
import toast from "react-hot-toast";
import type { NavigateFunction } from "react-router";
import {
  type ActivityResponseDto,
  deleteActivitiesById,
  getActivities,
} from "~/api";
import { appendErrorMessage } from "~/util/error.util";

/**
 * Fetches all activities for a specific year for administrative purposes.
 *
 * Unlike the standard member view, this handler explicitly requests both
 * past and future activities to ensure board members have a complete
 * historical and upcoming record for the selected year.
 *
 * @async
 * @param {number} year - The calendar year for which to retrieve activities.
 * @param {(loading: boolean) => void} setLoading - State setter to track the network request.
 * @param {(activities: ActivityResponseDto[]) => void} setActivities - State setter to store the retrieved activity list.
 */
export const loadAdminActivities = async (
  year: number,
  setLoading: (loading: boolean) => void,
  setActivities: (activities: ActivityResponseDto[]) => void,
  page?: number,
  pageSize?: number,
  includePast = true,
  isArchived = false,
) => {
  try {
    setLoading(true);
    const response = await getActivities({
      query: {
        IncludePast: includePast,
        IncludeFuture: true,
        Year: year,
        Page: page,
        PageSize: pageSize,
        IsArchived: isArchived,
      },
    });

    if (response.error || !response.data) {
      throw response.error ?? new Error("Failed to load activities");
    }

    setActivities(response.data);
  } catch (error) {
    console.error("Error fetching activities:", error);
    toast.error(appendErrorMessage(t("loading_failed"), error));
  } finally {
    setLoading(false);
  }
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

/**
 * Deletes an activity from the administrative overview after user confirmation.
 *
 * @async
 * @param {number} activityId - The unique identifier of the activity to delete.
 * @param {(message: string, options?: { title?: string; confirmLabel?: string; cancelLabel?: string; variant?: "primary" | "secondary" | "danger" }) => Promise<boolean>} confirm - Modal confirmation function.
 * @param {() => void} onSuccess - Callback invoked after the activity is deleted successfully.
 */
export const handleDeleteAdminActivity = async (
  activityId: number,
  confirm: (
    message: string,
    options?: {
      title?: string;
      confirmLabel?: string;
      cancelLabel?: string;
      variant?: "primary" | "secondary" | "danger";
    },
  ) => Promise<boolean>,
  onSuccess: () => void,
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

    onSuccess();
  };

  toast.promise(deleteProcess(), {
    loading: t("deleting"),
    success: t("activity_deleted_successfully"),
    error: (error) => appendErrorMessage(t("failed_to_delete_activity"), error),
  });
};
