import type React from "react";
import { t } from "i18next";
import toast from "react-hot-toast";
import { deleteApiEnrollmentsByActivityIdByMemberId, patchApiEnrollmentsByActivityIdByMemberId, type EnrollmentResponseDto } from "~/api";

type ParticipantUnenrollArgs = {
  enrollment: EnrollmentResponseDto;
  setLoading: (loading: boolean) => void;
  onUnenroll: () => void;
};

export const handleParticipantUnenroll = ({ enrollment, setLoading, onUnenroll }: ParticipantUnenrollArgs) => {
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

type SavePriceArgs = {
  targetPrice: number;
  enrollment: EnrollmentResponseDto;
  setLoading: (loading: boolean) => void;
  setPrice: (price: number) => void;
};

export const savePriceToServer = async ({ targetPrice, enrollment, setLoading, setPrice }: SavePriceArgs) => {
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

type PriceChangeArgs = {
  e: React.ChangeEvent<HTMLInputElement>;
  debounceTimeout: NodeJS.Timeout | null;
  setPrice: (price: number) => void;
  setDebounceTimeout: (timeout: NodeJS.Timeout) => void;
  saveAction: (price: number) => Promise<void>;
};

export const handlePriceChange = ({ e, debounceTimeout, setPrice, setDebounceTimeout, saveAction }: PriceChangeArgs) => {
  const newPrice = parseFloat(e.target.value) || 0;
  const roundedPrice = Math.round(newPrice * 100) / 100;

  setPrice(newPrice);

  if (debounceTimeout) clearTimeout(debounceTimeout);

  const timeout = setTimeout(() => {
    toast.promise(saveAction(roundedPrice), {
      loading: t("updating_price"),
      success: t("price_updated"),
      error: t("failed_to_update_price"),
    });
  }, 600);

  setDebounceTimeout(timeout);
};

type BlurArgs = {
  debounceTimeout: NodeJS.Timeout | null;
  price: number;
  saveAction: (price: number) => Promise<void>;
};

export const handlePriceBlur = ({ debounceTimeout, price, saveAction }: BlurArgs) => {
  if (debounceTimeout) {
    clearTimeout(debounceTimeout);
    const roundedPrice = Math.round(price * 100) / 100;
    saveAction(roundedPrice);
  }
};
