import { t } from "i18next";
import { PencilIcon } from "lucide-react";
import { useEffect, useState } from "react";
import { useLoaderData, useNavigate } from "react-router";
import type { ActivityResponseDto } from "~/api";
import ActivityDetailsTile from "~/components/Activity/ActivityDetailsTile/ActivityDetailsTile";
import ActivityParticipantsTile from "~/components/Activity/ActivityParticipantsTile/ActivityParticipantsTile";
import StickyLoadingLogo from "~/components/StickyLoadingLogo";
import Button from "~/components/UI/Button";
import { PageHeader } from "~/components/UI/PageHeader";
import { hasEnrollmentOpened } from "~/util/activity.util";
import { canEditActivity, isBoardOrCandidateBoard } from "~/util/group.util";
import { requireTokenParsed } from "~/util/loaderAuth.util";
import {
  fetchActivity,
  fetchOrganizerName,
  getActivityBackPath,
  handleEditActivityClick,
} from "./activity.handlers";

/**
 * Loads the activity named by the URL's `id` (plus its organizer's name and
 * the current user's token) before the route renders, so the page - and
 * React Router's scroll restoration when coming back to it - starts from a
 * complete page instead of a "loading" placeholder.
 */
export async function clientLoader({ params }: { params: { id?: string } }) {
  const tokenParsed = await requireTokenParsed();
  const activity = await fetchActivity(Number(params.id));
  const organizerName = await fetchOrganizerName(activity.organizerId);
  return { tokenParsed, activity, organizerName };
}

export function HydrateFallback() {
  return <StickyLoadingLogo />;
}

/**
 * Detailed view for a specific activity, including description and participant lists.
 *
 * This page serves as the single source of truth for an activity's information.
 * It manages:
 * - **Data Hydration**: Fetches activity details based on the URL `id` parameter.
 * - **Enrollment Management**: Passes state-updating functions to child tiles
 *   to allow immediate UI feedback after joining/leaving.
 * - **Participant Visibility**: Filters and displays the participant list and
 *   waiting list, respecting the `areParticipantsVisible` privacy flag.
 * - **Contextual Navigation**: Determines the "Back" path based on whether
 *   the user arrived via an admin route or the standard member list.
 * - **Permissions**: Shows an edit action only for authorized users (Board or Organizers).
 *
 * @page
 * @component
 * @param {Route.LoaderArgs} props - Route parameters provided by the framework, including the activity ID.
 */
export default function ActivityPage() {
  const loaderData = useLoaderData<typeof clientLoader>();
  const { tokenParsed, organizerName } = loaderData;
  const navigate = useNavigate();
  const { pathname } = window.location;

  // The tiles below patch the activity locally after an enrollment change
  // (see `setActivity`); a re-run loader hands back a fresh one to sync to.
  const [activity, setActivity] = useState<ActivityResponseDto>(
    loaderData.activity,
  );
  useEffect(() => {
    setActivity(loaderData.activity);
  }, [loaderData.activity]);

  const canEdit = canEditActivity(activity, tokenParsed);
  const isBoard = isBoardOrCandidateBoard(tokenParsed);

  return (
    <div className="flex flex-col w-full">
      <PageHeader
        title={activity.name}
        backTo={getActivityBackPath(pathname)}
        action={
          canEdit && (
            <Button
              onClick={() =>
                handleEditActivityClick(navigate, pathname, activity.id)
              }
              variant="secondary"
              className="flex items-center px-2"
            >
              <PencilIcon size={18} />
            </Button>
          )
        }
      />

      <div className="space-y-6 w-full">
        <ActivityDetailsTile
          activity={activity}
          setActivity={
            setActivity as React.Dispatch<
              React.SetStateAction<ActivityResponseDto | null>
            >
          }
          organizerName={organizerName}
        />
        {activity.areParticipantsVisible && (
          <>
            <ActivityParticipantsTile
              enrollments={
                activity.enrollments.filter((e) => !e.isOnWaitingList) ?? []
              }
              isBoard={isBoard}
              showCount={hasEnrollmentOpened(activity)}
            />
            <ActivityParticipantsTile
              title={t("waiting_list")}
              enrollments={(
                activity.enrollments.filter((e) => e.isOnWaitingList) ?? []
              )
                .slice()
                .sort(
                  (a, b) =>
                    new Date(a.registeredOn).getTime() -
                    new Date(b.registeredOn).getTime(),
                )}
              isBoard={isBoard}
              showCount={hasEnrollmentOpened(activity)}
            />
          </>
        )}
      </div>
    </div>
  );
}
