import { t } from "i18next";
import { useState, useEffect } from "react";
import { 
  type EnrollmentResponseDto, 
} from "~/api";
import BorderedTile from "~/components/Tiles/BorderedTile";
import Button from "~/components/UI/Button";
import Input from "~/components/UI/Input";
import { handleParticipantUnenroll, handlePriceBlur, handlePriceChange, savePriceToServer } from "./EditParticipantTile.handlers";

export default function EditParticipantTile({ enrollment, onUnenroll }: { enrollment: EnrollmentResponseDto; onUnenroll: () => void }) {
  const [loading, setLoading] = useState(false);
  const [price, setPrice] = useState(enrollment.price ?? 0);
  const [debounceTimeout, setDebounceTimeout] = useState<NodeJS.Timeout | null>(null);

  useEffect(() => {
    setPrice(enrollment.price ?? 0);
  }, [enrollment.price]);

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
               onChange={(e) =>
                 handlePriceChange({
                   e,
                   debounceTimeout,
                   setPrice,
                   setDebounceTimeout,
                   saveAction: (targetPrice) => savePriceToServer({ targetPrice, enrollment, setLoading, setPrice })
                 })
               }
               onBlur={() =>
                 handlePriceBlur({
                   debounceTimeout,
                   price,
                   saveAction: (targetPrice) => savePriceToServer({ targetPrice, enrollment, setLoading, setPrice })
                 })
               }
               disabled={loading}
             />
          </div>
        </div>

        <Button
          variant="danger"
          className="shrink-0 whitespace-nowrap"
          onClick={() => handleParticipantUnenroll({ enrollment, setLoading, onUnenroll })}
          disabled={loading}
        >
          {t("unenroll")}
        </Button>
      </div>
    </BorderedTile>
  );
}
