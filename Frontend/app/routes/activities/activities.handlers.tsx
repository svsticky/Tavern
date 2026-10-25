import { t } from "i18next";
import toast from "react-hot-toast";
import type { NavigateFunction } from "react-router";
import { type ActivityResponseDto, getActivities } from "~/api";
import { getEnv } from "~/util/config.utils";
import { isSameDayInAssociationTimeZone } from "~/util/date.util";
import { appendErrorMessage } from "~/util/error.util";
import { formatForWhatsApp } from "~/util/markdown.util";
import { generateA3Pdf } from "~/util/pdf.util";
import {
  formatPromoPrice,
  formatPromoTime,
  formatPromoTimeRange,
} from "~/util/promo.util";

/**
 * Arguments for the loadActivities handler.
 */
type LoadActivitiesArgs = {
  setLoading: (loading: boolean) => void;
  setActivities: (activities: ActivityResponseDto[]) => void;
};

/**
 * Fetches the list of current and future activities from the API.
 *
 * @async
 * @param {LoadActivitiesArgs} args - Configuration and state setter functions.
 */
export const loadActivities = async ({
  setLoading,
  setActivities,
}: LoadActivitiesArgs) => {
  try {
    setLoading(true);
    const activitiesResponse = await getActivities({
      query: {
        IncludePast: false,
        IncludeFuture: true,
      },
    });

    if (activitiesResponse.error || !activitiesResponse.data)
      throw new Error("Failed to load activities");

    setActivities(activitiesResponse.data as ActivityResponseDto[]);
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

    const regularActivities = weekActivities.filter((a) => !a.isWeeklyDrinks);
    const weeklyDrink = weekActivities.find((a) => a.isWeeklyDrinks);

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
    const promoLocale = isDutch ? "NL" : "EN";
    const days = isDutch ? dutchDays : englishDays;
    let message = isDutch ? "*Weekoverzicht*\n\n" : "*Week overview*\n\n";

    let hasAnyActivities = false;
    days.forEach((dayName, index) => {
      const dayDate = new Date(startOfWeek);
      dayDate.setDate(startOfWeek.getDate() + index);

      const dayActivities = regularActivities.filter(
        (a) =>
          new Date(a.dateTimeStart).toDateString() === dayDate.toDateString(),
      );

      if (dayActivities.length > 0) {
        hasAnyActivities = true;
        message += `_${dayName}_\n`;
        dayActivities.forEach((a) => {
          const startDate = new Date(a.dateTimeStart);
          const endDate = new Date(a.dateTimeEnd);
          const timeStr = isSameDayInAssociationTimeZone(startDate, endDate)
            ? `${formatPromoTime(startDate)} - ${formatPromoTime(endDate)}`
            : formatPromoTimeRange(startDate, endDate, promoLocale);
          const locStr = a.location || "TBA";
          const priceStr = formatPromoPrice(a.price, promoLocale);
          const summaryLine = `${a.name} | ${timeStr} | ${locStr} | ${priceStr}`;
          const rawDesc = isDutch
            ? a.shortDutchDescription || a.dutchDescription
            : a.shortEnglishDescription || a.englishDescription;
          const desc = formatForWhatsApp(rawDesc);
          const activityUrl = `${getEnv("HostUrl")}/activities/${a.id}`;
          message += `${summaryLine}\n${desc}\n${activityUrl}\n\n`;
        });
      }
    });

    if (!hasAnyActivities) {
      message += isDutch
        ? "Geen activiteiten deze week\n\n"
        : "No activities this week\n\n";
    }

    message += isDutch ? "_Wekelijkse borrel_\n" : "_Weekly drinks_\n";
    if (weeklyDrink) {
      const drinkDay =
        days[(new Date(weeklyDrink.dateTimeStart).getDay() + 6) % 7];
      const locationFallback = isDutch
        ? "Locatie onbekend"
        : "Unknown location";
      message += `${drinkDay}: ${weeklyDrink.location || locationFallback}\n`;
    } else {
      message += isDutch ? "Geen borrel deze week\n" : "No drinks this week\n";
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
