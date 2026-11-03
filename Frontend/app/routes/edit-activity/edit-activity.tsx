import { t } from "i18next";
import { useEffect, useState } from "react";
import { useLoaderData, useLocation, useParams } from "react-router";
import { getActivitiesById, getGroups, type ActivityResponseDto } from "~/api";
import EditActivityForm from "~/components/Activity/Edit/EditActivityForm/EditActivityForm";
import SendActivityMailComponent from "~/components/Activity/Edit/SendActivityMailComponent/SendActivityMailComponent";
import { PageHeader } from "~/components/UI/PageHeader";
import { useAuth } from "~/context/AuthContext";
import type { TokenParsed } from "~/types/TokenParsed";
import { isBoardOrCandidateBoard } from "~/util/group.util";
import { cn } from "~/util/tailwind.util";
import EditParticipantsTile from "../../components/Activity/Edit/EditParticipantsTile/EditParticipantsTile";
import {
  getEditActivityBackPath,
  loadEditActivityLoaderData,
} from "./edit-activity.handlers";
import { requireTokenParsed } from "~/util/loaderAuth.util";
import StickyLoadingLogo from "~/components/StickyLoadingLogo";

/**
 * Loads the groups and, in edit mode, the activity being edited (see `loadEditActivityLoaderData`).
 *
 * @param {Object} args - The loader arguments.
 * @param {{ id?: string }} args.params - The route parameters; `id` is set when editing an existing activity.
 */
export async function clientLoader({ params }: { params: { id?: string } }) {
  return loadEditActivityLoaderData(params);
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
 * A dynamic page for creating new activities or editing existing ones.
 *
 * This component acts as the primary orchestrator for activity management. It handles:
 * - **Context Detection**: Determines if the user is creating or editing based on the presence of an `id` param.
 * - **Permission Management**: Restricts administrative features (mailing, participant editing) to
 *   Board or Candidate Board members.
 * - **Dynamic Layout**: Switches from a single-column layout (Creation/Member view) to a
 *   split-column layout (Admin Edit view) to accommodate management tools.
 * - **Data Synchronization**: Hydrates the form with existing activity data and manages the loading state.
 *
 * Sub-components:
 * - `EditActivityForm`: Handles the primary metadata (name, date, description, etc.).
 * - `SendActivityMailComponent`: Allows admins to email all enrolled participants.
 * - `EditParticipantsTile`: Provides administrative tools for manual enrollment management.
 *
 * @page
 * @component
 */
export default function ActivityFormPage() {
  const { id } = useParams();
  const isEdit = !!id;
  const { pathname } = useLocation();
  const { tokenParsed, activity, groups } = useLoaderData<typeof clientLoader>();
  const isBoard = isBoardOrCandidateBoard(tokenParsed);
  
  const [activityState, setActivity] = useState<ActivityResponseDto | null>(activity);

  if (isEdit && !activity) return t("failed_fetching");

  return (
    <div className="">
      <PageHeader
        title={isEdit ? t("edit_activity") : t("create_activity")}
        backTo={getEditActivityBackPath(pathname, isEdit, id)}
      />

      <div
        className={cn(
          "grid grid-cols-1 gap-8",
          isBoard && isEdit && "lg:grid-cols-3",
        )}
      >
        <div className={cn("w-full", isEdit && isBoard && "lg:col-span-2")}>
          <EditActivityForm activity={activityState} id={id} isBoard={isBoard} groups={groups} />
        </div>

        {isBoard && isEdit && activityState && (
          <div className="flex flex-col gap-4">
            <SendActivityMailComponent activityId={activityState.id} />
            <EditParticipantsTile
              activity={activityState}
              setActivity={setActivity}
            />
          </div>
        )}
      </div>
    </div>
  );
}
