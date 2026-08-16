import { t } from "i18next";
import type React from "react";
import toast from "react-hot-toast";
import {
  deleteEnrollmentsByActivityIdByMemberId,
  type EnrollmentResponseDto,
  patchEnrollmentsByActivityIdByMemberId,
} from "~/api";
import { appendErrorMessage } from "~/util/error.util";

/**
 * Handles the administrative removal of a participant from an activity.
 *
 * Triggers a toast notification with a promise that resolves when the unenrollment
 * API call is complete.
 *
 * @param args - The configuration object.
 * @param args.enrollment - The enrollment record containing activity and member identification.
 * @param args.setLoading - Function to toggle the UI loading state.
 * @param args.onUnenroll - Callback executed after a successful server-side unenrollment to refresh local state.
 */
export const handleParticipantUnenroll = ({
  activityId,
  enrollment,
  setLoading,
  onUnenroll,
}: {
  activityId: number;
  enrollment: EnrollmentResponseDto;
  setLoading: (loading: boolean) => void;
  onUnenroll: () => void;
}) => {
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
 * Persists a price change to the server using a JSON Patch request.
 * If the update fails, the local price state is reverted to the last known server value.
 *
 * @async
 * @param args - Configuration for the patch operation.
 * @throws {Error} Throws if the API response contains an error.
 */
export const savePriceToServer = async ({
  activityId,
  targetPrice,
  enrollment,
  setLoading,
  setPrice,
}: {
  activityId: number;
  targetPrice: number;
  enrollment: EnrollmentResponseDto;
  setLoading: (loading: boolean) => void;
  setPrice: (price: number) => void;
}) => {
  if (targetPrice === enrollment.price) return;

  try {
    setLoading(true);
    const response = await patchEnrollmentsByActivityIdByMemberId({
      path: {
        activityId,
        memberId: enrollment.member.id!,
      },
      body: [
        {
          op: "replace",
          path: "/price",
          value: targetPrice,
        },
      ],
    });

    if (response.error) {
      throw response.error ?? new Error("Update failed");
    }

    enrollment.price = targetPrice;
  } catch (error) {
    setPrice(enrollment.price ?? 0);
    throw error;
  } finally {
    setLoading(false);
  }
};

/**
 * Handles the `onChange` event for a price input field, implementing a 600ms debounce.
 *
 * This ensures that the server is only updated once the user has finished typing,
 * reducing API traffic and unnecessary toast notifications.
 *
 * @param args - Configuration including event, current timeout, and state setters.
 */
export const handlePriceChange = ({
  e,
  debounceTimeout,
  setPrice,
  setDebounceTimeout,
  saveAction,
}: {
  e: React.ChangeEvent<HTMLInputElement>;
  debounceTimeout: NodeJS.Timeout | null;
  setPrice: (price: number) => void;
  setDebounceTimeout: (timeout: NodeJS.Timeout) => void;
  saveAction: (price: number) => Promise<void>;
}) => {
  const newPrice = parseFloat(e.target.value) || 0;
  const roundedPrice = Math.round(newPrice * 100) / 100;

  setPrice(newPrice);

  if (debounceTimeout) clearTimeout(debounceTimeout);

  const timeout = setTimeout(() => {
    toast.promise(saveAction(roundedPrice), {
      loading: t("updating_price"),
      success: t("price_updated"),
      error: (error) => appendErrorMessage(t("failed_to_update_price"), error),
    });
  }, 600);

  setDebounceTimeout(timeout);
};

type BlurArgs = {
  debounceTimeout: NodeJS.Timeout | null;
  price: number;
  saveAction: (price: number) => Promise<void>;
};

export const handlePriceBlur = ({
  debounceTimeout,
  price,
  saveAction,
}: BlurArgs) => {
  if (debounceTimeout) {
    clearTimeout(debounceTimeout);
    const roundedPrice = Math.round(price * 100) / 100;
    saveAction(roundedPrice);
  }
};
