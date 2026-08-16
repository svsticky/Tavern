import { t } from "i18next";
import { AlertTriangle, Check } from "lucide-react";
import toast from "react-hot-toast";
import {
  type ActivityResponseDto,
  deleteEnrollmentsByActivityIdByMemberId,
  type Language,
  postEnrollments,
  putEnrollmentsByActivityIdByMemberId,
} from "~/api";
import type { IAuthService } from "~/auth/IAuthService";
import { formatDate } from "~/util/date.util";
import { appendErrorMessage } from "~/util/error.util";
import {
  formatForGoogleCalendar,
  formatForWhatsApp,
} from "~/util/markdown.util";

/**
 * Generates a Google Calendar event link and opens it in a new browser tab.
 *
 * @param activity - The activity object containing name, description, location, and date details.
 */
export const handleAddToCalendar = (activity: ActivityResponseDto) => {
  const title = encodeURIComponent(activity.name || "Activiteit");
  const description = encodeURIComponent(
    formatForGoogleCalendar(activity.dutchDescription) || "",
  );
  const location = encodeURIComponent(activity.location || "TBA");

  const formatDateGoogle = (dateStr: string | undefined | null) => {
    if (!dateStr) return "";
    return new Date(dateStr).toISOString().replace(/-|:|\.\d+/g, "");
  };

  const startTime = formatDateGoogle(activity.dateTimeStart);
  const endTime = formatDateGoogle(activity.dateTimeEnd);

  const googleUrl = `https://www.google.com/calendar/render?action=TEMPLATE&text=${title}&details=${description}&location=${location}&dates=${startTime}/${endTime}`;

  window.open(googleUrl, "_blank", "noreferrer");
};

/**
 * Handles the enrollment of a user into an activity.
 * Validates authentication status, submits answers to the API, and updates the local state.
 *
 * @param initialized - Boolean indicating if the authentication client is initialized.
 * @param authService - The authentication service instance to retrieve user information.
 * @param activity - The current activity object.
 * @param setActivity - State setter to update the activity with the new enrollment list.
 * @param answers - A record of question IDs and user-provided answers.
 * @param setSubmitting - Callback to toggle the loading state of the UI.
 * @returns A promise that resolves when the enrollment process completes.
 */
export const handleEnrollment = async (
  authService: IAuthService,
  activity: ActivityResponseDto,
  setActivity:
    | React.Dispatch<React.SetStateAction<ActivityResponseDto | null>>
    | undefined,
  answers: Record<number, string>,
  setSubmitting: (submitting: boolean) => void,
) => {
  const tokenParsed = await authService.getTokenParsed();
  if (!tokenParsed) {
    console.error("User not authenticated");
    return;
  }

  if (!authService.isAuthenticated() || !activity.id) {
    console.error("User not authenticated or activity missing");
    return;
  }

  const enrollmentProcess = async () => {
    try {
      setSubmitting(true);

      const response = await postEnrollments({
        body: {
          activityId: activity.id,
          memberId: tokenParsed.UserId,
          specificationAnswers: Object.entries(answers).map(
            ([questionId, answer]) => ({
              questionId: Number(questionId),
              answer,
            }),
          ),
        },
      });

      if (response.error) {
        throw response.error ?? new Error("Enrollment failed");
      }

      if (response.data) {
        const submittedAnswers = Object.entries(answers).map(
          ([questionId, answer]) => ({
            questionId: Number(questionId),
            answerId: 0,
            answer: String(answer),
          }),
        );

        const newEnrollment = {
          isOnWaitingList: response.data.isOnWaitingList,
          memberId: tokenParsed.UserId,
          activityId: activity.id,
          member: {
            id: tokenParsed.UserId,
            firstName: tokenParsed.given_name,
            lastName: tokenParsed.family_name,
            profilePicturePath: response.data.member?.profilePicturePath,
          },
          specificationAnswers:
            (response.data.specificationAnswers?.length ?? 0) > 0
              ? response.data.specificationAnswers
              : submittedAnswers,
        } as any;

        activity.enrollments = activity.enrollments
          ? [...activity.enrollments, newEnrollment]
          : [newEnrollment];

        setActivity?.({ ...activity });

        return response.data;
      } else {
        throw new Error("No enrollment data returned from API");
      }
    } catch (error) {
      console.error("Error while enrolling:", error);
      throw error;
    } finally {
      setSubmitting(false);
    }
  };

  toast.promise(enrollmentProcess(), {
    loading: t("signing_in"),
    success: (data) => {
      if (data.isOnWaitingList) {
        toast(t("enrollment_waiting_list"), {
          icon: <AlertTriangle className="text-yellow-500" />,
        });

        return "";
      }

      toast.success(t("enrollment_successful"), {
        icon: <Check className="text-green-500" />,
      });

      return "";
    },
    error: (error) => appendErrorMessage(t("enrollment_failed"), error),
  });
};

/**
 * Updates an existing enrollment's answers for the current user.
 *
 * @param initialized - Boolean indicating if the authentication client is initialized.
 * @param authService - The authentication service instance for user authentication.
 * @param activity - The current activity object containing enrollments.
 * @param setActivity - State setter to update the local activity data.
 * @param answers - The new set of answers to be updated.
 * @param setSubmitting - Callback to toggle the loading state.
 */
export const handleUpdateEnrollment = async (
  authService: IAuthService,
  activity: ActivityResponseDto,
  setActivity:
    | React.Dispatch<React.SetStateAction<ActivityResponseDto | null>>
    | undefined,
  answers: Record<number, string>,
  setSubmitting: (submitting: boolean) => void,
) => {
  const tokenParsed = await authService.getTokenParsed();
  if (!tokenParsed) {
    console.error("User not authenticated");
    return;
  }

  if (!authService.isAuthenticated() || !activity.id || !tokenParsed.UserId)
    return;

  const updateProcess = async () => {
    try {
      setSubmitting(true);

      const response = await putEnrollmentsByActivityIdByMemberId({
        path: {
          activityId: activity.id,
          memberId: tokenParsed.UserId,
        },
        body: {
          activityId: activity.id,
          memberId: tokenParsed.UserId,
          specificationAnswers: Object.entries(answers).map(
            ([questionId, answer]) => ({
              questionId: Number(questionId),
              answer: String(answer),
            }),
          ),
        },
      });

      if (response.error) {
        throw response.error ?? new Error("Update failed");
      }

      const existingEnrollment = activity.enrollments.find(
        (e) => e.member?.id === tokenParsed.UserId,
      );
      const existingAnswerIds = new Map(
        (existingEnrollment?.specificationAnswers ?? []).map((answer) => [
          answer.questionId,
          answer.answerId,
        ]),
      );

      const updatedSpecificationAnswers = Object.entries(answers).map(
        ([questionId, answer]) => ({
          questionId: Number(questionId),
          answerId: existingAnswerIds.get(Number(questionId)) ?? 0,
          answer: String(answer),
        }),
      );

      const updatedEnrollments = activity.enrollments.map((e) => {
        if (e.member?.id === tokenParsed.UserId) {
          return {
            ...e,
            specificationAnswers: updatedSpecificationAnswers,
          };
        }
        return e;
      });

      setActivity?.({ ...activity, enrollments: updatedEnrollments });
    } catch (error) {
      console.error("Error while updating enrollment:", error);
      throw error;
    } finally {
      setSubmitting(false);
    }
  };

  toast.promise(updateProcess(), {
    loading: t("saving"),
    success: t("answers_updated"),
    error: (error) => appendErrorMessage(t("update_failed"), error),
  });
};

/**
 * Removes the current user from an activity's enrollment list.
 *
 * @param initialized - Authentication initialization status.
 * @param authService - The authentication service instance.
 * @param activity - The activity from which to unenroll.
 * @param setActivity - State setter to update the activity's enrollment list locally.
 * @param setSubmitting - Callback to toggle loading state.
 */
export const handleUnenrollment = async (
  authService: IAuthService,
  activity: ActivityResponseDto,
  setActivity: any,
  setSubmitting: (submitting: boolean) => void,
) => {
  const tokenParsed = await authService.getTokenParsed();

  if (!authService.isAuthenticated() || !activity.id || !tokenParsed) {
    console.error("User not authenticated or activity missing");
    return;
  }

  const unenrollmentProcess = async () => {
    try {
      setSubmitting(true);

      const response = await deleteEnrollmentsByActivityIdByMemberId({
        path: {
          activityId: Number(activity.id),
          memberId: String(tokenParsed.UserId),
        },
      });

      if (response.error) {
        throw response.error ?? new Error("Unenrollment failed");
      }

      activity.enrollments = activity.enrollments.filter(
        (e) => e.member?.id !== tokenParsed.UserId,
      );
      setActivity?.({ ...activity });
    } catch (error) {
      console.error("Error while unenrolling:", error);
      throw error;
    } finally {
      setSubmitting(false);
    }
  };

  toast.promise(unenrollmentProcess(), {
    loading: t("signing_out"),
    success: t("unenrollment_successful"),
    error: (error) => appendErrorMessage(t("unenrollment_failed"), error),
  });
};

/**
 * Formats activity details into a WhatsApp-friendly message and copies it to the clipboard.
 * Supports localization (NL/EN) for the message template.
 *
 * @param activity - The activity data to format.
 * @param lang - The language preference ('NL' or 'EN').
 */
export const handleCopyForWhatsapp = async (
  activity: ActivityResponseDto,
  lang: Language,
) => {
  const startDate = new Date(activity.dateTimeStart);
  const endDate = new Date(activity.dateTimeEnd);

  const text =
    lang === "NL"
      ? `*${activity.name} | ${formatDate(startDate, "fullDateTime")} - ${formatDate(endDate, "fullDateTime")} | Locatie: ${activity.location || "TBA"} | Prijs: ${activity.price === 0 || activity.price == null ? "Gratis" : `€ ${activity.price.toFixed(2)}`}* \n\n${window.location.href}\n\n${formatForWhatsApp(activity.dutchDescription)}`
      : `*${activity.name} | ${formatDate(startDate, "fullDateTime")} - ${formatDate(endDate, "fullDateTime")} | Location: ${activity.location || "TBA"} | Price: ${activity.price === 0 || activity.price == null ? "Free" : `€ ${activity.price.toFixed(2)}`}* \n\n${window.location.href}\n\n${formatForWhatsApp(activity.englishDescription)}`;

  toast.promise(navigator.clipboard.writeText(text), {
    loading: t("copying"),
    success: t("copy_successful"),
    error: (error) => appendErrorMessage(t("copy_failed"), error),
  });
};
