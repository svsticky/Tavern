import { t } from "i18next";
import {
  CalendarClock,
  CalendarDaysIcon,
  DownloadIcon,
  PlusIcon,
} from "lucide-react";
import { useEffect, useState } from "react";
import { useNavigate } from "react-router";
import type { ActivityResponseDto } from "~/api";
import ActivityTile from "~/components/Activity/ActivityTile/ActivityTile";
import PersonalCalendarTile from "~/components/Calendar/PersonalCalendarTile/PersonalCalendarTile";
import { NoContentTile } from "~/components/Tiles/NoContentTile";
import Button from "~/components/UI/Button";
import Modal from "~/components/UI/Modal/Modal";
import { PageHeader } from "~/components/UI/PageHeader";
import { useAuth } from "~/context/AuthContext";
import type { TokenParsed } from "~/types/TokenParsed";
import { getCommitteeYear } from "~/util/date.util";
import { isBoardOrCandidateBoard } from "~/util/group.util";
import { cn } from "~/util/tailwind.util";
import {
  type ActivityFilter,
  copyWeekOverview,
  downloadPosters,
  handleCreateActivityClick,
  loadActivities,
} from "./activities.handlers";

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
  const [calendarTileOpen, setCalendarTileOpen] = useState(false);
  const [filter, setFilter] = useState<ActivityFilter>("all");

  useEffect(() => {
    if (!tokenParsed) return;
    loadActivities({
      setLoading,
      setActivities,
      filter,
      userId: tokenParsed.UserId,
    });
  }, [tokenParsed, filter]);

  if (!tokenParsed) return null;

  const isInGroup =
    isBoard ||
    (tokenParsed?.group_memberships ?? []).filter(
      (g) => g.split(":")[0] === getCommitteeYear().toString(),
    ).length > 0;

  return (
    <>
      <div
        className={`flex flex-col ${isBoard ? " 2xl:flex-row 2xl:items-start 2xl:gap-3" : "md:flex-row md:items-start md:gap-3"} justify-between gap-0 `}
      >
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
        <div
          className={`flex flex-col ${isBoard ? " 2xl:flex-row 2xl:items-start" : "md:flex-row md:items-start"} justify-between gap-3`}
        >
          <Button
            variant="secondary"
            onClick={() => setCalendarTileOpen(true)}
            className={`text-xs px-3 py-1 ${!isBoard ? "mb-4" : ""}`}
            title={t("personal_calendar")}
          >
            <CalendarClock size={20} className="mr-1" />
            {t("personal_calendar")}
          </Button>
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
      </div>

      <Modal
        isOpen={calendarTileOpen}
        onClose={() => setCalendarTileOpen(false)}
        title={t("personal_calendar")}
      >
        <PersonalCalendarTile />
      </Modal>

      {/* Activities Filter Tabs */}
      <div className="flex items-center gap-2 mb-6 overflow-x-auto pb-1">
        <button
          type="button"
          onClick={() => setFilter("all")}
          className={cn(
            "px-3.5 py-1.5 text-xs font-semibold rounded-full border transition-all cursor-pointer whitespace-nowrap",
            filter === "all"
              ? "bg-slate-900 text-white border-slate-900 shadow-sm"
              : "bg-white text-slate-700 border-slate-300 hover:bg-slate-50",
          )}
        >
          {t("all_activities")}
        </button>
        <button
          type="button"
          onClick={() => setFilter("enrolled")}
          className={cn(
            "px-3.5 py-1.5 text-xs font-semibold rounded-full border transition-all cursor-pointer whitespace-nowrap",
            filter === "enrolled"
              ? "bg-slate-900 text-white border-slate-900 shadow-sm"
              : "bg-white text-slate-700 border-slate-300 hover:bg-slate-50",
          )}
        >
          {t("my_enrollments")}
        </button>
        <button
          type="button"
          onClick={() => setFilter("history")}
          className={cn(
            "px-3.5 py-1.5 text-xs font-semibold rounded-full border transition-all cursor-pointer whitespace-nowrap",
            filter === "history"
              ? "bg-slate-900 text-white border-slate-900 shadow-sm"
              : "bg-white text-slate-700 border-slate-300 hover:bg-slate-50",
          )}
        >
          {t("enrolled_history")}
        </button>
      </div>

      {loading ? (
        t("loading")
      ) : activities.length === 0 ? (
        <NoContentTile
          text={
            filter === "history"
              ? t("no_historical_activities")
              : filter === "enrolled"
                ? t("no_enrollments")
                : t("no_upcoming_activities")
          }
        />
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
