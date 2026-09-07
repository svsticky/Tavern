import { t } from "i18next";
import {
  CalendarClock,
  CalendarDaysIcon,
  DownloadIcon,
  MenuIcon,
  PlusIcon,
  Search,
} from "lucide-react";
import { useEffect, useRef, useState } from "react";
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
import {
  getGroupIdsWithPermission,
  hasPermission,
  isBoardOrCandidateBoard,
} from "~/util/group.util";
import { cn } from "~/util/tailwind.util";
import {
  type ActivityFilter,
  copyWeekOverview,
  downloadPosters,
  handleCreateActivityClick,
  loadActivities,
} from "./activities.handlers";

interface BoardDropdownProps {
  activities: ActivityResponseDto[];
  token: string;
}

export function BoardDropdown({ activities, token }: BoardDropdownProps) {
  const [isOpen, setIsOpen] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!isOpen) return;

    const handleOutside = (event: MouseEvent) => {
      if (
        dropdownRef.current &&
        !dropdownRef.current.contains(event.target as Node)
      ) {
        setIsOpen(false);
      }
    };

    document.addEventListener("mousedown", handleOutside);
    return () => {
      document.removeEventListener("mousedown", handleOutside);
    };
  }, [isOpen]);

  return (
    <div className="relative" ref={dropdownRef}>
      <Button
        onClick={() => setIsOpen((prev) => !prev)}
        variant="secondary"
        className="items-center px-3 py-1"
        aria-label="Board Actions"
        aria-expanded={isOpen}
      >
        <MenuIcon className="w-5 h-5" />
      </Button>

      {isOpen && (
        <ul className="flex flex-col gap-2 right-0 top-11 absolute w-max bg-white border border-gray-200 rounded-lg p-2 shadow-lg z-50 animate-in fade-in zoom-in-95 duration-150">
          <Button
            variant="secondary"
            onClick={() => {
              downloadPosters(activities, token);
              setIsOpen(false);
            }}
            className="text-xs px-3 py-1.5 justify-start text-left"
            title="Download Koala Posters"
          >
            <DownloadIcon size={18} className="mr-2" />
            {t("download_posters")}
          </Button>
          <Button
            variant="secondary"
            onClick={() => {
              copyWeekOverview("NL", activities);
              setIsOpen(false);
            }}
            className="text-xs px-3 py-1.5 justify-start text-left"
          >
            <CalendarDaysIcon size={18} className="mr-2" />
            {t("copy")} {t("weekoverview").toLowerCase()} NL
          </Button>
          <Button
            variant="secondary"
            onClick={() => {
              copyWeekOverview("EN", activities);
              setIsOpen(false);
            }}
            className="text-xs px-3 py-1.5 justify-start text-left"
          >
            <CalendarDaysIcon size={18} className="mr-2" />
            {t("copy")} {t("weekoverview").toLowerCase()} EN
          </Button>
        </ul>
      )}
    </div>
  );
}

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
  const [searchQuery, setSearchQuery] = useState("");

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

  const filteredActivities = activities.filter((activity) => {
    if (!searchQuery.trim()) return true;
    const q = searchQuery.toLowerCase().trim();
    const nameMatch = activity.name?.toLowerCase().includes(q);
    const locationMatch = activity.location?.toLowerCase().includes(q);
    return Boolean(nameMatch || locationMatch);
  });

  const canCreateActivity =
    isBoardOrCandidateBoard(tokenParsed) ||
    hasPermission(tokenParsed, "EditAllActivities") ||
    getGroupIdsWithPermission(tokenParsed, "EditActivityForGroup").length > 0;

  return (
    <>
      <div className="flex flex-col md:flex-row md:items-start md:gap-3 justify-between gap-0">
        <PageHeader
          title={t("activities")}
          action={
            <div className="flex items-center gap-2">
              <Button
                variant="secondary"
                onClick={() => setCalendarTileOpen(true)}
                className="text-xs px-3 py-1"
                title={t("personal_calendar")}
              >
                <CalendarClock size={20} className="mr-1" />
                <span className="hidden md:inline-block">
                  {t("personal_calendar")}
                </span>
              </Button>
              {canCreateActivity && (
                <Button
                  variant="secondary"
                  onClick={() => handleCreateActivityClick(navigate)}
                  className="items-center px-3 py-1"
                  aria-label="Create Activity"
                >
                  <PlusIcon className="w-5 h-5" />
                </Button>
              )}
              {isBoard && (
                <BoardDropdown activities={activities} token={token ?? ""} />
              )}
            </div>
          }
        />
      </div>

      <Modal
        isOpen={calendarTileOpen}
        onClose={() => setCalendarTileOpen(false)}
        title={t("personal_calendar")}
      >
        <PersonalCalendarTile />
      </Modal>

      {/* Activities Filter Tabs & Search Bar */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 mb-6">
        <div className="flex items-center gap-2 overflow-x-auto pb-1 sm:pb-0">
          <button
            type="button"
            onClick={() => setFilter("all")}
            className={cn(
              "px-3.5 py-1.5 text-xs font-semibold rounded-full border transition-all cursor-pointer whitespace-nowrap",
              filter === "all"
                ? "bg-(--board-primary) text-white border-(--board-primary) shadow-xs"
                : "bg-white text-gray-700 border-gray-300 hover:bg-gray-50",
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
                ? "bg-(--board-primary) text-white border-(--board-primary) shadow-xs"
                : "bg-white text-gray-700 border-gray-300 hover:bg-gray-50",
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
                ? "bg-(--board-primary) text-white border-(--board-primary) shadow-xs"
                : "bg-white text-gray-700 border-gray-300 hover:bg-gray-50",
            )}
          >
            {t("enrolled_history")}
          </button>
        </div>

        <div className="relative w-full sm:w-64">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400 pointer-events-none" />
          <input
            type="text"
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            placeholder={t("search_activities")}
            aria-label={t("search_activities")}
            className="w-full pl-9 pr-3 py-1.5 text-xs bg-white border border-gray-300 rounded-lg focus:outline-none focus:ring-1 focus:ring-(--board-primary) focus:border-(--board-primary) text-gray-800 placeholder-gray-400 shadow-2xs"
          />
        </div>
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
      ) : filteredActivities.length === 0 ? (
        <NoContentTile text={t("no_activities_found")} />
      ) : (
        <div className="grid gap-4 justify-center grid-cols-[repeat(auto-fill,minmax(250px,1fr))] w-full">
          {filteredActivities.map((activity) => (
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
