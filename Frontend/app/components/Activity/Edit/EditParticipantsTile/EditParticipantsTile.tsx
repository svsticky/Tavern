import { t } from "i18next";
import { type ActivityResponseDto } from "~/api";
import BorderedTile from "~/components/Tiles/BorderedTile";
import Button from "~/components/UI/Button";
import { FormHeader } from "~/components/UI/Form/FormHeader";
import EditParticipantTile from "./EditParticipantTile/EditParticipantTile";
import EditWaitinglistParticipantTile from "./EditWaitinglistParticipantTile/EditWaitinglistParticipantTile";
import Modal from "~/components/UI/Modal/Modal";
import { useState } from "react";
import SearchMemberOverlay from "../../../Member/SearchMemberOverlay";
import { handleDownloadEnrollments, handleEnrollParticipant, handleMoveToParticipants, handleUnenrollParticipant } from "./EditParticipantsTile.handlers";

export default function EditParticipantsTile({activity, setActivity}: {activity: ActivityResponseDto; setActivity: React.Dispatch<React.SetStateAction<ActivityResponseDto | null>>}) {
    const [isSearchOpen, setIsSearchOpen] = useState(false);
    const [loading, setLoading] = useState(false);
  
    const enrollments = activity.enrollments.filter(e => !e.isOnWaitingList) ?? [];
    const waitingList = activity.enrollments.filter(e => e.isOnWaitingList) ?? [];

    return (
      <div className="lg:col-span-1 space-y-6">
        <BorderedTile>
          <div className="flex flex-col gap-4">
            <FormHeader title={`${t("participants")} (${enrollments.length}${activity.participantLimit ? `/${activity.participantLimit}` : ""})`} border />
            <Button variant="primary" onClick={() => handleDownloadEnrollments(activity)}>{t("download_enrollments")}</Button>
            <div className="space-y-4 pr-2">
              {
                enrollments.length > 0 ? (
                enrollments.map((e, index) => (
                  <EditParticipantTile
                    key={index}
                    enrollment={e}
                    onUnenroll={() => handleUnenrollParticipant(e.member.id!, activity, setActivity)}
                  />
                ))
              ) : (
                <p className="text-sm text-gray-400 italic">{t("no_participants_yet")}</p>
              )}
              {
                waitingList.length > 0 && (
                  <EditWaitinglistParticipantTile
                    enrollment={waitingList[0]}
                    onUnenroll={() => handleUnenrollParticipant(waitingList[0].member.id!, activity, setActivity)}
                    onMoveToParticipants={() => handleMoveToParticipants(waitingList[0].member.id!, activity, setActivity)}
                  />
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
              onSelect={(member) =>
                handleEnrollParticipant({
                  member,
                  activity,
                  setActivity,
                  setLoading,
                  setIsSearchOpen
                })
              }
              loading={loading}
            />
        </Modal>
      </div>
    );
}
