import { t } from "i18next";
import { useState, useEffect } from "react";
import toast from "react-hot-toast";
import { 
  deleteApiEnrollmentsByActivityIdByMemberId, 
  patchApiEnrollmentsByActivityIdByMemberId,
  type EnrollmentResponseDto, 
} from "~/api";
import BorderedTile from "~/components/Tiles/BorderedTile";
import Button from "~/components/UI/Button";

export default function EditWaitinglistParticipantTile({ enrollment, onUnenroll, onMoveToParticipants }: { enrollment: EnrollmentResponseDto; onUnenroll: () => void; onMoveToParticipants: () => void }) {
  const [loading, setLoading] = useState(false);

  const handleUnenroll = () => {
    const handleUnenrollAction = async () => {
      try {
        setLoading(true);
        const response = await deleteApiEnrollmentsByActivityIdByMemberId({
          path: { ActivityId: enrollment.activity.id, MemberId: enrollment.member.id! },
        });

        if (response.error) throw new Error("Failed to unenroll");

        onUnenroll();
      } catch (error) {
        console.error("Error unenrolling participant:", error);
        throw error;
      } finally {
        setLoading(false);
      }
    };

    toast.promise(handleUnenrollAction(), {
      loading: t("unenrolling_participant"),
      success: t("participant_unenrolled"),
      error: t("failed_to_unenroll_participant"),
    });
  };

  const handleMoveToParticipants = () => {
    const handleMoveToParticipantsAction = async () => {
      try {
        setLoading(true);
        const response = await patchApiEnrollmentsByActivityIdByMemberId({
          path: { ActivityId: enrollment.activity.id!, MemberId: enrollment.member.id! },
          body: [
            { op: "replace", path: "/isOnWaitingList", value: false }
           ]
        });

        if (response.error) throw new Error("Failed to move to participants");

        onUnenroll();
      } catch (error) {
        console.error("Error moving participant to participants:", error);
        throw error;
      } finally {
        setLoading(false);
      }
    };

    toast.promise(handleMoveToParticipantsAction(), {
      loading: t("moving_participant_to_participants"),
      success: t("participant_moved_to_participants"),
      error: t("failed_to_move_participant_to_participants"),
    });
  };

  return (
    <BorderedTile className="bg-gray-50 p-2" noPadding>
        <p className="font-semibold text-sm truncate">
            {enrollment.member.firstName} {enrollment.member.lastName}
        </p>

        <div className="flex flex-col sm:flex-row items-center gap-2 w-full mt-2">

            <Button variant="secondary" className="flex-1 w-full overflow-hidden" onClick={handleMoveToParticipants} disabled={loading}>
                <span className="truncate block w-full px-1">
                    {t("move_to_participants")}
                </span>
            </Button>
            
            <Button variant="danger" className="flex-1 w-full overflow-hidden" onClick={handleUnenroll} disabled={loading}>
                <span className="truncate block w-full px-1">
                    {t("unenroll")}
                </span>
            </Button>
            
        </div>
    </BorderedTile>
  );
}