import { t } from "i18next";
import type React from "react";
import toast from "react-hot-toast";
import {
  type ActivityResponseDto,
  getActivitiesByIdEnrollmentsExport,
  type MemberResponseDto,
  postEnrollments,
} from "~/api";
import { appendErrorMessage } from "~/util/error.util";

/**
 * Triggers a download of the activity's enrollment list as a CSV file.
 *
 * This handler fetches a blob from the API, creates a temporary DOM element to
 * initiate the download, and cleans up resources (URL objects and elements) afterward.
 *
 * @param activity - The activity object for which to export enrollments.
 */
export const handleDownloadEnrollments = (activity: ActivityResponseDto) => {
  const handleDownloadAction = async () => {
    try {
      const response = await getActivitiesByIdEnrollmentsExport({
        path: { id: activity.id },
        responseType: "blob",
      });

      if (response.error || !response.data) {
        throw response.error ?? new Error("Failed to download enrollments");
      }

      const blob = new Blob([response.data as any], { type: "text/csv" });
      const url = window.URL.createObjectURL(blob);

      const link = document.createElement("a");
      link.href = url;
      link.setAttribute("download", `enrollments_${activity.name}.csv`);
      document.body.appendChild(link);
      link.click();

      link.parentNode?.removeChild(link);
      window.URL.revokeObjectURL(url);
    } catch (error) {
      console.error("Error downloading enrollments:", error);
      throw error;
    }
  };

  toast.promise(handleDownloadAction(), {
    loading: t("downloading"),
    success: t("download_success"),
    error: (error) => appendErrorMessage(t("download_failed"), error),
  });
};

/**
 * Manually enrolls a specific member into an activity.
 *
 * This is an administrative action that bypasses standard user checks. Upon
 * success, it updates the local activity state by pushing the new enrollment
 * into the list and closes the search interface.
 *
 * @function
 * @param {Object} args - The configuration object.
 * @param {MemberResponseDto} args.member - The member to be enrolled.
 * @param {ActivityResponseDto} args.activity - The current activity.
 * @param {React.Dispatch} args.setActivity - State setter to update the activity with the new participant.
 * @param {(loading: boolean) => void} args.setLoading - Callback to toggle the local loading state.
 * @param {(open: boolean) => void} args.setIsSearchOpen - Callback to close the member search modal.
 */
export const handleEnrollParticipant = async ({
  member,
  activity,
  setActivity,
  setLoading,
  setIsSearchOpen,
}: {
  member: MemberResponseDto;
  activity: ActivityResponseDto;
  setActivity: React.Dispatch<React.SetStateAction<ActivityResponseDto | null>>;
  setLoading: (loading: boolean) => void;
  setIsSearchOpen: (open: boolean) => void;
}) => {
  setLoading(true);
  const enrollProcess = async () => {
    try {
      const enrollment = await postEnrollments({
        body: { activityId: activity.id, memberId: member.id! },
      });

      if (enrollment.error || !enrollment.data) {
        throw new Error("Enrollment failed");
      }

      activity.enrollments.push({
        member: enrollment.data.member,
        activity: enrollment.data.activity,
        isOnWaitingList: enrollment.data.isOnWaitingList,
        price: enrollment.data.price,
        registeredOn: enrollment.data.registeredOn,
      });
      setActivity({ ...activity });
      setIsSearchOpen(false);
    } catch (err) {
      console.log("Failed to enroll member:", err);
      throw err;
    } finally {
      setLoading(false);
    }
  };

  toast.promise(enrollProcess(), {
    loading: t("enrolling"),
    success: t("enrollment_successful"),
    error: (error) => appendErrorMessage(t("enrollment_failed"), error),
  });
};

/**
 * Removes a participant from the local activity state after they have been unenrolled.
 *
 * @function
 * @param {string} memberId - The unique ID of the member to remove.
 * @param {ActivityResponseDto} activity - The activity object to filter.
 * @param {React.Dispatch} setActivity - State setter to apply the updated enrollment list.
 */
export const handleUnenrollParticipant = (
  memberId: string,
  activity: ActivityResponseDto,
  setActivity: React.Dispatch<React.SetStateAction<ActivityResponseDto | null>>,
) => {
  activity.enrollments = activity.enrollments.filter(
    (e) => e.member.id !== memberId,
  );
  setActivity({ ...activity });
};

export const handleMoveToParticipants = (
  memberId: string,
  activity: ActivityResponseDto,
  setActivity: React.Dispatch<React.SetStateAction<ActivityResponseDto | null>>,
) => {
  const enrollment = activity.enrollments.find((e) => e.member.id === memberId);
  if (enrollment) {
    enrollment.isOnWaitingList = false;
    setActivity({ ...activity });
  }
};
