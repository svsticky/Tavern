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
import Input from "~/components/UI/Input";

export default function EditParticipantTile({ enrollment, onUnenroll }: { enrollment: EnrollmentResponseDto; onUnenroll: () => void }) {
  const [loading, setLoading] = useState(false);
  const [price, setPrice] = useState(enrollment.price ?? 0);
  const [debounceTimeout, setDebounceTimeout] = useState<NodeJS.Timeout | null>(null);

  useEffect(() => {
    setPrice(enrollment.price ?? 0);
  }, [enrollment.price]);

  const handleUnenroll = () => {
    const handleUnenrollAction = async () => {
      try {
        setLoading(true);
        const response = await deleteApiEnrollmentsByActivityIdByMemberId({
          path: { activityId: `$${enrollment.activity.id}`, memberId: enrollment.member.id! },
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

  const savePriceToServer = async (targetPrice: number) => {
    if (targetPrice === enrollment.price) return;

    try {
      setLoading(true);
      const response = await patchApiEnrollmentsByActivityIdByMemberId({
        path: {
          ActivityId: enrollment.activity.id,
          MemberId: enrollment.member.id!,
        },
        body: [
          {
            op: "replace",
            path: "/price",
            value: targetPrice,
          },
        ],
      });

      if (response.error) throw new Error("Update failed");

      enrollment.price = targetPrice;
    } catch (error) {
      setPrice(enrollment.price ?? 0);
      throw error;
    } finally {
      setLoading(false);
    }
  };

  const onPriceChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const newPrice = parseFloat(e.target.value) || 0;
    const roundedPrice = Math.round(newPrice * 100) / 100;
    
    setPrice(newPrice);

    if (debounceTimeout) clearTimeout(debounceTimeout);
    
    const timeout = setTimeout(() => {
      toast.promise(savePriceToServer(roundedPrice), {
        loading: t("updating_price"),
        success: t("price_updated"),
        error: t("failed_to_update_price"),
      });
    }, 600);

    setDebounceTimeout(timeout);
  };

  const handleBlur = () => {
    if (debounceTimeout) {
      clearTimeout(debounceTimeout);
      const roundedPrice = Math.round(price * 100) / 100;
      savePriceToServer(roundedPrice);
    }
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
              type="number"
              step="0.01"
              value={price === 0 ? "" : price}
              placeholder="0.00"
              className="h-8 text-sm text-right px-2 w-full"
              onChange={onPriceChange}
              onBlur={handleBlur}
              disabled={loading}
            />
          </div>
        </div>

        <Button
          variant="danger"
          className="shrink-0 whitespace-nowrap"
          onClick={handleUnenroll}
          disabled={loading}
        >
          {t("unenroll")}
        </Button>
      </div>
    </BorderedTile>
  );
}