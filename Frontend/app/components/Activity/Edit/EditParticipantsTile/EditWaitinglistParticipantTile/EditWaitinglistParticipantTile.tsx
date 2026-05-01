import { t } from "i18next";
import { useState } from "react";
import type { EnrollmentResponseDto } from "~/api";
import BorderedTile from "~/components/Tiles/BorderedTile";
import Button from "~/components/UI/Button";
import {
  handleMoveFromWaitinglist,
  handleWaitinglistUnenroll,
} from "./EditWaitinglistParticipantTile.handlers";

/**
 * Renders a tile for managing participants in the waiting list.
 *
 * @param {Object} props - The component's props.
 * @param {EnrollmentResponseDto} props.enrollment - The enrollment to manage.
 * @param {() => void} props.onUnenroll - Callback function to handle unenrollment.
 * @param {() => void} props.onMoveToParticipants - Callback function to move the participant to participants.
 */
export default function EditWaitinglistParticipantTile({
  enrollment,
  onUnenroll,
}: {
  enrollment: EnrollmentResponseDto;
  onUnenroll: () => void;
  onMoveToParticipants: () => void;
}) {
  const [loading, setLoading] = useState(false);

  return (
    <BorderedTile className="bg-gray-50 p-2" noPadding>
      <p className="font-semibold text-sm truncate">
        {enrollment.member.firstName} {enrollment.member.lastName}
      </p>

      <div className="flex flex-col sm:flex-row items-center gap-2 w-full mt-2">
        <Button
          variant="secondary"
          className="flex-1 w-full overflow-hidden"
          onClick={() =>
            handleMoveFromWaitinglist({ enrollment, setLoading, onUnenroll })
          }
          disabled={loading}
        >
          <span className="truncate block w-full px-1">
            {t("move_to_participants")}
          </span>
        </Button>

        <Button
          variant="danger"
          className="flex-1 w-full overflow-hidden"
          onClick={() =>
            handleWaitinglistUnenroll({ enrollment, setLoading, onUnenroll })
          }
          disabled={loading}
        >
          <span className="truncate block w-full px-1">{t("unenroll")}</span>
        </Button>
      </div>
    </BorderedTile>
  );
}
