import { t } from "i18next";
import { useState } from "react";
import type { ActivityResponseDto } from "~/api";
import BorderedTile from "~/components/Tiles/BorderedTile";
import Button from "~/components/UI/Button";
import { FormHeader } from "~/components/UI/Form/FormHeader";
import Modal from "~/components/UI/Modal/Modal";
import SearchMemberOverlay from "../../../Member/SearchMemberOverlay";
import {
  handleDownloadEnrollments,
  handleEnrollParticipant,
  handleMoveToParticipants,
  handleUnenrollParticipant,
} from "./EditParticipantsTile.handlers";
import EditParticipantTile from "./EditParticipantTile/EditParticipantTile";
import EditWaitinglistParticipantTile from "./EditWaitinglistParticipantTile/EditWaitinglistParticipantTile";

/**
 * An administrative tile component for managing the participants and waiting list
 * of a specific activity.
 *
 * Features:
 * - **Participant Overview**: Displays a list of enrolled members with an optional
 *   capacity counter (e.g., 15/20).
 * - **Waiting List Promotion**: Identifies the next person on the waiting list and
 *   provides actions to promote them to the main participant list.
 * - **Member Search & Manual Enrollment**: Opens a modal containing a `SearchMemberOverlay`
 *   to find and manually register association members.
 * - **Data Export**: Provides a trigger to download the full enrollment list as a CSV.
 * - **Real-time State Updates**: Coordinates with handler functions to update the
 *   local `activity` state without requiring a full page refresh.
 *
 * @component
 * @param {Object} props - The component props.
 * @param {ActivityResponseDto} props.activity - The current activity data containing the enrollment list.
 * @param {React.Dispatch<React.SetStateAction<ActivityResponseDto | null>>} props.setActivity - State setter to update participant/waiting list data locally.
 *
 * @example
 * ```tsx
 * <EditParticipantsTile
 *   activity={currentActivity}
 *   setActivity={setActivity}
 * />
 * ```
 */
export default function EditParticipantsTile({
  activity,
  setActivity,
}: {
  activity: ActivityResponseDto;
  setActivity: React.Dispatch<React.SetStateAction<ActivityResponseDto | null>>;
}) {
  const [isSearchOpen, setIsSearchOpen] = useState(false);
  const [loading, setLoading] = useState(false);

  const enrollments =
    activity.enrollments.filter((e) => !e.isOnWaitingList) ?? [];
  // Earliest-registered first, so waitingList[0] is genuinely next in line to be promoted.
  const waitingList = (
    activity.enrollments.filter((e) => e.isOnWaitingList) ?? []
  )
    .slice()
    .sort(
      (a, b) =>
        new Date(a.registeredOn).getTime() - new Date(b.registeredOn).getTime(),
    );

  return (
    <div className="lg:col-span-1 space-y-6">
      <BorderedTile>
        <div className="flex flex-col gap-4">
          <FormHeader
            title={`${t("participants")} (${enrollments.length}${activity.participantLimit ? `/${activity.participantLimit}` : ""})`}
            border
          />
          <Button
            variant="primary"
            onClick={() => handleDownloadEnrollments(activity)}
          >
            {t("download_enrollments")}
          </Button>
          <div className="space-y-4 pr-2">
            {enrollments.length > 0 ? (
              enrollments.map((e, index) => (
                <EditParticipantTile
                  key={index}
                  activityId={activity.id}
                  enrollment={e}
                  onUnenroll={() =>
                    handleUnenrollParticipant(
                      e.member.id!,
                      activity,
                      setActivity,
                    )
                  }
                />
              ))
            ) : (
              <p className="text-sm text-gray-400 italic">
                {t("no_participants_yet")}
              </p>
            )}

            {waitingList.length > 0 && (
              <>
                <FormHeader
                  title={`${t("waiting_list")} (${waitingList.length})`}
                  border
                />
                <EditWaitinglistParticipantTile
                  activityId={activity.id}
                  enrollment={waitingList[0]}
                  onUnenroll={() =>
                    handleUnenrollParticipant(
                      waitingList[0].member.id!,
                      activity,
                      setActivity,
                    )
                  }
                  onMoveToParticipants={() =>
                    handleMoveToParticipants(
                      waitingList[0].member.id!,
                      activity,
                      setActivity,
                    )
                  }
                />
              </>
            )}
          </div>

          <Button
            variant="secondary"
            className="mt-4"
            onClick={() => setIsSearchOpen(true)}
          >
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
          onSelect={(member) =>
            handleEnrollParticipant({
              member,
              activity,
              setActivity,
              setLoading,
              setIsSearchOpen,
            })
          }
          loading={loading}
        />
      </Modal>
    </div>
  );
}
