import toast from "react-hot-toast";
import { deleteApiEnrollmentsByActivityIdByMemberId, postApiEnrollments, putApiEnrollmentsByActivityIdByMemberId, type ActivityResponseDto, type Language } from "~/api";
import type Keycloak from "keycloak-js";
import { formatForGoogleCalendar, formatForWhatsApp } from "~/util/markdown.util";
import { t } from "i18next";
import { formatDate } from "~/util/date.util";

export const handleAddToCalendar = (activity: ActivityResponseDto) => {
    const title = encodeURIComponent(activity.name || 'Activiteit');
    const description = encodeURIComponent(formatForGoogleCalendar(activity.dutchDescription) || '');
    const location = encodeURIComponent(activity.location || 'TBA');

    const formatDateGoogle = (dateStr: string | undefined | null) => {
      if (!dateStr) return '';
      return new Date(dateStr).toISOString().replace(/-|:|\.\d+/g, '');
    };

    const startTime = formatDateGoogle(activity.dateTimeStart);
    const endTime = formatDateGoogle(activity.dateTimeEnd);

    const googleUrl = `https://www.google.com/calendar/render?action=TEMPLATE&text=${title}&details=${description}&location=${location}&dates=${startTime}/${endTime}`;

    window.open(googleUrl, '_blank', 'noreferrer');
};

export const handleEnrollment = async (
        initialized: boolean, 
        keycloak: Keycloak, 
        activity: ActivityResponseDto, 
        setActivity:  React.Dispatch<React.SetStateAction<ActivityResponseDto | null>> | undefined, 
        answers: Record<number, string>,
        setSubmitting: (submitting: boolean) => void
    ) => {
    if (!initialized || !keycloak.authenticated || !activity.id || !keycloak.tokenParsed?.UserId) {
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
            specificationAnswers: Object.entries(answers).map(([questionId, answer]) => ({
              questionId: Number(questionId),
              answer
            }))
          }
        });

        if (response.data) {
          const newEnrollment = {
            isOnWaitingList: response.data.isOnWaitingList,
            memberId: keycloak.tokenParsed?.UserId,
            activityId: activity.id,
            member: {
              id: keycloak.tokenParsed?.UserId,
              firstName: keycloak.tokenParsed?.given_name,
              lastName: keycloak.tokenParsed?.family_name,
              profilePicturePath: response.data.member?.profilePicturePath
            },
            specificationAnswers: response.data.specificationAnswers
          } as any;

          activity.enrollments = activity.enrollments
            ? [...activity.enrollments, newEnrollment]
            : [newEnrollment];

          setActivity && setActivity({ ...activity });
        }

      } catch (error) {
        console.error("Error while enrolling:", error);
        throw error;
      } finally {
        setSubmitting(false);
      }
    }

    toast.promise(enrollmentProcess(), {
      loading: t("signing_in"),
      success: t("enrollment_successful"),
      error: t("enrollment_failed")
    });
};

export const handleUpdateEnrollment = async (
        initialized: boolean, 
        keycloak: Keycloak, 
        activity: ActivityResponseDto, 
        setActivity: React.Dispatch<React.SetStateAction<ActivityResponseDto | null>> | undefined, 
        answers: Record<number, string>, 
        setSubmitting: (submitting: boolean) => void
    ) => {
    if (!initialized || !keycloak.authenticated || !activity.id || !keycloak.tokenParsed?.UserId) return;

    const updateProcess = async () => {
      try {
        setSubmitting(true);

        const response = await putApiEnrollmentsByActivityIdByMemberId({
          path: {
            activityId: activity.id,
            memberId: keycloak.tokenParsed?.UserId
          },
          body: {
            activityId: activity.id,
            memberId: keycloak.tokenParsed?.UserId,
            specificationAnswers: Object.entries(answers).map(([questionId, answer]) => ({
              questionId: Number(questionId),
              answer: String(answer)
            }))
          }
        });

        if (response.error) {
          throw new Error("Update failed");
        }

        const updatedEnrollments = activity.enrollments.map(e => {
          if (e.member.id === keycloak.tokenParsed?.UserId) {
            return {
              ...e,
              specificationAnswers: e.specificationAnswers?.map(existingAns => ({
                ...existingAns,
                answer: answers[existingAns.questionId] ?? existingAns.answer
              }))
            };
          }
          return e;
        });

        setActivity && setActivity({ ...activity, enrollments: updatedEnrollments });
      } catch (error) {
        console.error("Error while updating enrollment:", error);
        throw error;
      } finally {
        setSubmitting(false);
      }
    }

    toast.promise(updateProcess(), {
      loading: t("saving"),
      success: t("answers_updated"),
      error: t("update_failed")
    });
};

export const handleUnenrollment = async (
        initialized: boolean, 
        keycloak: Keycloak, 
        activity: ActivityResponseDto, 
        setActivity: any,
        setSubmitting: (submitting: boolean) => void
    ) => {
    if (!initialized || !keycloak.authenticated || !activity.id || !keycloak.tokenParsed?.UserId) {
      return;
    }

    const unenrollmentProcess = async () => {
      try {
        setSubmitting(true);

        console.log("Attempting to unenroll with ActivityId:", activity.id, "and MemberId:", keycloak.tokenParsed?.UserId);
        
        const response = await deleteApiEnrollmentsByActivityIdByMemberId({
          path: {
            ActivityId: Number(activity.id),
            MemberId: String(keycloak.tokenParsed?.UserId)
          }
        });
        
        if (response.error) {
          throw new Error("Unenrollment failed");
        }

        activity.enrollments = activity.enrollments.filter(e => e.member.id !== keycloak.tokenParsed?.UserId);
        setActivity && setActivity({ ...activity });
      } catch (error) {
        console.error("Error while unenrolling:", error);
        throw error;
      } finally {
        setSubmitting(false);
      }
    }

    toast.promise(unenrollmentProcess(), {
      loading: t("signing_out"),
      success: t("unenrollment_successful"),
      error: t("unenrollment_failed")
    });
};

export const handleCopyForWhatsapp = async (activity: ActivityResponseDto, lang: Language) => {
    const startDate = new Date(activity.dateTimeStart);
    const endDate = new Date(activity.dateTimeEnd);

    const text = lang === "NL" ?
      `*${activity.name} | ${formatDate(startDate, "fullDateTime")} - ${formatDate(endDate, "fullDateTime")} | Locatie: ${activity.location || 'TBA'} | Prijs: ${activity.price === 0 || activity.price == null ? 'Gratis' : `€ ${activity.price.toFixed(2)}`}* \n\n${window.location.href}\n\n${formatForWhatsApp(activity.dutchDescription)}` :
      `*${activity.name} | ${formatDate(startDate, "fullDateTime")} - ${formatDate(endDate, "fullDateTime")} | Location: ${activity.location || 'TBA'} | Price: ${activity.price === 0 || activity.price == null ? 'Free' : `€ ${activity.price.toFixed(2)}`}* \n\n${window.location.href}\n\n${formatForWhatsApp(activity.englishDescription)}`;

    toast.promise(navigator.clipboard.writeText(text), {
      loading: t("copying"),
      success: t("copy_successful"),
      error: t("copy_failed")
    });
};