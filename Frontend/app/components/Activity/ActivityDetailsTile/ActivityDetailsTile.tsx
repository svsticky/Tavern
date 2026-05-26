import { t } from "i18next";
import {
  Calendar,
  Clock,
  Image as ImageIcon,
  MapPin,
  Users,
} from "lucide-react";
import { useEffect, useState } from "react";
import Markdown from "react-markdown";
import type {
  ActivityResponseDto,
  SpecificationAnswerResponseDto,
} from "~/api";
import { useApp } from "~/context/AppContext";
import { useAuth } from "~/context/AuthContext";
import type { TokenParsed } from "~/types/TokenParsed";
import { getEnv } from "~/util/config.utils";
import { formatDate } from "~/util/date.util";
import { isBoardOrCandidateBoard } from "~/util/group.util";
import { isMemberInTargetAudience } from "~/util/targetaudience.util";
import BorderedTile from "../../Tiles/BorderedTile";
import Button from "../../UI/Button";
import AnswerQuestionsTile from "../AnswerQuestionsTile";
import {
  handleAddToCalendar,
  handleCopyForWhatsapp,
  handleEnrollment,
  handleUnenrollment,
  handleUpdateEnrollment,
} from "./ActivityDetailsTile.handlers";
import InfoItem from "./InfoItem";

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
  const authService = useAuth();
  const { boardGroupId, candidateBoardGroupId, member } = useApp();
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

  const posterUrl = `${getEnv("ApiUrl")}/api/activities/${activity.id}/poster`;
  const hasPoster = !!activity.posterFileName;

  const startDate = new Date(activity.dateTimeStart);
  const endDate = new Date(activity.dateTimeEnd);

  const startDateString = startDate.toLocaleDateString("nl-NL", {
    day: "numeric",
    month: "long",
    year: "numeric",
  });

  const startTimeString = startDate.toLocaleTimeString("nl-NL", {
    hour: "2-digit",
    minute: "2-digit",
  });

  const endDateString = endDate.toLocaleDateString("nl-NL", {
    day: "numeric",
    month: "long",
    year: "numeric",
  });

  const endTimeString = endDate.toLocaleTimeString("nl-NL", {
    hour: "2-digit",
    minute: "2-digit",
  });

  const ableToEnroll =
    !member?.suspended &&
    member?.studyEnrollments?.some(
      (se) =>
        se.status === "Enrolled" ,
    );

  const inTargetAudience = isMemberInTargetAudience(
    member,
    activity.allowedAudience,
  );
  const currentEnrollment = tokenParsed
    ? activity.enrollments.find((e) => e.member.id === tokenParsed.UserId)
    : undefined;
  const isEnrolled = !!currentEnrollment;
  const [answers, setAnswers] = useState<Record<number, string>>({});
  const isBoard = isBoardOrCandidateBoard(
    tokenParsed,
    boardGroupId,
    candidateBoardGroupId,
  );

  useEffect(() => {
    setAnswers(toAnswerMap(currentEnrollment?.specificationAnswers));
  }, [currentEnrollment?.specificationAnswers]);

  return (
    <div className="grid grid-cols-1 lg:grid-cols-12 gap-8 items-start">
      {/* Poster Column */}
      <div className="lg:col-span-5 lg:sticky lg:top-8">
        <div className="relative w-full aspect-[1/1.414] bg-slate-100 rounded-3xl shadow-lg border border-slate-100 overflow-hidden">
          {/* Loading */}
          {posterStatus === "loading" && hasPoster && (
            <div className="absolute inset-0 flex flex-col items-center justify-center gap-4">
              <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-(--board-primary-light)" />
              <p className="text-gray-500 animate-pulse">{t("loading")}</p>
            </div>
          )}

          {/* No poster */}
          {!hasPoster && (
            <div className="absolute inset-0 flex flex-col items-center justify-center bg-slate-200">
              <ImageIcon className="text-slate-400 mb-2" size={48} />
              <span className="text-slate-400 text-sm font-medium">
                {t("no_poster")}
              </span>
            </div>
          )}

          {/* Error fallback */}
          {posterStatus === "error" && hasPoster && (
            <div className="absolute inset-0 flex flex-col items-center justify-center bg-slate-200">
              <ImageIcon className="text-slate-400 mb-2" size={48} />
              <span className="text-slate-400 text-sm font-medium">
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
              className={`w-full h-full object-cover transition-opacity duration-500 ${
                posterStatus === "loading" ? "opacity-0" : "opacity-100"
              }`}
              loading="lazy"
            />
          )}
        </div>
      </div>

      {/* Info Column */}
      <div className="lg:col-span-7 flex flex-col gap-6">
        <section>
          <h1 className="text-4xl font-black text-slate-900 mt-4 mb-2 tracking-tight">
            {activity.name}
          </h1>
          <p className="text-2xl font-semibold text-slate-800">
            {activity.price === 0 || activity.price == null
              ? t("free")
              : `€ ${activity.price.toFixed(2)}`}
          </p>
        </section>

        <BorderedTile>
          <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-2 mb-2">
            <h3 className="font-bold text-slate-900">{t("description")}</h3>

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
              {tokenParsed?.locale === "NL"
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
        </div>

        {activity.isEnrollable && (
          <AnswerQuestionsTile
            questions={activity.specificationQuestions}
            answers={answers}
            onChange={(id, value) =>
              setAnswers((prev) => ({ ...prev, [id]: value }))
            }
            disabled={
              submitting || activity.enrollmentDeadline
                ? new Date(Date.now()) > new Date(activity.enrollmentDeadline!)
                : false
            }
          />
        )}

        {/* Actions */}
        <div className="flex flex-col gap-3 pt-4 border-t border-slate-100">
          {ableToEnroll &&
            (isEnrolled ? (
              <div className="flex flex-col gap-3">
                {activity.specificationQuestions.length > 0 && (
                  <Button
                    variant="primary"
                    className="w-full sm:w-auto"
                    onClick={() =>
                      handleUpdateEnrollment(
                        authService,
                        activity,
                        setActivity,
                        answers,
                        setSubmitting,
                      )
                    }
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
                  {t("sign_out")}
                  {submitting && "..."}
                </Button>
              </div>
            ) : (
              activity.isEnrollable && (
                <Button
                  variant="primary"
                  className="w-full sm:w-auto"
                  onClick={() =>
                    handleEnrollment(
                      authService,
                      activity,
                      setActivity,
                      answers,
                      setSubmitting,
                    )
                  }
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
              )
            ))}

          <Button
            variant="secondary"
            className="w-full sm:w-auto"
            onClick={() => handleAddToCalendar(activity)}
          >
            <div className="flex items-center gap-2">
              <Calendar size={18} />
              {t("add_to_calendar")}
            </div>
          </Button>
        </div>
      </div>
    </div>
  );
}
