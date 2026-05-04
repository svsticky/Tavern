import { t } from "i18next";
import type Keycloak from "keycloak-js";
import toast from "react-hot-toast";
import {
  type ActivityResponseDto,
  deleteApiEnrollmentsByActivityIdByMemberId,
  type Language,
  postApiEnrollments,
  putApiEnrollmentsByActivityIdByMemberId,
} from "~/api";
import { formatDate } from "~/util/date.util";
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
 * @param keycloak - The Keycloak instance for user authentication and token data.
 * @param activity - The current activity object.
 * @param setActivity - State setter to update the activity with the new enrollment list.
 * @param answers - A record of question IDs and user-provided answers.
 * @param setSubmitting - Callback to toggle the loading state of the UI.
 * @returns A promise that resolves when the enrollment process completes.
 */
export const handleEnrollment = async (
  initialized: boolean,
  keycloak: Keycloak,
  activity: ActivityResponseDto,
  setActivity:
    | React.Dispatch<React.SetStateAction<ActivityResponseDto | null>>
    | undefined,
  answers: Record<number, string>,
  setSubmitting: (submitting: boolean) => void,
) => {
  if (
    !initialized ||
    !keycloak.authenticated ||
    !activity.id ||
    !keycloak.tokenParsed?.UserId
  ) {
    console.error("User not authenticated or activity missing");
    return;
  }

  const enrollmentProcess = async () => {
    try {
      setSubmitting(true);

      const response = await postApiEnrollments({
        body: {
          activityId: activity.id,
          memberId: keycloak.tokenParsed?.UserId,
          specificationAnswers: Object.entries(answers).map(
            ([questionId, answer]) => ({
              questionId: Number(questionId),
              answer,
            }),
          ),
        },
      });

      if (response.error) throw new Error("Enrollment failed");

      if (response.data) {
        const newEnrollment = {
          isOnWaitingList: response.data.isOnWaitingList,
          memberId: keycloak.tokenParsed?.UserId,
          activityId: activity.id,
          member: {
            id: keycloak.tokenParsed?.UserId,
            firstName: keycloak.tokenParsed?.given_name,
            lastName: keycloak.tokenParsed?.family_name,
            profilePicturePath: response.data.member?.profilePicturePath,
          },
          specificationAnswers: response.data.specificationAnswers,
        } as any;

        activity.enrollments = activity.enrollments
          ? [...activity.enrollments, newEnrollment]
          : [newEnrollment];

        setActivity?.({ ...activity });
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
    success: t("enrollment_successful"),
    error: t("enrollment_failed"),
  });
};

/**
 * Updates an existing enrollment's answers for the current user.
 *
 * @param initialized - Boolean indicating if the authentication client is initialized.
 * @param keycloak - The Keycloak instance for user authentication.
 * @param activity - The current activity object containing enrollments.
 * @param setActivity - State setter to update the local activity data.
 * @param answers - The new set of answers to be updated.
 * @param setSubmitting - Callback to toggle the loading state.
 */
export const handleUpdateEnrollment = async (
  initialized: boolean,
  keycloak: Keycloak,
  activity: ActivityResponseDto,
  setActivity:
    | React.Dispatch<React.SetStateAction<ActivityResponseDto | null>>
    | undefined,
  answers: Record<number, string>,
  setSubmitting: (submitting: boolean) => void,
) => {
  if (
    !initialized ||
    !keycloak.authenticated ||
    !activity.id ||
    !keycloak.tokenParsed?.UserId
  )
    return;

  const updateProcess = async () => {
    try {
      setSubmitting(true);

      const response = await putApiEnrollmentsByActivityIdByMemberId({
        path: {
          activityId: activity.id,
          memberId: keycloak.tokenParsed?.UserId,
        },
        body: {
          activityId: activity.id,
          memberId: keycloak.tokenParsed?.UserId,
          specificationAnswers: Object.entries(answers).map(
            ([questionId, answer]) => ({
              questionId: Number(questionId),
              answer: String(answer),
            }),
          ),
        },
      });

      if (response.error) {
        throw new Error("Update failed");
      }

      const updatedEnrollments = activity.enrollments.map((e) => {
        if (e.member.id === keycloak.tokenParsed?.UserId) {
          return {
            ...e,
            specificationAnswers: e.specificationAnswers?.map(
              (existingAns) => ({
                ...existingAns,
                answer: answers[existingAns.questionId] ?? existingAns.answer,
              }),
            ),
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
    error: t("update_failed"),
  });
};

/**
 * Removes the current user from an activity's enrollment list.
 *
 * @param initialized - Authentication initialization status.
 * @param keycloak - Keycloak instance.
 * @param activity - The activity from which to unenroll.
 * @param setActivity - State setter to update the activity's enrollment list locally.
 * @param setSubmitting - Callback to toggle loading state.
 */
export const handleUnenrollment = async (
  initialized: boolean,
  keycloak: Keycloak,
  activity: ActivityResponseDto,
  setActivity: any,
  setSubmitting: (submitting: boolean) => void,
) => {
  if (
    !initialized ||
    !keycloak.authenticated ||
    !activity.id ||
    !keycloak.tokenParsed?.UserId
  ) {
    return;
  }

  const unenrollmentProcess = async () => {
    try {
      setSubmitting(true);

      const response = await deleteApiEnrollmentsByActivityIdByMemberId({
        path: {
          activityId: Number(activity.id),
          memberId: String(keycloak.tokenParsed?.UserId),
        },
      });

      if (response.error) {
        throw new Error("Unenrollment failed");
      }

      activity.enrollments = activity.enrollments.filter(
        (e) => e.member.id !== keycloak.tokenParsed?.UserId,
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
    error: t("unenrollment_failed"),
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
    error: t("copy_failed"),
  });
};
