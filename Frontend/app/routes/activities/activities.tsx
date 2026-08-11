import { t } from "i18next";
import { CalendarDaysIcon, DownloadIcon, PlusIcon } from "lucide-react";
import { useEffect, useState } from "react";
import { useNavigate } from "react-router";
import type { ActivityResponseDto } from "~/api";
import ActivityTile from "~/components/Activity/ActivityTile/ActivityTile";
import { NoContentTile } from "~/components/Tiles/NoContentTile";
import Button from "~/components/UI/Button";
import { PageHeader } from "~/components/UI/PageHeader/PageHeader";
import { useAuth } from "~/context/AuthContext";
import type { TokenParsed } from "~/types/TokenParsed";
import { isBoardOrCandidateBoard } from "~/util/group.util";
import {
  copyWeekOverview,
  downloadPosters,
  handleCreateActivityClick,
  loadActivities,
} from "./activities.handlers";
import { getCommitteeYear } from "~/util/date.util";

/**
 * The main activities listing page for both members and administrators.
 *
 * This page serves as a hub for viewing upcoming events. It dynamically adjusts
 * its interface based on the user's permissions:
 * - **Members**: View a responsive grid of `ActivityTile` components.
 * - **Group Members**: Access a "Create Activity" button.
 * - **Board Members**: Access administrative tools such as generating poster PDFs
 *   and copying social media week overviews in multiple languages.
 *
 * Layout Features:
 * - **Responsive Grid**: Uses CSS Grid with `auto-fill` and `minmax` to create a
 *   fluid layout that adjusts based on screen width.
 * - **Conditional Actions**: Uses the `PageHeader`'s action prop to inject
 *   context-sensitive buttons.
 * - **Loading/Empty States**: Standardized handling for API wait times and
 *   scenarios with no upcoming events.
 *
 * @page
 * @component
 */
export default function ActivitiesPage() {
  const authService = useAuth();
  const [token, setToken] = useState<string | null>(null);
  const [tokenParsed, setTokenParsed] = useState<TokenParsed | null>(null);

  useEffect(() => {
    let cancelled = false;
    const loadToken = async () => {
      const tokenVal = await authService.getToken();
      const tokenParsedVal = await authService.getTokenParsed();
      if (!cancelled) {
        setToken(tokenVal);
        setTokenParsed(tokenParsedVal);
        if (!tokenParsedVal) {
          console.error("User not authenticated");
        }
      }
    };
    loadToken();
    return () => {
      cancelled = true;
    };
  }, [authService]);

  const isBoard = isBoardOrCandidateBoard(tokenParsed);

  const navigate = useNavigate();

  const [loading, setLoading] = useState(true);
  const [activities, setActivities] = useState<ActivityResponseDto[]>([]);
  useEffect(() => {
    if (!tokenParsed) return;
    loadActivities({
      setLoading,
      setActivities,
    });
  }, [tokenParsed]);

  if (!tokenParsed) return null;

  const isInGroup = 
      isBoardOrCandidateBoard(tokenParsed) || 
      (tokenParsed?.group_memberships ?? []).filter(g => g.split(':')[0] === getCommitteeYear().toString()).length > 0;

  return (
    <>
      <div className="flex flex-col lg:flex-row lg:items-center lg:items-start justify-between gap-3">
        <PageHeader
          title={t("activities")}
          action={
            <div className="flex items-center gap-2">
              {isInGroup && (
                <Button
                  variant="secondary"
                  onClick={() => handleCreateActivityClick(navigate)}
                  className="items-center px-3 py-1"
                >
                  <PlusIcon className="w-5 h-5" />
                </Button>
              )}
            </div>
          }
        />
        {isBoard && (
          <>
            <Button
              variant="secondary"
              onClick={() => downloadPosters(activities, token ?? "")}
              className="text-xs px-3 py-1"
              title="Download Koala Posters"
            >
              <DownloadIcon size={20} className="mr-1" />
              {t("download_posters")}
            </Button>
            <Button
              variant="secondary"
              onClick={() => copyWeekOverview("NL", activities)}
              className="text-xs px-3 py-1"
            >
              <CalendarDaysIcon size={20} className="mr-1" />
              {t("copy")} {t("weekoverview").toLowerCase()} NL
            </Button>
            <Button
              variant="secondary"
              onClick={() => copyWeekOverview("EN", activities)}
              className="text-xs px-3 py-1 mb-4"
            >
              <CalendarDaysIcon size={20} className="mr-1" />
              {t("copy")} {t("weekoverview").toLowerCase()} EN
            </Button>
          </>
        )}
      </div>
      {loading ? (
        t("loading")
      ) : activities.length === 0 ? (
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
      )}
    </>
  );
}
