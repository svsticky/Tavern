import { useKeycloak } from "@react-keycloak/web";
import { t } from "i18next";
import { CalendarDaysIcon, DownloadIcon, PlusIcon } from "lucide-react";
import { useEffect, useState } from "react";
import toast from "react-hot-toast";
import { useNavigate } from "react-router";
import { getApiActivities, type ActivityResponseDto } from "~/api";
import ActivityTile from "~/components/Activity/ActivityTile";
import { NoContentTile } from "~/components/Tiles/NoContentTile";
import Button from "~/components/UI/Button";
import { PageHeader } from "~/components/UI/PageHeader";
import { isBoardOrCandidateBoard } from "~/util/group.util";
import { generateA3Pdf } from "~/util/pdf.util";

export default function ActivitiesPage() {
  const { keycloak, initialized } = useKeycloak();

  const navigate = useNavigate();

  const [loading, setLoading] = useState(true);
  const [activities, setActivities] = useState<ActivityResponseDto[]>([]);
  useEffect(() => {
      async function loadData() {
        if (!initialized || !keycloak.authenticated) return;
  
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
      }
  
      loadData();
    }, [initialized, keycloak.authenticated]);

  const isInGroup = (keycloak.tokenParsed?.group_memberships ?? []).length > 0;
  const isBoard = isBoardOrCandidateBoard(keycloak.tokenParsed);

  const copyWeekOverview = async (locale: string) => {
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

      const weekActivities = activities.filter(a => {
        const d = new Date(a.dateTimeStart);
        return d >= startOfWeek && d < endOfWeek;
      }) ?? [];

      const dutchDays = ["Maandag", "Dinsdag", "Woensdag", "Donderdag", "Vrijdag", "Zaterdag", "Zondag"];
      const englishDays = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"];
      const isDutch = locale === "NL"
      const days = isDutch ? dutchDays : englishDays;
      let message = isDutch ? "*Weekoverzicht*\n\n" : "*Weekly Overview*\n\n";

      days.forEach((dayName, index) => {
        const dayDate = new Date(startOfWeek);
        dayDate.setDate(startOfWeek.getDate() + index);
        
        const dayActivities = weekActivities.filter(a => 
          new Date(a.dateTimeStart).toDateString() === dayDate.toDateString()
        );

        message += `*${dayName}*\n`;
        if (dayActivities.length > 0) {
          message += dayActivities.map(a => `${isDutch ? a.dutchDescription : a.englishDescription || a.name}\n\n${import.meta.env.HostUrl}/activities/${a.id}`).join("\n\n&\n\n") + "\n\n";
        } else {
          message += isDutch ? "Geen activiteit :(\n\n" : "No activities :(\n\n";
        }
      });

      const weeklyDrink = weekActivities.find(a => a.isWeeklyDrinks);
      message += isDutch ? `*Wekelijkse borrel*\n` : `*Weekly Drinks*\n`;
      if (weeklyDrink) {
        const drinkDay = days[(new Date(weeklyDrink.dateTimeStart).getDay() + 6) % 7];
        const locationFallback = isDutch ? 'Locatie onbekend' : 'Unknown location';
        message += `${drinkDay}: ${weeklyDrink.location || locationFallback}\n`;
      } else {
        message += isDutch ? "Geen borrel deze week" : "No drinks this week";
      }

      await navigator.clipboard.writeText(message);
      toast.success(t("copied_to_clipboard"));
    } catch (err) {
      toast.error(t("copy_failed"));
    }
  };

  const downloadPosters = async () => {
    const posterUrls = activities
      .filter(a => a.showInKoala && a.posterPath) 
      .map(a => `${import.meta.env.ApiUrl}/api/activities/${a.id}/poster`);

    if (posterUrls.length === 0) {
      toast.error(t("no_posters_found"));
      return;
    }

    const toastId = toast.loading(t("generating_pdf..."));

    try {
      await generateA3Pdf(posterUrls, keycloak.token ?? "");

      toast.success(t("pdf_downloaded"), { id: toastId });
    } catch (error) {
      console.error(error);
      toast.error(t("pdf_failed"), { id: toastId });
    }
  };

  return (
    <>
      <div className="flex justify-between items-center">
        <PageHeader title={t("activities")} 
          action={(
            <div className="flex items-center gap-2">
          {isBoard && (
            <>
              <Button
                variant="secondary"
                onClick={downloadPosters}
                className="text-xs px-3 py-1"
                title="Download Koala Posters"
              >
                <DownloadIcon size={18} className="mr-1" />
                {t("download_posters")}
              </Button>
              <Button
                variant="secondary"
                onClick={() => copyWeekOverview("NL")}
                className="text-xs px-3 py-1"
              >
                <CalendarDaysIcon size={18} className="mr-1" />
                {t("copy")} NL
              </Button>
              <Button
                variant="secondary"
                onClick={() => copyWeekOverview("EN")}
                className="text-xs px-3 py-1"
              >
                <CalendarDaysIcon size={18} className="mr-1" />
                {t("copy")} EN
              </Button>
            </>
          )}
          {isInGroup && (
          <Button 
            variant="secondary"
            onClick={() => (navigate("/activities/create"))}
            className="items-center px-3 py-1"
          >
            <PlusIcon className="w-5 h-5" />
          </Button>
        )}</div>)}
        />
      </div>
      {loading ? (
        'Loading...'
      ) : (
      activities.length === 0 ? (
          <NoContentTile text={t("no_upcoming_activities")} />
        ) : (
          <div className="grid gap-4 justify-center grid-cols-[repeat(auto-fill,minmax(250px,1fr))] w-full">
            {activities.map((activity) => (
              <ActivityTile
                key={activity.id}
                className="w-auto"
                activity={activity}
              />
            ))}
          </div>
        ))}
    </>
  );
}
