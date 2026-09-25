import { t } from "i18next";
import { useEffect, useState } from "react";
import { useLoaderData, useLocation, useParams } from "react-router";
import type { ActivityResponseDto } from "~/api";
import EditActivityForm from "~/components/Activity/Edit/EditActivityForm/EditActivityForm";
import { fetchGroups } from "~/components/Activity/Edit/EditActivityForm/EditActivityForm.handlers";
import SendActivityMailComponent from "~/components/Activity/Edit/SendActivityMailComponent/SendActivityMailComponent";
import StickyLoadingLogo from "~/components/StickyLoadingLogo";
import { PageHeader } from "~/components/UI/PageHeader";
import { isBoardOrCandidateBoard } from "~/util/group.util";
import { requireTokenParsed } from "~/util/loaderAuth.util";
import { cn } from "~/util/tailwind.util";
import EditParticipantsTile from "../../components/Activity/Edit/EditParticipantsTile/EditParticipantsTile";
import {
  fetchEditActivity,
  getEditActivityBackPath,
} from "./edit-activity.handlers";

export async function clientLoader({ params }: { params: { id?: string } }) {
  const tokenParsed = await requireTokenParsed();
  const [activity, groups] = await Promise.all([
    params.id ? fetchEditActivity(params.id) : Promise.resolve(null),
    fetchGroups(),
  ]);
  return { tokenParsed, activity, groups };
}

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
  const loaderData = useLoaderData<typeof clientLoader>();
  const { tokenParsed, groups } = loaderData;

  // EditParticipantsTile patches the activity locally; a re-run loader hands back a fresh one.
  const [activity, setActivity] = useState<ActivityResponseDto | null>(
    loaderData.activity,
  );
  useEffect(() => {
    setActivity(loaderData.activity);
  }, [loaderData.activity]);

  const isBoard = isBoardOrCandidateBoard(tokenParsed);

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
          <EditActivityForm
            activity={activity}
            id={id}
            isBoard={isBoard}
            groups={groups}
          />
        </div>

        {isBoard && isEdit && activity && (
          <div className="flex flex-col gap-4">
            <SendActivityMailComponent activityId={activity.id} />
            <EditParticipantsTile
              activity={activity}
              setActivity={setActivity}
            />
          </div>
        )}
      </div>
    </div>
  );
}
