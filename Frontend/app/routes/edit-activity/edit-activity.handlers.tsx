import { t } from "i18next";
import toast from "react-hot-toast";
import { type ActivityResponseDto, getActivitiesById } from "~/api";

/**
 * Arguments for the loadEditActivityData handler.
 */
type LoadEditActivityArgs = {
  isEdit: boolean;
  id: string | undefined;
  setActivity: (activity: ActivityResponseDto) => void;
  setLoading: (loading: boolean) => void;
};

/**
 * Hydrates the Activity editor with existing data if the page is in edit mode.
 *
 * If `isEdit` is true, this handler performs a network request to fetch the activity
 * details by ID. If the fetch fails, it triggers an error toast. In creation mode,
 * it simply toggles the loading state off.
 *
 * @async
 * @param {LoadEditActivityArgs} args - Configuration object containing:
 * @param {boolean} args.isEdit - Whether the handler should fetch existing data.
 * @param {string | undefined} args.id - The ID of the activity to retrieve.
 * @param {Function} args.setActivity - Function to update the local activity state.
 * @param {Function} args.setLoading - Function to update the loading indicator state.
 */
export const loadEditActivityData = async ({
  isEdit,
  id,
  setActivity,
  setLoading,
}: LoadEditActivityArgs) => {
  try {
    if (isEdit) {
      const activityRes = await getActivitiesById({
        path: { id: Number(id) },
      });
      if (activityRes.error || !activityRes.data)
        throw new Error("Failed to load activity");
      setActivity(activityRes.data);
    }
  } catch (error) {
    console.error("Error loading data:", error);
    toast.error(t("loading_failed"));
  } finally {
    setLoading(false);
  }
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
