import { t } from "i18next";
import toast from "react-hot-toast";
import { putMembersByIdMailinglists } from "~/api";
import { appendErrorMessage } from "~/util/error.util";

/**
 * Toggles a mailing list's membership in the set of subscribed list ids.
 *
 * @param {string} id - The id of the mailing list.
 * @param {boolean} checked - Whether the checkbox is checked or not.
 * @param {(setter: (prev: Set<string>) => Set<string>) => void} setSubscribedIds - A function to update the subscribed-ids state.
 */
export const handleYearlyMailinglistToggle = (
  id: string,
  checked: boolean,
  setSubscribedIds: (setter: (prev: Set<string>) => Set<string>) => void,
) => {
  setSubscribedIds((prev) => {
    const next = new Set(prev);
    if (checked) {
      next.add(id);
    } else {
      next.delete(id);
    }
    return next;
  });
};

/**
 * Saves the member's mailing list subscriptions, including the yearly-renewal lists,
 * and shows a toast with the result.
 *
 * @param {string} memberId - The ID of the member.
 * @param {Set<string>} subscribedIds - The ids of the mailing lists the member should be subscribed to.
 * @param {(saving: boolean) => void} setSaving - A function to update the saving state.
 */
export const handleSaveYearlyMailinglists = async (
  memberId: string,
  subscribedIds: Set<string>,
  setSaving: (saving: boolean) => void,
) => {
  setSaving(true);

  const saveProcess = async () => {
    try {
      const response = await putMembersByIdMailinglists({
        path: { id: memberId },
        query: { includeYearlyRenewal: true },
        body: Array.from(subscribedIds),
      });

      if (response.error) {
        throw response.error ?? new Error("Failed to save mail subscriptions");
      }
    } finally {
      setSaving(false);
    }
  };

  toast.promise(saveProcess(), {
    loading: t("saving"),
    success: t("mailing_list_preferences_saved"),
    error: (error) =>
      appendErrorMessage(t("error_saving_mailing_list_preferences"), error),
  });
};
