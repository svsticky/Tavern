import type React from "react";
import { t } from "i18next";
import toast from "react-hot-toast";
import { getApiActivitiesByIdEnrollmentsExport, postApiEnrollments, type ActivityResponseDto, type MemberResponseDto } from "~/api";

export const handleDownloadEnrollments = (activity: ActivityResponseDto) => {
  const handleDownloadAction = async () => {
    try {
      const response = await getApiActivitiesByIdEnrollmentsExport({
        path: { id: activity.id },
        responseType: "blob"
      });

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
    error: t("download_failed"),
  });
};

type EnrollArgs = {
  member: MemberResponseDto;
  activity: ActivityResponseDto;
  setActivity: React.Dispatch<React.SetStateAction<ActivityResponseDto | null>>;
  setLoading: (loading: boolean) => void;
  setIsSearchOpen: (open: boolean) => void;
};

export const handleEnrollParticipant = async ({ member, activity, setActivity, setLoading, setIsSearchOpen }: EnrollArgs) => {
  setLoading(true);
  try {
    const enrollment = await postApiEnrollments({
      body: { activityId: activity.id, memberId: member.id! }
    });

    if (enrollment.data) {
      toast.success(t("member_enrolled_success"));
      activity.enrollments.push({
        member: enrollment.data.member,
        activity: enrollment.data.activity,
        isOnWaitingList: enrollment.data.isOnWaitingList,
        price: enrollment.data.price
      });
      setActivity({ ...activity });
      setIsSearchOpen(false);
    }
  } catch (err) {
    toast.error(t("enroll_failed"));
  } finally {
    setLoading(false);
  }
};

export const handleUnenrollParticipant = (
  memberId: string,
  activity: ActivityResponseDto,
  setActivity: React.Dispatch<React.SetStateAction<ActivityResponseDto | null>>
) => {
  activity.enrollments = activity.enrollments.filter((e) => e.member.id !== memberId);
  setActivity({ ...activity });
};

export const handleMoveToParticipants = (
  memberId: string,
  activity: ActivityResponseDto,
  setActivity: React.Dispatch<React.SetStateAction<ActivityResponseDto | null>>
) => {
  const enrollment = activity.enrollments.find((e) => e.member.id === memberId);
  if (enrollment) {
    enrollment.isOnWaitingList = false;
    setActivity({ ...activity });
  }
};
