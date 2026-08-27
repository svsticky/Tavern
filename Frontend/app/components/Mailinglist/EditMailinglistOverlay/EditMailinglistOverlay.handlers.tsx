import { t } from "i18next";
import type React from "react";
import toast from "react-hot-toast";
import {
  type CuratedMailinglistDto,
  deleteMailinglistsById,
  getMailinglistsAddable,
  type MailinglistDto,
  type MailinglistVisibility,
  patchMailinglistsById,
  postMailinglists,
} from "~/api";
import { appendErrorMessage } from "~/util/error.util";

/**
 * Fetches the provider's lists that haven't been curated yet, for the "add mailing list" picker.
 */
export const fetchAddableMailinglists = async (
  setLoading: (loading: boolean) => void,
  setAddableLists: React.Dispatch<React.SetStateAction<MailinglistDto[]>>,
) => {
  try {
    setLoading(true);
    const response = await getMailinglistsAddable();
    if (response.error || !response.data) {
      throw (
        response.error ?? new Error("Failed to fetch addable mailing lists")
      );
    }
    setAddableLists(response.data);
  } catch (error) {
    toast.error(appendErrorMessage(t("fetch_mailinglists_failed"), error));
  } finally {
    setLoading(false);
  }
};

type SubmitArgs = {
  e: React.FormEvent;
  curatedList?: CuratedMailinglistDto;
  providerListId: string;
  visibility: MailinglistVisibility;
  setLoading: (loading: boolean) => void;
  onComplete: (list?: CuratedMailinglistDto) => void;
};

/**
 * Handles adding a new curated mailing list, or updating an existing one's visibility.
 * The underlying provider list can't be repointed once curated - only Visibility is editable.
 */
export const handleMailinglistSubmit = async ({
  e,
  curatedList,
  providerListId,
  visibility,
  setLoading,
  onComplete,
}: SubmitArgs) => {
  e.preventDefault();

  const submitProcess = async () => {
    setLoading(true);
    try {
      if (curatedList?.id != null) {
        const response = await patchMailinglistsById({
          path: { id: curatedList.id },
          body: { visibility },
        });

        if (response.error) {
          throw response.error ?? new Error("Failed to update mailing list");
        }

        onComplete({ ...curatedList, visibility });
      } else {
        const response = await postMailinglists({
          body: { providerListId, visibility },
        });

        if (response.error || !response.data) {
          throw response.error ?? new Error("Failed to add mailing list");
        }

        onComplete(response.data);
      }
    } finally {
      setLoading(false);
    }
  };

  toast.promise(submitProcess(), {
    loading: t("saving"),
    success: curatedList ? t("mailing_list_updated") : t("mailing_list_added"),
    error: (error) =>
      appendErrorMessage(
        curatedList
          ? t("error_updating_mailing_list")
          : t("error_adding_mailing_list"),
        error,
      ),
  });
};

type DeleteArgs = {
  curatedList?: CuratedMailinglistDto;
  setLoading: (loading: boolean) => void;
  onComplete: (list?: CuratedMailinglistDto) => void;
  confirm: (message: string) => Promise<boolean>;
};

export const handleMailinglistDelete = async ({
  curatedList,
  setLoading,
  onComplete,
  confirm,
}: DeleteArgs) => {
  if (!curatedList?.id) return;

  if (!(await confirm(t("delete_mailing_list_confirmation")))) {
    return;
  }

  const deleteProcess = async () => {
    setLoading(true);
    try {
      const response = await deleteMailinglistsById({
        path: { id: curatedList.id! },
      });

      if (response.error) {
        throw response.error ?? new Error("Failed to delete mailing list");
      }

      onComplete(undefined);
    } finally {
      setLoading(false);
    }
  };

  toast.promise(deleteProcess(), {
    loading: t("deleting"),
    success: t("mailing_list_deleted"),
    error: (error) =>
      appendErrorMessage(t("error_deleting_mailing_list"), error),
  });
};
