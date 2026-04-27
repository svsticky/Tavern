import { t } from "i18next";
import toast from "react-hot-toast";
import type { NavigateFunction } from "react-router";
import { getApiActivities, type ActivityResponseDto } from "~/api";

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

export const handleViewActivity = (navigate: NavigateFunction, activityId: number) => {
  navigate(`/admin/activities/${activityId}`);
};
