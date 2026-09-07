import {
  Archive,
  ArchiveRestore,
  Calendar,
  Clock,
  Copy,
  Download,
  Image as ImageIcon,
  MapPin,
  Users,
} from "lucide-react";
import { useEffect, useState } from "react";
import toast from "react-hot-toast";
import { useTranslation } from "react-i18next";
import Markdown from "react-markdown";
import { useNavigate } from "react-router";
import {
  type ActivityResponseDto,
  getGroupsById,
  patchActivitiesById,
  type SpecificationAnswerResponseDto,
} from "~/api";
import { useApp } from "~/context/AppContext";
import { useAuth } from "~/context/AuthContext";
import type { TokenParsed } from "~/types/TokenParsed";
import {
  getActivityEnrollmentStatus,
  hasEnrollmentOpened,
} from "~/util/activity.util";
import { hasAllMandatoryAnswers } from "~/util/answer.util";
import { getEnv } from "~/util/config.utils";
import { formatDate } from "~/util/date.util";
import { hasPermission, isBoardOrCandidateBoard } from "~/util/group.util";
import { capitalizeFirst } from "~/util/string.util";
import { isMemberInTargetAudience } from "~/util/targetaudience.util";
import BorderedTile from "../../Tiles/BorderedTile";
import Button from "../../UI/Button";
import { useConfirm } from "../../UI/ConfirmModal/useConfirm";
import AnswerQuestionsTile from "../AnswerQuestionsTile";
import {
  handleAddToCalendar,
  handleCopyForWhatsapp,
  handleDownloadIcs,
  handleEnrollment,
  handleUnenrollment,
  handleUpdateEnrollment,
} from "./ActivityDetailsTile.handlers";
import InfoItem from "./InfoItem";

/**
 * Renders the organizing group's logo, falling back to the same placeholder used
 * for a member's own group memberships on the home page if the group has no logo
 * (or it fails to load).
 */
function OrganizerIcon({ groupId }: { groupId: number }) {
  const [imageUrl, setImageUrl] = useState(
    `${getEnv("ApiUrl")}/groups/${groupId}/group-picture`,
  );

  return (
    <img
      src={imageUrl}
      onError={() => setImageUrl("/profile-picture.svg")}
      alt=""
      className="w-8 h-8 object-contain"
    />
  );
}

const toAnswerMap = (answers?: SpecificationAnswerResponseDto[] | null) => {
  const mapped: Record<number, string> = {};
  answers?.forEach((answer) => {
    mapped[answer.questionId] = answer.answer;
  });
  return mapped;
};

/**
 * A detailed tile component for displaying activity information, including posters,
 * descriptions, and enrollment actions.
 *
 * This component handles:
 * - Dynamic poster loading with state management (loading/error/success).
 * - Enrollment logic (signing in, waiting list, unenrolling, updating answers).
 * - Clipboard integration for WhatsApp sharing (restricted to Board/Candidate Board).
 * - External calendar integration.
 *
 * @component
 * @param {Object} props - The component props.
 * @param {ActivityResponseDto} props.activity - The activity data to display.
 * @param {React.Dispatch<React.SetStateAction<ActivityResponseDto | null>>} [props.setActivity] - Optional state setter to update activity data (e.g., after enrollment changes) in the parent component.
 *
 * @example
 * ```tsx
 * <ActivityDetailsTile
 *   activity={activityData}
 *   setActivity={setActivityData}
 * />
 * ```
 */
export default function ActivityDetailsTile({
  activity,
  setActivity,
}: {
  activity: ActivityResponseDto;
  setActivity?: React.Dispatch<
    React.SetStateAction<ActivityResponseDto | null>
  >;
}) {
  const { t, i18n } = useTranslation();
  const navigate = useNavigate();
  const [confirmModal, confirm] = useConfirm();
  const authService = useAuth();
  const { member } = useApp();
  const [tokenParsed, setTokenParsed] = useState<TokenParsed | null>(null);

  useEffect(() => {
    const loadToken = async () => {
      const parsedToken = await authService.getTokenParsed();
      if (!parsedToken) {
        console.error("User not authenticated");
        return;
      }
      setTokenParsed(parsedToken);
    };
    loadToken();
  }, [authService]);

  const [submitting, setSubmitting] = useState(false);
  const [posterStatus, setPosterStatus] = useState<
    "loading" | "loaded" | "error"
  >("loading");
  const [organizerName, setOrganizerName] = useState<string | null>(null);

  useEffect(() => {
    if (!activity.organizerId) {
      setOrganizerName(null);
      return;
    }

    let cancelled = false;
    const loadOrganizer = async () => {
      const response = await getGroupsById({
        path: { id: activity.organizerId! },
      });
      if (!cancelled) {
        setOrganizerName(response.data?.name ?? null);
      }
    };
    loadOrganizer();
    return () => {
      cancelled = true;
    };
  }, [activity.organizerId]);

  const posterUrl = `${getEnv("ApiUrl")}/activities/${activity.id}/poster`;
  const hasPoster = !!activity.posterFileName;

  const startDate = new Date(activity.dateTimeStart);
  const endDate = new Date(activity.dateTimeEnd);

  const startDateString = capitalizeFirst(formatDate(startDate, "weekdayDate"));

  const startTimeString = formatDate(startDate, "timeOnly");

  const endDateString = capitalizeFirst(formatDate(endDate, "weekdayDate"));

  const endTimeString = formatDate(endDate, "timeOnly");

  const inTargetAudience = isMemberInTargetAudience(
    member,
    activity.allowedAudience,
  );
  const currentEnrollment = tokenParsed
    ? activity.enrollments.find((e) => e.member?.id === tokenParsed.UserId)
    : undefined;
  const isEnrolled = !!currentEnrollment;
  const [answers, setAnswers] = useState<Record<number, string>>({});
  const isBoard = isBoardOrCandidateBoard(tokenParsed);

  const waitingListEnrollments = (activity.enrollments || []).filter(
    (e) => e.isOnWaitingList,
  );
  const waitingIndex = tokenParsed
    ? waitingListEnrollments.findIndex(
        (e) => e.member?.id === tokenParsed.UserId,
      )
    : -1;
  const waitingPosition = waitingIndex !== -1 ? waitingIndex + 1 : null;
  const aheadCount = waitingIndex !== -1 ? waitingIndex : null;

  const canClone =
    isBoard ||
    (tokenParsed
      ? hasPermission(tokenParsed, "EditAllActivities") ||
        (activity.organizerId
          ? hasPermission(
              tokenParsed,
              "EditActivityForGroup",
              activity.organizerId,
            )
          : false)
      : false);

  useEffect(() => {
    setAnswers(toAnswerMap(currentEnrollment?.specificationAnswers));
  }, [currentEnrollment?.specificationAnswers]);

  const { canEnroll, canUnenroll } = getActivityEnrollmentStatus(activity);

  const submitAnswers = (action: typeof handleEnrollment) => {
    if (!hasAllMandatoryAnswers(activity.specificationQuestions, answers)) {
      toast.error(t("please_fill_all_fields"));
      return;
    }

    action(authService, activity, setActivity, answers, setSubmitting);
  };

  const isDutch = (
    i18n.language ||
    member?.preferredLanguage ||
    tokenParsed?.locale ||
    "nl"
  )
    .toLowerCase()
    .startsWith("nl");

  return (
    <div className="grid grid-cols-1 lg:grid-cols-12 gap-8 items-start">
      {/* Poster Column */}
      <div className="lg:col-span-5 lg:sticky lg:top-8">
        <div className="relative w-full aspect-[1/1.414] bg-gray-100 rounded-2xl shadow-sm border border-gray-200 overflow-hidden">
          {/* Loading */}
          {posterStatus === "loading" && hasPoster && (
            <div className="absolute inset-0 flex flex-col items-center justify-center gap-4">
              <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-(--board-primary-light)" />
              <p className="text-gray-500 animate-pulse">{t("loading")}</p>
            </div>
          )}

          {/* No poster */}
          {!hasPoster && (
            <div className="absolute inset-0 flex flex-col items-center justify-center bg-gray-200">
              <ImageIcon className="text-gray-400 mb-2" size={48} />
              <span className="text-gray-400 text-sm font-medium">
                {t("no_poster")}
              </span>
            </div>
          )}

          {/* Error fallback */}
          {posterStatus === "error" && hasPoster && (
            <div className="absolute inset-0 flex flex-col items-center justify-center bg-gray-200">
              <ImageIcon className="text-gray-400 mb-2" size={48} />
              <span className="text-gray-400 text-sm font-medium">
                {t("no_poster")}
              </span>
            </div>
          )}

          {/* Content */}
          {hasPoster && (
            <img
              src={posterUrl}
              alt={activity.name}
              onLoad={() => setPosterStatus("loaded")}
              onError={() => setPosterStatus("error")}
              crossOrigin="use-credentials"
              className={`w-full h-full object-cover transition-all duration-500 hover:scale-105 ${
                posterStatus === "loading" ? "opacity-0" : "opacity-100"
              }`}
              loading="lazy"
            />
          )}
        </div>
      </div>

      {/* Details Column */}
      <div className="lg:col-span-7 flex flex-col gap-6">
        <section className="space-y-1">
          <h1 className="text-3xl font-bold text-gray-900 tracking-tight">
            {activity.name}
          </h1>
          <p className="text-lg font-semibold text-(--board-primary)">
            {activity.price === 0 || activity.price == null
              ? t("free")
              : `€ ${activity.price.toFixed(2)}`}
          </p>
        </section>

        <BorderedTile>
          <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-2 mb-2">
            <h3 className="font-bold text-gray-900">{t("description")}</h3>

            {isBoard && (
              <div className="flex flex-col sm:flex-row gap-2 w-full sm:w-auto">
                <Button
                  variant="secondary"
                  onClick={() => handleCopyForWhatsapp(activity, "NL")}
                  className="text-xs px-3 py-1 w-full sm:w-auto"
                >
                  {t("copy")} NL
                </Button>

                <Button
                  variant="secondary"
                  onClick={() => handleCopyForWhatsapp(activity, "EN")}
                  className="text-xs px-3 py-1 w-full sm:w-auto"
                >
                  {t("copy")} EN
                </Button>
              </div>
            )}
          </div>

          <div
            className="prose prose-sm max-w-none
              prose-p:!my-0.5
              prose-ul:!my-0.5
              prose-ol:!my-0.5
              prose-li:!my-0
              [&_li>p]:!my-0
              leading-snug
            "
          >
            <Markdown>
              {isDutch
                ? activity.dutchDescription || t("no_description_available_nl")
                : activity.englishDescription ||
                  t("no_description_available_en")}
            </Markdown>
          </div>
        </BorderedTile>

        {/* Info Grid */}
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-y-6 gap-x-4 p-2">
          <InfoItem
            icon={<Calendar size={18} />}
            label={t("date")}
            value={`${startDateString} ${startTimeString} - ${startDate.toDateString() !== endDate.toDateString() ? `${endDateString} ` : ""}${endTimeString}`}
          />
          <InfoItem
            icon={<MapPin size={18} />}
            label={t("location")}
            value={activity.location || ""}
          />
          {organizerName && activity.organizerId && (
            <InfoItem
              icon={<OrganizerIcon groupId={activity.organizerId} />}
              label={t("organizer")}
              value={organizerName}
            />
          )}
          {hasEnrollmentOpened(activity) && (
            <>
              <InfoItem
                icon={<Clock size={18} />}
                label={t("unenrollment_deadline")}
                value={
                  activity.unenrollmentDeadline
                    ? formatDate(
                        new Date(activity.unenrollmentDeadline),
                        "fullDateTime",
                      )
                    : t("none")
                }
              />
              <InfoItem
                icon={<Clock size={18} />}
                label={t("enrollment_deadline")}
                value={
                  activity.enrollmentDeadline
                    ? formatDate(
                        new Date(activity.enrollmentDeadline),
                        "fullDateTime",
                      )
                    : t("none")
                }
              />
              <InfoItem
                icon={<Users size={18} />}
                label={t("participants")}
                value={`${activity.enrollments.filter((e) => !e.isOnWaitingList).length}${activity.participantLimit ? ` ${t("of")} ${activity.participantLimit}` : ""}`}
              />
            </>
          )}
        </div>

        {(isEnrolled || canEnroll) && (
          <AnswerQuestionsTile
            questions={activity.specificationQuestions}
            answers={answers}
            onChange={(id, value) =>
              setAnswers((prev) => ({ ...prev, [id]: value }))
            }
            disabled={submitting || !canUnenroll}
          />
        )}

        {/* Archived Status Indicator */}
        {activity.isArchived && (
          <div className="flex items-center gap-3 p-3.5 rounded-xl border border-stone-300 bg-stone-100 text-stone-800 shadow-2xs">
            <Archive size={20} className="text-stone-600 shrink-0" />
            <div className="flex flex-col">
              <span className="text-sm font-semibold">
                {t("archived_activity_badge")}
              </span>
              <span className="text-xs text-stone-600">
                {t("archived_activity_notice")}
              </span>
            </div>
          </div>
        )}

        {/* Waiting List Position Indicator */}
        {currentEnrollment?.isOnWaitingList && waitingPosition !== null && (
          <div className="flex items-center gap-3 p-3.5 rounded-xl border border-amber-200 bg-amber-50 text-amber-900 shadow-2xs">
            <div className="flex items-center justify-center w-8 h-8 rounded-full bg-amber-200/80 text-amber-800 shrink-0 font-bold text-sm">
              #{waitingPosition}
            </div>
            <div className="flex flex-col">
              <span className="text-sm font-semibold">
                {t("waiting_list_position", { position: waitingPosition })}
              </span>
              <span className="text-xs text-amber-700">
                {aheadCount === 0
                  ? isDutch
                    ? "Je bent de eerstvolgende zodra er een plek vrijkomt!"
                    : "You are next in line if a spot opens up!"
                  : aheadCount === 1
                    ? t("waiting_list_ahead", { count: aheadCount })
                    : t("waiting_list_ahead_plural", { count: aheadCount })}
              </span>
            </div>
          </div>
        )}

        {/* Actions */}
        <div className="flex flex-col gap-3 pt-4 border-t border-gray-200">
          {isEnrolled
            ? canUnenroll && (
                <div className="flex flex-col gap-3">
                  {activity.specificationQuestions.length > 0 && (
                    <Button
                      variant="primary"
                      className="w-full sm:w-auto"
                      onClick={() => submitAnswers(handleUpdateEnrollment)}
                      disabled={submitting}
                    >
                      {submitting ? t("saving") : t("update_answers")}
                    </Button>
                  )}

                  <Button
                    variant="danger"
                    className="w-full sm:w-auto"
                    onClick={() =>
                      handleUnenrollment(
                        authService,
                        activity,
                        setActivity,
                        setSubmitting,
                      )
                    }
                    disabled={
                      submitting ||
                      (activity.unenrollmentDeadline
                        ? new Date(Date.now()) >
                          new Date(activity.unenrollmentDeadline)
                        : false)
                    }
                  >
                    {currentEnrollment.isOnWaitingList
                      ? t("leave_waitlist")
                      : t("sign_out")}
                    {submitting && "..."}
                  </Button>
                </div>
              )
            : canEnroll && (
                <Button
                  variant="primary"
                  className="w-full sm:w-auto"
                  onClick={() => submitAnswers(handleEnrollment)}
                  disabled={submitting}
                >
                  {activity.participantLimit &&
                  activity.participantLimit <=
                    (activity.enrollments.length || 0) &&
                  inTargetAudience
                    ? t("sign_in_on_waitlist")
                    : t("sign_in")}
                  {submitting && "..."}
                </Button>
              )}
          <Button
            variant="secondary"
            className="w-full sm:w-auto"
            onClick={() => handleAddToCalendar(activity, isDutch)}
          >
            <div className="flex items-center gap-2">
              <Calendar size={18} />
              {t("add_to_google_calendar")}
            </div>
          </Button>
          <Button
            variant="secondary"
            className="w-full sm:w-auto"
            onClick={() => handleDownloadIcs(activity, isDutch)}
          >
            <div className="flex items-center gap-2">
              <Download size={18} />
              {t("download_ics")}
            </div>
          </Button>
          {canClone && (
            <Button
              variant="secondary"
              className="w-full sm:w-auto"
              onClick={() =>
                navigate(`/activities/create?cloneFrom=${activity.id}`)
              }
            >
              <div className="flex items-center gap-2">
                <Copy size={18} />
                {t("clone_activity")}
              </div>
            </Button>
          )}
          {isBoard && (
            <Button
              variant="secondary"
              className="w-full sm:w-auto"
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
                if (setActivity) {
                  setActivity((prev) =>
                    prev ? { ...prev, isArchived: nextArchived } : prev,
                  );
                }
                toast.success(
                  nextArchived
                    ? t("activity_archived")
                    : t("activity_unarchived"),
                );
              }}
            >
              <div className="flex items-center gap-2">
                {activity.isArchived ? (
                  <ArchiveRestore size={18} />
                ) : (
                  <Archive size={18} />
                )}
                {activity.isArchived
                  ? t("unarchive_activity")
                  : t("archive_activity")}
              </div>
            </Button>
          )}
        </div>
      </div>
      {confirmModal}
    </div>
  );
}
