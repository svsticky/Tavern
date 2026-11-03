import { t } from "i18next";
import {
  CalendarClock,
  CalendarDaysIcon,
  DownloadIcon,
  MenuIcon,
  PlusIcon,
} from "lucide-react";
import { useEffect, useRef, useState } from "react";
import { data, useLoaderData, useNavigate } from "react-router";
import type { ActivityResponseDto } from "~/api";
import ActivityTile from "~/components/Activity/ActivityTile/ActivityTile";
import { DateRowHeightGroup } from "~/components/Activity/ActivityTile/DateRowHeightGroup";
import PersonalCalendarTile from "~/components/Calendar/PersonalCalendarTile/PersonalCalendarTile";
import Button from "~/components/UI/Button";
import Modal from "~/components/UI/Modal/Modal";
import { PageHeader } from "~/components/UI/PageHeader";
import { getCommitteeYear } from "~/util/date.util";
import { isBoardOrCandidateBoard } from "~/util/group.util";
import {
  copyWeekOverview,
  downloadPosters,
  handleCreateActivityClick,
} from "./activities.handlers";
import { requireTokenParsed } from "~/util/loaderAuth.util";
import { useAuth } from "~/context/AuthContext";
import StickyLoadingLogo from "~/components/StickyLoadingLogo";
import { getActivities } from "~/api";

/**
 * Loads the user's token and all activities.
 * Redirects to the login page when the user isn't logged in.
 *
 * @throws {Error} When the activities fail to load, which shows the route's error page.
 */
export async function clientLoader(){
  const tokenParsed = await requireTokenParsed();
  const activitiesResponse = await getActivities();
  if (activitiesResponse.error || !activitiesResponse.data) throw new Error("Failed to load activities");
  return { tokenParsed, activities: activitiesResponse.data };
}

/**
 * Shown while the route's `clientLoader` runs on the first page load.
 *
 * @component
 */
export function HydrateFallback() {
  return <StickyLoadingLogo />;
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
  const { tokenParsed, activities } = useLoaderData<typeof clientLoader>();

  const isBoard = isBoardOrCandidateBoard(tokenParsed);

  const navigate = useNavigate();

  const [calendarTileOpen, setCalendarTileOpen] = useState(false);

  const isInGroup =
    isBoard ||
    (tokenParsed.group_memberships ?? []).filter(
      (g) => g.split(":")[0] === getCommitteeYear().toString(),
    ).length > 0;

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
                className="text-xs px-2 sm:px-3 py-1"
                title={t("personal_calendar")}
              >
                <CalendarClock size={20} />
                <span className="hidden md:inline-block">
                  {t("personal_calendar")}
                </span>
              </Button>
              {isInGroup && (
                <Button
                  variant="secondary"
                  onClick={() => handleCreateActivityClick(navigate)}
                  className="items-center px-2 sm:px-3 py-1"
                >
                  <PlusIcon className="w-5 h-5" />
                </Button>
              )}
              {isBoard && (
                <BoardDropdown activities={activities} />
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

      <div className="grid gap-4 justify-center grid-cols-[repeat(auto-fill,minmax(250px,1fr))] w-full">
        <DateRowHeightGroup>
          {activities.map((activity) => (
            <ActivityTile
              key={activity.id}
              className="w-auto"
              activity={activity}
            />
          ))}
        </DateRowHeightGroup>
      </div>
    </>
  );
}

interface BoardDropdownProps {
  activities: ActivityResponseDto[];
}

function BoardDropdown({ activities }: BoardDropdownProps) {
  const authService = useAuth();
  const [isOpen, setOpen] = useState(false);
  const dropdownRef = useRef<HTMLDivElement | null>(null);

  // Close on click outside the dropdown or on Escape
  useEffect(() => {
    if (!isOpen) return;

    const onClickOutside = (event: MouseEvent) => {
      if (!dropdownRef.current?.contains(event.target as Node)) {
        setOpen(false);
      }
    };
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === "Escape") setOpen(false);
    };

    document.addEventListener("mousedown", onClickOutside);
    document.addEventListener("keydown", onKeyDown);
    return () => {
      document.removeEventListener("mousedown", onClickOutside);
      document.removeEventListener("keydown", onKeyDown);
    };
  }, [isOpen]);

  return (
    <div ref={dropdownRef} className="relative">
      <Button
        onClick={() => setOpen(!isOpen)}
        variant="secondary"
        className="items-center px-2 sm:px-3 py-1"
        aria-expanded={isOpen}
      >
        <MenuIcon className="w-5 h-5" />
      </Button>

      <div
        className={`${isOpen ? "flex" : "hidden"} flex-col gap-4 right-0 top-9 absolute w-max bg-white border border-gray-200 rounded-lg p-4 z-100`}
      >
        <Button
          variant="secondary"
          onClick={async () =>
            downloadPosters(activities, (await authService.getToken()) ?? "")
          }
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
          className="text-xs px-3 py-1"
        >
          <CalendarDaysIcon size={20} className="mr-1" />
          {t("copy")} {t("weekoverview").toLowerCase()} EN
        </Button>
      </div>
    </div>
  );
}
