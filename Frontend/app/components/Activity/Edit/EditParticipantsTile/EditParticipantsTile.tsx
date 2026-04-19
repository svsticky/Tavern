import { t } from "i18next";
import toast from "react-hot-toast";
import { deleteApiEnrollmentsByActivityIdByMemberId, getApiActivitiesByIdEnrollmentsExport, postApiEnrollments, type ActivityResponseDto, type Member } from "~/api";
import BorderedTile from "~/components/Tiles/BorderedTile";
import Button from "~/components/UI/Button";
import { FormHeader } from "~/components/UI/Form/FormHeader";
import EditParticipantTile from "./EditParticipantTile";
import EditWaitinglistParticipantTile from "./EditWaitinglistParticipantTile";
import Modal from "~/components/UI/Modal";
import { useState } from "react";
import SearchMemberOverlay from "../../../Member/SearchMemberOverlay";

export default function EditParticipantsTile({activity, setActivity}: {activity: ActivityResponseDto; setActivity: React.Dispatch<React.SetStateAction<ActivityResponseDto | null>>}) {
    const [isSearchOpen, setIsSearchOpen] = useState(false);
    const [loading, setLoading] = useState(false);
  
    const enrollments = activity?.enrollments.filter(e => !e.isOnWaitingList) ?? [];
    const waitingList = activity?.enrollments.filter(e => e.isOnWaitingList) ?? [];

    const handleDownloadEnrollments = () => {
      const handleDownloadAction = async () => {
        try {
          const response = await getApiActivitiesByIdEnrollmentsExport({ 
              path: { id: activity.id },
              responseType: 'blob' 
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
    }

    const handleEnrollment = async (member: Member, isOnWaitingList: boolean, price: number) => {
      
    }

    const handleEnroll = async (member: Member) => {
      setLoading(true); 
      try {
        const enrollment = await postApiEnrollments({
          body: { activityId: activity.id, memberId: member.id }
        });

        if(enrollment.data) {
          toast.success(t("member_enrolled_success"));
            activity.enrollments.push({
            memberId: member.id,
            member: member,
            activityId: activity.id,
            isOnWaitingList: enrollment.data.isOnWaitingList ?? false,
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

    const handleUnenroll = (memberId: string) => {
      activity.enrollments = activity.enrollments.filter(e => e.memberId !== memberId);
      setActivity({ ...activity });
    }

    const handleMoveToParticipants = (memberId: string) => {
      const enrollment = activity.enrollments.find(e => e.memberId === memberId);
      if (enrollment) {
        enrollment.isOnWaitingList = false;
        setActivity({ ...activity });
      }
    };

    return (
      <div className="lg:col-span-1 space-y-6">
        <BorderedTile>
          <div className="flex flex-col gap-4">
            <FormHeader title={`${t("participants")} (${enrollments.length}${activity.participantLimit ? `/${activity.participantLimit}` : ""})`} border />
            <Button variant="primary" onClick={handleDownloadEnrollments}>{t("download_enrollments")}</Button>
            <div className="space-y-4 pr-2">
              {
                enrollments.length > 0 ? (
                enrollments.map((e, index) => (
                  <EditParticipantTile key={index} enrollment={e} onUnenroll={() => handleUnenroll(e.memberId!)} />
                ))
              ) : (
                <p className="text-sm text-gray-400 italic">{t("no_participants_yet")}</p>
              )}
              {
                waitingList.length > 0 && (
                  <EditWaitinglistParticipantTile enrollment={waitingList[0]} onUnenroll={() => handleUnenroll(waitingList[0].memberId!)} onMoveToParticipants={() => handleMoveToParticipants(waitingList[0].memberId!)} />
                )
              }
            </div>

            <Button variant="secondary" className="mt-4" onClick={() => setIsSearchOpen(true)}>
              {t("enroll_member")}
            </Button>
          </div>
        </BorderedTile>

        <Modal 
          isOpen={isSearchOpen} 
          onClose={() => setIsSearchOpen(false)} 
          title={t("enroll_member")}
        >
          <SearchMemberOverlay 
            selectText={t("enroll")}
            onSelect={handleEnroll}
            loading={loading}
          />
        </Modal>
      </div>
    );
}