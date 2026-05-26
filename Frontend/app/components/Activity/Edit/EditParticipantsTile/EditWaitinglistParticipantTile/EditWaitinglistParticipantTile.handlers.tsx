import { t } from "i18next";
import toast from "react-hot-toast";
import {
  deleteEnrollmentsByActivityIdByMemberId,
  type EnrollmentResponseDto,
  patchEnrollmentsByActivityIdByMemberId,
} from "~/api";
import { appendErrorMessage } from "~/util/error.util";

type WaitingListActionArgs = {
  activityId: number;
  enrollment: EnrollmentResponseDto;
  setLoading: (loading: boolean) => void;
  onUnenroll: () => void;
};

/**
 * Handles the unenrollment of a participant from the waiting list.
 *
 * @param {Object} args - The arguments for the function.
 * @param {EnrollmentResponseDto} args.enrollment - The enrollment to be unenrolled.
 * @param {(loading: boolean) => void} args.setLoading - Function to update the loading state.
 * @param {() => void} args.onUnenroll - Callback function to handle unenrollment completion.
 */
export const handleWaitinglistUnenroll = ({
  activityId,
  enrollment,
  setLoading,
  onUnenroll,
}: WaitingListActionArgs) => {
  const handleUnenrollAction = async () => {
    try {
      setLoading(true);
      const response = await deleteEnrollmentsByActivityIdByMemberId({
        path: {
          activityId,
          memberId: enrollment.member.id!,
        },
      });

      if (response.error) {
        throw response.error ?? new Error("Failed to unenroll");
      }

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
    error: (error) =>
      appendErrorMessage(t("failed_to_unenroll_participant"), error),
  });
};

/**
 * Handles the movement of a participant from the waiting list to participants.
 *
 * @param {Object} args - The arguments for the function.
 * @param {EnrollmentResponseDto} args.enrollment - The enrollment to be moved.
 * @param {(loading: boolean) => void} args.setLoading - Function to update the loading state.
 * @param {() => void} args.onUnenroll - Callback function to handle unenrollment completion.
 */
export const handleMoveFromWaitinglist = ({
  activityId,
  enrollment,
  setLoading,
  onUnenroll,
}: WaitingListActionArgs) => {
  const handleMoveToParticipantsAction = async () => {
    try {
      setLoading(true);
      const response = await patchEnrollmentsByActivityIdByMemberId({
        path: {
          activityId,
          memberId: enrollment.member.id!,
        },
        body: [{ op: "replace", path: "/isOnWaitingList", value: false }],
      });

      if (response.error) {
        throw response.error ?? new Error("Failed to move to participants");
      }

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
    error: (error) =>
      appendErrorMessage(
        t("failed_to_move_participant_to_participants"),
        error,
      ),
  });
};
