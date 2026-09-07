import { t } from "i18next";
import toast from "react-hot-toast";
import type { NavigateFunction } from "react-router";
import { type ActivityResponseDto, getActivities } from "~/api";
import { getEnv } from "~/util/config.utils";
import { appendErrorMessage } from "~/util/error.util";
import { generateA3Pdf } from "~/util/pdf.util";
export type ActivityFilter = "all" | "enrolled" | "history";

/**
 * Arguments for the loadActivities handler.
 */
type LoadActivitiesArgs = {
  setLoading: (loading: boolean) => void;
  setActivities: (activities: ActivityResponseDto[]) => void;
  filter?: ActivityFilter;
  userId?: string;
};

/**
 * Fetches the list of activities from the API based on the selected filter.
 *
 * @async
 * @param {LoadActivitiesArgs} args - Configuration and state setter functions.
 */
export const loadActivities = async ({
  setLoading,
  setActivities,
  filter = "all",
  userId,
}: LoadActivitiesArgs) => {
  try {
    setLoading(true);

    const query: {
      IncludePast?: boolean;
      IncludeFuture?: boolean;
      UserId?: string;
    } = {
      IncludePast: false,
      IncludeFuture: true,
    };

    if (filter === "enrolled" && userId) {
      query.UserId = userId;
      query.IncludePast = false;
      query.IncludeFuture = true;
    } else if (filter === "history" && userId) {
      query.UserId = userId;
      query.IncludePast = true;
      query.IncludeFuture = false;
    }

    const activitiesResponse = await getActivities({
      query,
    });

    if (activitiesResponse.error || !activitiesResponse.data)
      throw new Error("Failed to load activities");

    const data = [...(activitiesResponse.data as ActivityResponseDto[])];
    if (filter === "history") {
      data.sort(
        (a, b) =>
          new Date(b.dateTimeStart).getTime() -
          new Date(a.dateTimeStart).getTime(),
      );
    }

    setActivities(data);
  } catch (error) {
    console.error("Error while loading data:", error);
    toast.error(appendErrorMessage(t("loading_failed"), error));
  } finally {
    setLoading(false);
  }
};

/**
 * Generates a formatted text overview of the current/next week's activities
 * and copies it to the user's clipboard for social media distribution.
 *
 * Logic:
 * - If today is Monday-Wednesday, it targets the current week.
 * - If today is Thursday-Sunday, it targets the following week.
 * - Formats output based on the user's locale (NL vs EN).
 *
 * @async
 * @param {string} locale - The user's current language preference.
 * @param {ActivityResponseDto[]} activities - The full list of activities to filter from.
 */
export const copyWeekOverview = async (
  locale: string,
  activities: ActivityResponseDto[],
) => {
  try {
    const now = new Date();
    const currentDay = now.getDay();

    const targetDate = new Date(now);
    if (currentDay > 3 || currentDay === 0) {
      targetDate.setDate(now.getDate() + (8 - (currentDay || 7)));
    } else {
      targetDate.setDate(now.getDate() - (currentDay - 1));
    }

    const startOfWeek = new Date(targetDate.setHours(0, 0, 0, 0));
    const endOfWeek = new Date(startOfWeek);
    endOfWeek.setDate(startOfWeek.getDate() + 7);

    const weekActivities =
      activities.filter((a) => {
        const d = new Date(a.dateTimeStart);
        return d >= startOfWeek && d < endOfWeek;
      }) ?? [];

    const dutchDays = [
      "Maandag",
      "Dinsdag",
      "Woensdag",
      "Donderdag",
      "Vrijdag",
      "Zaterdag",
      "Zondag",
    ];
    const englishDays = [
      "Monday",
      "Tuesday",
      "Wednesday",
      "Thursday",
      "Friday",
      "Saturday",
      "Sunday",
    ];
    const isDutch = locale === "NL";
    const days = isDutch ? dutchDays : englishDays;
    let message = isDutch ? "*Weekoverzicht*\n\n" : "*Weekly Overview*\n\n";

    days.forEach((dayName, index) => {
      const dayDate = new Date(startOfWeek);
      dayDate.setDate(startOfWeek.getDate() + index);

      const dayActivities = weekActivities.filter(
        (a) =>
          new Date(a.dateTimeStart).toDateString() === dayDate.toDateString(),
      );

      message += `*${dayName}*\n`;
      if (dayActivities.length > 0) {
        message += `${dayActivities.map((a) => `${isDutch ? a.dutchDescription : a.englishDescription || a.name}\n\n${getEnv("HostUrl")}/activities/${a.id}`).join("\n\n&\n\n")}\n\n`;
      } else {
        message += isDutch ? "Geen activiteit :(\n\n" : "No activities :(\n\n";
      }
    });

    const weeklyDrink = weekActivities.find((a) => a.isWeeklyDrinks);
    message += isDutch ? "*Wekelijkse borrel*\n" : "*Weekly Drinks*\n";
    if (weeklyDrink) {
      const drinkDay =
        days[(new Date(weeklyDrink.dateTimeStart).getDay() + 6) % 7];
      const locationFallback = isDutch
        ? "Locatie onbekend"
        : "Unknown location";
      message += `${drinkDay}: ${weeklyDrink.location || locationFallback}\n`;
    } else {
      message += isDutch ? "Geen borrel deze week" : "No drinks this week";
    }

    await navigator.clipboard.writeText(message);
    toast.success(t("copied_to_clipboard"));
  } catch (error) {
    toast.error(appendErrorMessage(t("copy_failed"), error));
  }
};

export const downloadPosters = async (
  activities: ActivityResponseDto[],
  token: string,
) => {
  const posterUrls = activities
    .filter((a) => a.showInKoala && a.posterPath)
    .map((a) => `${getEnv("ApiUrl")}/activities/${a.id}/poster`);

  if (posterUrls.length === 0) {
    toast.error(appendErrorMessage(t("no_posters_found")));
    return;
  }

  const toastId = toast.loading(t("generating_pdf"));

  try {
    await generateA3Pdf(posterUrls, token);
    toast.success(t("pdf_downloaded"), { id: toastId });
  } catch (error) {
    console.error(error);
    toast.error(appendErrorMessage(t("pdf_failed"), error), { id: toastId });
  }
};

export const handleCreateActivityClick = (navigate: NavigateFunction) => {
  navigate("/activities/create");
};
