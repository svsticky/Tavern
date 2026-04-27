import { t } from "i18next";
import toast from "react-hot-toast";
import type { NavigateFunction } from "react-router";
import { getApiActivitiesById, type ActivityResponseDto } from "~/api";
import { isBoardOrCandidateBoard } from "~/util/group.util";

type LoadActivityArgs = {
  initialized: boolean;
  authenticated: boolean | undefined;
  activityId: number;
  setLoading: (loading: boolean) => void;
  setActivity: (activity: ActivityResponseDto) => void;
};

export const loadActivityData = async ({ initialized, authenticated, activityId, setLoading, setActivity }: LoadActivityArgs) => {
  if (!initialized || !authenticated) return;

  try {
    setLoading(true);
    const activitiesResponse = await getApiActivitiesById({
      path: { id: activityId }
    });

    if(activitiesResponse.error || !activitiesResponse.data) throw new Error("Failed to load activity");
    
    setActivity(activitiesResponse.data);
  } catch (error) {
    console.error("Error while loading data:", error);
    toast.error(t("loading_failed"));
  } finally {
    setLoading(false);
  }
};

export const canEditActivity = (activity: ActivityResponseDto, tokenParsed: any) => {
  return (
    isBoardOrCandidateBoard(tokenParsed) ||
    (!activity.showInKoala && !activity.showOnWebsite && activity.organizerId && new Date(activity.dateTimeStart) > new Date(Date.now()))
  );
};

export const getActivityBackPath = (pathname: string) => `${pathname.startsWith("/admin") ? "/admin" : ""}/activities`;

export const handleEditActivityClick = (navigate: NavigateFunction, pathname: string, activityId: number) => {
  navigate(`${pathname.startsWith("/admin") ? "/admin" : ""}/activities/edit/${activityId}`);
};
