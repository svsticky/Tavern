import { t } from "i18next";
import { PencilIcon } from "lucide-react";
import { useEffect, useState } from "react";
import { useLoaderData, useNavigate } from "react-router";
import type { ActivityResponseDto } from "~/api";
import ActivityDetailsTile from "~/components/Activity/ActivityDetailsTile/ActivityDetailsTile";
import ActivityParticipantsTile from "~/components/Activity/ActivityParticipantsTile/ActivityParticipantsTile";
import Button from "~/components/UI/Button";
import { PageHeader } from "~/components/UI/PageHeader";
import { useAuth } from "~/context/AuthContext";
import type { TokenParsed } from "~/types/TokenParsed";
import { hasEnrollmentOpened } from "~/util/activity.util";
import { canEditActivity, isBoardOrCandidateBoard } from "~/util/group.util";
import type { Route } from "./+types/activity";
import {
  getActivityBackPath,
  handleEditActivityClick,
  loadActivityLoaderData,
} from "./activity.handlers";
import StickyLoadingLogo from "~/components/StickyLoadingLogo";

/**
 * Loads the activity from the `id` route parameter (see `loadActivityLoaderData`).
 *
 * @param {Object} args - The loader arguments.
 * @param {{ id?: string }} args.params - The route parameters, containing the activity ID.
 */
export async function clientLoader({ params }: { params: { id?: string } }) {
  return loadActivityLoaderData(params);
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
  const navigate = useNavigate();
  const { pathname } = window.location;
  const { tokenParsed, activity, organizerName } = useLoaderData<typeof clientLoader>();

  const [activityState, setActivityState] = useState<ActivityResponseDto>(activity);

  const canEdit = canEditActivity(activityState, tokenParsed);

  const isBoard = isBoardOrCandidateBoard(tokenParsed);

  return (
    <div className="flex flex-col w-full">
      <PageHeader
        title={activityState.name}
        backTo={getActivityBackPath(pathname)}
        action={
          canEdit && (
            <Button
              onClick={() =>
                handleEditActivityClick(navigate, pathname, activityState.id)
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
          activity={activityState}
          tokenParsed={tokenParsed}
          organizerName={organizerName}
          setActivity={setActivityState}
        />
        {activityState.areParticipantsVisible && (
          <>
            <ActivityParticipantsTile
              enrollments={
                activityState.enrollments.filter((e) => !e.isOnWaitingList) ?? []
              }
              isBoard={isBoard}
              showCount={hasEnrollmentOpened(activityState)}
            />
            <ActivityParticipantsTile
              title={t("waiting_list")}
              enrollments={(
                activityState.enrollments.filter((e) => e.isOnWaitingList) ?? []
              )
                .slice()
                .sort(
                  (a, b) =>
                    new Date(a.registeredOn).getTime() -
                    new Date(b.registeredOn).getTime(),
                )}
              isBoard={isBoard}
              showCount={hasEnrollmentOpened(activityState)}
            />
          </>
        )}
      </div>
    </div>
  );
}
