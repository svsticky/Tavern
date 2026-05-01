import { t } from "i18next";
import toast from "react-hot-toast";
import type { NavigateFunction } from "react-router";
import { getApiActivities, type ActivityResponseDto } from "~/api";

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
  setActivities: (activities: ActivityResponseDto[]) => void
) => {
  try {
    setLoading(true);
    const response = await getApiActivities({
      query: {
        IncludePast: true,
        IncludeFuture: true,
        Year: year
      }
    });

    if(response.error || !response.data) throw new Error("Failed to load activities");

    setActivities(response.data);
  } catch (error) {
    console.error("Error fetching activities:", error);
    toast.error(t("loading_failed"));
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
export const handleViewActivity = (navigate: NavigateFunction, activityId: number) => {
  navigate(`/admin/activities/${activityId}`);
};
