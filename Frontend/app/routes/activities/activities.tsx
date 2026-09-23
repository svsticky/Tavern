import { t } from "i18next";
import {
  CalendarClock,
  CalendarDaysIcon,
  DownloadIcon,
  PlusIcon,
} from "lucide-react";
import { useState } from "react";
import { useLoaderData, useNavigate } from "react-router";
import { type ActivityResponseDto, getActivities } from "~/api";
import ActivityTile from "~/components/Activity/ActivityTile/ActivityTile";
import { DateRowHeightGroup } from "~/components/Activity/ActivityTile/DateRowHeightGroup";
import PersonalCalendarTile from "~/components/Calendar/PersonalCalendarTile/PersonalCalendarTile";
import StickyLoadingLogo from "~/components/StickyLoadingLogo";
import { NoContentTile } from "~/components/Tiles/NoContentTile";
import Button from "~/components/UI/Button";
import Modal from "~/components/UI/Modal/Modal";
import { PageHeader } from "~/components/UI/PageHeader";
import { useAuth } from "~/context/AuthContext";
import { getCommitteeYear } from "~/util/date.util";
import { isBoardOrCandidateBoard } from "~/util/group.util";
import { requireTokenParsed } from "~/util/loaderAuth.util";
import {
  copyWeekOverview,
  downloadPosters,
  handleCreateActivityClick,
} from "./activities.handlers";

type LoaderData = {
  activities: ActivityResponseDto[];
  isBoard: boolean;
  isInGroup: boolean;
};

export async function clientLoader(): Promise<LoaderData> {
  const tokenParsed = await requireTokenParsed();
  const isBoard = isBoardOrCandidateBoard(tokenParsed);
  const isInGroup =
    isBoard ||
    (tokenParsed?.group_memberships ?? []).filter(
      (g) => g.split(":")[0] === getCommitteeYear().toString(),
    ).length > 0;

  const activitiesResponse = await getActivities({
    query: { IncludePast: false, IncludeFuture: true },
  });

  if (activitiesResponse.error || !activitiesResponse.data) {
    throw new Error("Failed to load activities");
  }

  return {
    activities: activitiesResponse.data as ActivityResponseDto[],
    isBoard,
    isInGroup,
  };
}

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
 *
 * @page
 * @component
 */
export default function ActivitiesPage() {
  const { activities, isBoard, isInGroup } =
    useLoaderData<typeof clientLoader>();
  const authService = useAuth();
  const navigate = useNavigate();
  const [calendarTileOpen, setCalendarTileOpen] = useState(false);

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

      {activities.length === 0 ? (
        <NoContentTile text={t("no_upcoming_activities")} />
      ) : (
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
      )}
    </>
  );
}
