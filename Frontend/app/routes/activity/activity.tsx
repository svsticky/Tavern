import { t } from "i18next";
import { Archive, ArchiveRestore, PencilIcon } from "lucide-react";
import { useEffect, useState } from "react";
import toast from "react-hot-toast";
import { useNavigate } from "react-router";
import { type ActivityResponseDto, patchActivitiesById } from "~/api";
import ActivityDetailsTile from "~/components/Activity/ActivityDetailsTile/ActivityDetailsTile";
import ActivityParticipantsTile from "~/components/Activity/ActivityParticipantsTile/ActivityParticipantsTile";
import Button from "~/components/UI/Button";
import { useConfirm } from "~/components/UI/ConfirmModal/useConfirm";
import { PageHeader } from "~/components/UI/PageHeader";
import { useAuth } from "~/context/AuthContext";
import type { TokenParsed } from "~/types/TokenParsed";
import { canEditActivity, isBoardOrCandidateBoard } from "~/util/group.util";
import type { Route } from "./+types/activity";
import {
  getActivityBackPath,
  handleEditActivityClick,
  loadActivityData,
} from "./activity.handlers";

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
export default function ActivityPage({ params }: Route.LoaderArgs) {
  const authService = useAuth();
  const [tokenParsed, setTokenParsed] = useState<TokenParsed | null>(null);
  const [canEdit, setCanEdit] = useState(false);
  const navigate = useNavigate();
  const { pathname } = window.location;
  const [loading, setLoading] = useState(true);
  const [activity, setActivity] = useState<ActivityResponseDto | null>(null);

  useEffect(() => {
    const loadToken = async () => {
      const tokenParsed = await authService.getTokenParsed();
      setTokenParsed(tokenParsed);

      if (!tokenParsed) {
        console.error("User not authenticated");
        return;
      }
    };
    loadToken();
  }, [authService]);

  useEffect(() => {
    if (!tokenParsed) return;
    const activityId = Number(params.id);
    if (activity?.id === activityId) return;
    loadActivityData({
      activityId,
      setLoading,
      setActivity: (next) => setActivity(next),
    });
  }, [activity?.id, params.id, tokenParsed]);

  useEffect(() => {
    if (!tokenParsed || activity == null) {
      setCanEdit(false);
      return;
    }

    setCanEdit(canEditActivity(activity, tokenParsed));
  }, [activity, tokenParsed]);

  const [confirmModal, confirm] = useConfirm();
  const isBoard = isBoardOrCandidateBoard(tokenParsed);

  if (loading || !tokenParsed) return t("loading");

  if (activity == null) return t("failed_fetching");

  return (
    <div className="flex flex-col w-full">
      <PageHeader
        title={activity.name}
        backTo={getActivityBackPath(pathname)}
        action={
          activity &&
          (canEdit || isBoard) && (
            <div className="flex items-center gap-2">
              {isBoard && (
                <Button
                  onClick={async () => {
                    const confirmed = await confirm(
                      activity.isArchived
                        ? t("confirm_unarchive_activity")
                        : t("confirm_archive_activity"),
                      {
                        title: activity.isArchived
                          ? t("unarchive_activity")
                          : t("archive_activity"),
                        variant: "secondary",
                      },
                    );
                    if (!confirmed) return;

                    const nextArchived = !activity.isArchived;
                    const res = await patchActivitiesById({
                      path: { id: activity.id },
                      body: [
                        {
                          op: "replace",
                          path: "/isarchived",
                          value: nextArchived,
                        },
                      ],
                    });
                    if (res.error) {
                      toast.error(t("failed_updating"));
                      return;
                    }
                    setActivity((prev) =>
                      prev ? { ...prev, isArchived: nextArchived } : prev,
                    );
                    toast.success(
                      nextArchived
                        ? t("activity_archived")
                        : t("activity_unarchived"),
                    );
                  }}
                  variant="secondary"
                  className="flex items-center px-2"
                  aria-label={
                    activity.isArchived
                      ? t("unarchive_activity")
                      : t("archive_activity")
                  }
                  title={
                    activity.isArchived
                      ? t("unarchive_activity")
                      : t("archive_activity")
                  }
                >
                  {activity.isArchived ? (
                    <ArchiveRestore size={18} />
                  ) : (
                    <Archive size={18} />
                  )}
                </Button>
              )}
              {canEdit && (
                <Button
                  onClick={() =>
                    handleEditActivityClick(navigate, pathname, activity.id)
                  }
                  variant="secondary"
                  className="flex items-center px-2"
                >
                  <PencilIcon size={18} />
                </Button>
              )}
            </div>
          )
        }
      />

      <div className="space-y-6 w-full">
        <ActivityDetailsTile activity={activity} setActivity={setActivity} />
        {activity.areParticipantsVisible && (
          <>
            <ActivityParticipantsTile
              enrollments={
                !activity.areParticipantsVisible
                  ? []
                  : (activity.enrollments.filter((e) => !e.isOnWaitingList) ??
                    [])
              }
            />
            <ActivityParticipantsTile
              title={t("waiting_list")}
              enrollments={
                !activity.areParticipantsVisible
                  ? []
                  : (
                      activity.enrollments.filter((e) => e.isOnWaitingList) ??
                      []
                    )
                      .slice()
                      .sort(
                        (a, b) =>
                          new Date(a.registeredOn).getTime() -
                          new Date(b.registeredOn).getTime(),
                      )
              }
            />
          </>
        )}
      </div>
      {confirmModal}
    </div>
  );
}
