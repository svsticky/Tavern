import { t } from "i18next";
import toast from "react-hot-toast";
import {
  getMembersByIdMailinglists,
  type MemberMailinglistDto,
  putMembersByIdMailinglists,
} from "~/api";
import { appendErrorMessage } from "~/util/error.util";

/**
 * Fetches the yearly-renewal mailing list context for a member (General lists plus
 * YearlyRenewalOnly ones, e.g. "alumni") - not just the everyday General set.
 */
export const fetchYearlyMailinglists = async (
  memberId: string,
  setLoading: (loading: boolean) => void,
  setMailingLists: (lists: MemberMailinglistDto[]) => void,
  setSubscribedIds: (ids: Set<string>) => void,
  setUnavailable: (unavailable: boolean) => void,
) => {
  try {
    setLoading(true);
    const response = await getMembersByIdMailinglists({
      path: { id: memberId },
      query: { includeYearlyRenewal: true },
    });

    if (response.error || !response.data) {
      throw response.error ?? new Error("Failed to fetch mailing lists");
    }

    setMailingLists(response.data);
    setSubscribedIds(
      new Set(
        response.data.filter((list) => list.subscribed).map((list) => list.id!),
      ),
    );
    setUnavailable(false);
  } catch (error) {
    console.error("Error fetching mailing lists:", error);
    toast.error(appendErrorMessage(t("fetch_mailinglists_failed"), error));
    setMailingLists([]);
    setUnavailable(true);
  } finally {
    setLoading(false);
  }
};

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
