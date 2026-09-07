import { t } from "i18next";
import toast from "react-hot-toast";
import { type ActivityResponseDto, getActivitiesById } from "~/api";
import { appendErrorMessage } from "~/util/error.util";

/**
 * Arguments for the loadEditActivityData handler.
 */
type LoadEditActivityArgs = {
  isEdit: boolean;
  id: string | undefined;
  cloneFromId?: string | null;
  setActivity: (activity: ActivityResponseDto) => void;
  setLoading: (loading: boolean) => void;
};

/**
 * Hydrates the Activity editor with existing data if the page is in edit mode or clone mode.
 *
 * If `isEdit` is true, this handler performs a network request to fetch the activity
 * details by ID. If `cloneFromId` is provided in creation mode, it fetches the source
 * activity and populates the form with cloned values and updated dates.
 *
 * @async
 * @param {LoadEditActivityArgs} args - Configuration object
 */
export const loadEditActivityData = async ({
  isEdit,
  id,
  cloneFromId,
  setActivity,
  setLoading,
}: LoadEditActivityArgs) => {
  try {
    if (isEdit && id) {
      const activityRes = await getActivitiesById({
        path: { id: Number(id) },
      });
      if (activityRes.error || !activityRes.data)
        throw new Error("Failed to load activity");
      setActivity(activityRes.data);
    } else if (cloneFromId) {
      const activityRes = await getActivitiesById({
        path: { id: Number(cloneFromId) },
      });
      if (activityRes.error || !activityRes.data)
        throw new Error("Failed to load activity to clone");

      const source = activityRes.data;
      const originalStart = new Date(source.dateTimeStart).getTime();
      const originalEnd = new Date(source.dateTimeEnd).getTime();
      const duration = Math.max(
        originalEnd - originalStart,
        2 * 60 * 60 * 1000,
      );

      const nextWeek = new Date(Date.now() + 7 * 24 * 60 * 60 * 1000);
      const newStart = new Date(nextWeek);
      const sourceStartDate = new Date(source.dateTimeStart);
      newStart.setHours(
        sourceStartDate.getHours(),
        sourceStartDate.getMinutes(),
        0,
        0,
      );
      const newEnd = new Date(newStart.getTime() + duration);

      const clonedActivity: ActivityResponseDto = {
        ...source,
        id: 0,
        name: `${source.name} (${t("copy")})`,
        dateTimeStart: newStart.toISOString(),
        dateTimeEnd: newEnd.toISOString(),
        enrollmentDeadline: null,
        unenrollmentDeadline: null,
        enrollOpenDate: null,
        posterFileName: null,
        posterPath: null,
        enrollments: [],
        isArchived: false,
        showInKoala: false,
        specificationQuestions: (source.specificationQuestions || []).map(
          (q) => ({
            ...q,
            id: undefined as unknown as number,
            options: q.options ? [...q.options] : [],
          }),
        ),
      };
      setActivity(clonedActivity);
    }
  } catch (error) {
    console.error("Error loading data:", error);
    toast.error(appendErrorMessage(t("loading_failed"), error));
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
