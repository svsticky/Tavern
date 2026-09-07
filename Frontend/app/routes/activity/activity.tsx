import { t } from "i18next";
import {
  Archive,
  ArchiveRestore,
  Copy,
  PencilIcon,
  Trash2Icon,
} from "lucide-react";
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
import { hasEnrollmentOpened } from "~/util/activity.util";
import { downloadActivityEnrollmentsCsv } from "~/util/activityCsv.util";
import {
  canEditActivity,
  hasPermission,
  isBoardOrCandidateBoard,
  isInGroupWithId,
} from "~/util/group.util";
import { generateParticipantChecklistPdf } from "~/util/pdf.util";
import type { Route } from "./+types/activity";
import {
  getActivityBackPath,
  handleDeleteActivity,
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
  const [confirmModal, confirm] = useConfirm();
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

  const isBoard = isBoardOrCandidateBoard(tokenParsed);
  const isOrganizer =
    !!activity?.organizerId &&
    tokenParsed !== null &&
    (isInGroupWithId(tokenParsed, activity.organizerId) ||
      hasPermission(tokenParsed, "EditActivityForGroup", activity.organizerId));
  const canExport = isBoard || isOrganizer;

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
              <Button
                onClick={() =>
                  navigate(`/activities/create?cloneFrom=${activity.id}`)
                }
                variant="secondary"
                className="flex items-center px-2"
                aria-label={t("clone_activity")}
              >
                <Copy size={18} />
              </Button>
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
                  aria-label={t("edit")}
                >
                  <PencilIcon size={18} />
                </Button>
              )}
              {isBoard && (
                <Button
                  onClick={() =>
                    handleDeleteActivity(
                      activity.id,
                      navigate,
                      pathname,
                      confirm,
                    )
                  }
                  variant="danger"
                  className="flex items-center px-2"
                  aria-label={t("delete_activity")}
                >
                  <Trash2Icon size={18} />
                </Button>
              )}
            </div>
          )
        }
      />

      <div className="space-y-6 w-full">
        <ActivityDetailsTile activity={activity} setActivity={setActivity} />
        {hasEnrollmentOpened(activity) &&
          (activity.areParticipantsVisible || isBoard) && (
            <>
              <ActivityParticipantsTile
                enrollments={
                  activity.enrollments.filter((e) => !e.isOnWaitingList) ?? []
                }
                isAdmin={isBoard}
                onExportCsv={
                  canExport
                    ? () => {
                        downloadActivityEnrollmentsCsv(
                          activity,
                          (tokenParsed?.locale || "nl")
                            .toLowerCase()
                            .startsWith("nl"),
                        );
                        toast.success(t("csv_exported"));
                      }
                    : undefined
                }
                onExportPdf={
                  canExport
                    ? () => {
                        generateParticipantChecklistPdf(
                          activity,
                          (tokenParsed?.locale || "nl")
                            .toLowerCase()
                            .startsWith("nl"),
                        );
                        toast.success(t("pdf_exported"));
                      }
                    : undefined
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
                isAdmin={isBoard}
              />
            </>
          )}
      </div>
      {confirmModal}
    </div>
  );
}
