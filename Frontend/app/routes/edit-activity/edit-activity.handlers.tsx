import { t } from "i18next";
import toast from "react-hot-toast";
import { getApiActivitiesById, type ActivityResponseDto } from "~/api";

type LoadEditActivityArgs = {
  isEdit: boolean;
  id: string | undefined;
  setActivity: (activity: ActivityResponseDto) => void;
  setLoading: (loading: boolean) => void;
};

export const loadEditActivityData = async ({ isEdit, id, setActivity, setLoading }: LoadEditActivityArgs) => {
  try {
    if (isEdit) {
      const activityRes = await getApiActivitiesById({ path: { id: Number(id) } });
      if (activityRes.error || !activityRes.data) throw new Error("Failed to load activity");
      setActivity(activityRes.data);
    }
  } catch (error) {
    console.error("Error loading data:", error);
    toast.error(t("loading_failed"));
  } finally {
    setLoading(false);
  }
};

export const getEditActivityBackPath = (pathname: string, isEdit: boolean, id: string | undefined) =>
  `${pathname.startsWith("/admin") ? "/admin" : ""}${isEdit ? `/activities/${id}` : "/activities"}`;
