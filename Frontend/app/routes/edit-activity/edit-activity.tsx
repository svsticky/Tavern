import { t } from "i18next";
import { useEffect, useState } from "react";
import { useLocation, useParams } from "react-router";
import type { ActivityResponseDto } from "~/api";
import EditActivityForm from "~/components/Activity/Edit/EditActivityForm/EditActivityForm";
import SendActivityMailComponent from "~/components/Activity/Edit/SendActivityMailComponent/SendActivityMailComponent";
import { PageHeader } from "~/components/UI/PageHeader/PageHeader";
import { useApp } from "~/context/AppContext";
import { useAuth } from "~/context/AuthContext";
import type { TokenParsed } from "~/types/TokenParsed";
import { isBoardOrCandidateBoard } from "~/util/group.util";
import { cn } from "~/util/tailwind.util";
import EditParticipantsTile from "../../components/Activity/Edit/EditParticipantsTile/EditParticipantsTile";
import {
  getEditActivityBackPath,
  loadEditActivityData,
} from "./edit-activity.handlers";

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

  const authService = useAuth();
  const { boardGroupId, candidateBoardGroupId } = useApp();
  const [tokenParsed, setTokenParsed] = useState<TokenParsed | null>(null);
  const [isBoard, setIsBoard] = useState(false);

  const [loading, setLoading] = useState<boolean>(true);

  const [activity, setActivity] = useState<ActivityResponseDto | null>(null);

  useEffect(() => {
    const loadToken = async () => {
      const tokenParsed = await authService.getTokenParsed();
      setTokenParsed(tokenParsed);
      if (!tokenParsed) {
        console.error("User not authenticated");
        return;
      }
      setIsBoard(
        isBoardOrCandidateBoard(
          tokenParsed,
          boardGroupId,
          candidateBoardGroupId,
        ),
      );
    };
    loadToken();
  }, [authService, boardGroupId, candidateBoardGroupId]);

  useEffect(() => {
    if (!tokenParsed) return;
    loadEditActivityData({
      isEdit,
      id,
      setActivity: (next) => setActivity(next),
      setLoading,
    });
  }, [id, isEdit, tokenParsed]);

  if (loading) return t("loading");

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
          <EditActivityForm activity={activity} id={id} isBoard={isBoard} />
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
