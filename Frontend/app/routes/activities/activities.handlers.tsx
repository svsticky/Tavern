import { t } from "i18next";
import toast from "react-hot-toast";
import type { NavigateFunction } from "react-router";
import { getApiActivities, type ActivityResponseDto } from "~/api";
import { generateA3Pdf } from "~/util/pdf.util";

type LoadActivitiesArgs = {
  initialized: boolean;
  authenticated: boolean | undefined;
  setLoading: (loading: boolean) => void;
  setActivities: (activities: ActivityResponseDto[]) => void;
};

export const loadActivities = async ({ initialized, authenticated, setLoading, setActivities }: LoadActivitiesArgs) => {
  if (!initialized || !authenticated) return;

  try {
    setLoading(true);
    const activitiesResponse = await getApiActivities({
      query: {
        IncludePast: false,
        IncludeFuture: true,
      }
    });

    if (activitiesResponse.data) {
      setActivities(activitiesResponse.data as ActivityResponseDto[]);
    }
  } catch (error) {
    console.error("Error while loading data:", error);
    toast.error(t("loading_failed"));
  } finally {
    setLoading(false);
  }
};

export const copyWeekOverview = async (locale: string, activities: ActivityResponseDto[]) => {
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

    const weekActivities = activities.filter((a) => {
      const d = new Date(a.dateTimeStart);
      return d >= startOfWeek && d < endOfWeek;
    }) ?? [];

    const dutchDays = ["Maandag", "Dinsdag", "Woensdag", "Donderdag", "Vrijdag", "Zaterdag", "Zondag"];
    const englishDays = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"];
    const isDutch = locale === "NL";
    const days = isDutch ? dutchDays : englishDays;
    let message = isDutch ? "*Weekoverzicht*\n\n" : "*Weekly Overview*\n\n";

    days.forEach((dayName, index) => {
      const dayDate = new Date(startOfWeek);
      dayDate.setDate(startOfWeek.getDate() + index);

      const dayActivities = weekActivities.filter((a) =>
        new Date(a.dateTimeStart).toDateString() === dayDate.toDateString()
      );

      message += `*${dayName}*\n`;
      if (dayActivities.length > 0) {
        message += dayActivities.map((a) => `${isDutch ? a.dutchDescription : a.englishDescription || a.name}\n\n${import.meta.env.HostUrl}/activities/${a.id}`).join("\n\n&\n\n") + "\n\n";
      } else {
        message += isDutch ? "Geen activiteit :(\n\n" : "No activities :(\n\n";
      }
    });

    const weeklyDrink = weekActivities.find((a) => a.isWeeklyDrinks);
    message += isDutch ? "*Wekelijkse borrel*\n" : "*Weekly Drinks*\n";
    if (weeklyDrink) {
      const drinkDay = days[(new Date(weeklyDrink.dateTimeStart).getDay() + 6) % 7];
      const locationFallback = isDutch ? "Locatie onbekend" : "Unknown location";
      message += `${drinkDay}: ${weeklyDrink.location || locationFallback}\n`;
    } else {
      message += isDutch ? "Geen borrel deze week" : "No drinks this week";
    }

    await navigator.clipboard.writeText(message);
    toast.success(t("copied_to_clipboard"));
  } catch {
    toast.error(t("copy_failed"));
  }
};

export const downloadPosters = async (activities: ActivityResponseDto[], token: string) => {
  const posterUrls = activities
    .filter((a) => a.showInKoala && a.posterPath)
    .map((a) => `${import.meta.env.ApiUrl}/api/activities/${a.id}/poster`);

  if (posterUrls.length === 0) {
    toast.error(t("no_posters_found"));
    return;
  }

  const toastId = toast.loading(t("generating_pdf"));

  try {
    await generateA3Pdf(posterUrls, token);
    toast.success(t("pdf_downloaded"), { id: toastId });
  } catch (error) {
    console.error(error);
    toast.error(t("pdf_failed"), { id: toastId });
  }
};

export const handleCreateActivityClick = (navigate: NavigateFunction) => {
  navigate("/activities/create");
};
