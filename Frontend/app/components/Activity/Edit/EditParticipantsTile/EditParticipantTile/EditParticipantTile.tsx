import { t } from "i18next";
import { useEffect, useState } from "react";
import toast from "react-hot-toast";
import type { EnrollmentResponseDto } from "~/api";
import BorderedTile from "~/components/Tiles/BorderedTile";
import Button from "~/components/UI/Button";
import Input from "~/components/UI/Input";
import { appendErrorMessage } from "~/util/error.util";
import {
  handleParticipantUnenroll,
  savePriceToServer,
} from "./EditParticipantTile.handlers";

/**
 * An administrative tile component for managing an individual participant's enrollment.
 *
 * Features:
 * - **Price Management**: Provides a controlled input to adjust the price for a specific enrollment.
 *   Saves once editing is finished (`onBlur` or Enter), while keeping typing unformatted.
 * - **Unenrollment**: Allows administrators to remove a participant with a single action,
 *   guarded by a loading state and toast notifications.
 * - **State Syncing**: Automatically updates the local price state if the enrollment prop changes
 *   externally (e.g., after a list refresh).
 *
 * @component
 * @param {Object} props - The component props.
 * @param {EnrollmentResponseDto} props.enrollment - The enrollment data for the specific member.
 * @param {() => void} props.onUnenroll - Callback executed after a successful unenrollment to update the parent list.
 *
 * @example
 * ```tsx
 * <EditParticipantTile
 *   enrollment={participantData}
 *   onUnenroll={() => refreshParticipantList()}
 * />
 * ```
 */
export default function EditParticipantTile({
  activityId,
  enrollment,
  onUnenroll,
}: {
  activityId: number;
  enrollment: EnrollmentResponseDto;
  onUnenroll: () => void;
}) {
  const [loading, setLoading] = useState(false);
  const [priceInput, setPriceInput] = useState(
    enrollment.price == null ? "" : enrollment.price.toFixed(2),
  );

  useEffect(() => {
    setPriceInput(enrollment.price == null ? "" : enrollment.price.toFixed(2));
  }, [enrollment.price]);

  const savePrice = async () => {
    const normalizedInput = priceInput.trim().replace(",", ".");
    const parsedPrice = normalizedInput === "" ? 0 : Number(normalizedInput);
    if (Number.isNaN(parsedPrice)) {
      setPriceInput(
        enrollment.price == null ? "" : enrollment.price.toFixed(2),
      );
      return;
    }

    const roundedPrice = Math.round(parsedPrice * 100) / 100;
    setPriceInput(roundedPrice === 0 ? "" : roundedPrice.toFixed(2));

    await toast.promise(
      savePriceToServer({
        activityId,
        targetPrice: roundedPrice,
        enrollment,
        setLoading,
        setPrice: (price) => setPriceInput(price === 0 ? "" : price.toFixed(2)),
      }),
      {
        loading: t("updating_price"),
        success: t("price_updated"),
        error: (error) =>
          appendErrorMessage(t("failed_to_update_price"), error),
      },
    );
  };

  return (
    <BorderedTile className="bg-gray-50 p-2" noPadding>
      <p className="font-semibold text-sm truncate">
        {enrollment.member.firstName} {enrollment.member.lastName}
      </p>

      <div className="flex flex-col sm:flex-row sm:items-center gap-2 w-full justify-between mt-2">
        <div className="flex items-center gap-2 flex-1 min-w-0">
          <span className="text-sm text-gray-500 shrink-0">€</span>
          <div className="flex-1 min-w-0">
            <Input
              min="0"
              type="text"
              step="0.01"
              value={priceInput}
              placeholder="0.00"
              className="h-8 bg-white text-sm text-right px-2 w-full"
              onChange={(e) => setPriceInput(e.target.value)}
              onBlur={() => {
                void savePrice();
              }}
              onKeyDown={(e) => {
                if (e.key !== "Enter") return;
                e.preventDefault();
                void savePrice();
                (e.target as HTMLInputElement).blur();
              }}
              inputMode="decimal"
              pattern="^[0-9]*[.,]?[0-9]*$"
              disabled={loading}
            />
          </div>
        </div>

        <Button
          variant="danger"
          className="shrink-0 whitespace-nowrap"
          onClick={() =>
            handleParticipantUnenroll({
              activityId,
              enrollment,
              setLoading,
              onUnenroll,
            })
          }
          disabled={loading}
        >
          {t("unenroll")}
        </Button>
      </div>
    </BorderedTile>
  );
}
