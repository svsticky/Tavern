import { t } from "i18next";
import type React from "react";
import toast from "react-hot-toast";
import {
  deleteMailinglistsById,
  type Mailinglist,
  type PostMailinglistDto,
  postMailinglists,
  putMailinglistsById,
} from "~/api";
import { appendErrorMessage } from "~/util/error.util";

type HandleSubmitArgs = {
  e: React.FormEvent;
  formData: Omit<Mailinglist, "id">;
  mailingList?: Mailinglist;
  setLoading: (loading: boolean) => void;
  onComplete: (list?: Mailinglist) => void;
};
export const handleMailingListSubmit = async ({
  e,
  formData,
  mailingList,
  setLoading,
  onComplete,
}: HandleSubmitArgs) => {
  e.preventDefault();
  setLoading(true);

  try {
    if (mailingList?.id) {
      const response = await putMailinglistsById({
        path: { id: mailingList.id },
        body: formData as PostMailinglistDto,
      });

      if (response.error) {
        throw response.error;
      }

      const updatedList: Mailinglist = {
        ...formData,
        id: mailingList.id,
      };

      toast.success(t("mailing_list_updated"));
      onComplete(updatedList);
    } else {
      const response = await postMailinglists({
        body: formData as PostMailinglistDto,
      });

      if (response.error || !response.data) {
        throw response.error ?? new Error("Failed to create mailing list");
      }

      const data = response.data as any;
      const newList: Mailinglist = {
        ...formData,
        id: data.id,
        bitValue: data.bitValue,
      };

      console.log("Created mailing list:", newList);

      toast.success(t("mailing_list_created"));
      onComplete(newList);
    }
  } catch (error) {
    toast.error(appendErrorMessage(t("error_saving_mailing_list"), error));
  } finally {
    setLoading(false);
  }
};

type HandleDeleteArgs = {
  mailingList: Mailinglist;
  setLoading: (loading: boolean) => void;
  onComplete: (list?: Mailinglist) => void;
};

export const handleMailingListDelete = async ({
  mailingList,
  setLoading,
  onComplete,
}: HandleDeleteArgs) => {
  setLoading(true);
  try {
    const response = await deleteMailinglistsById({
      path: { id: mailingList.id! },
    });

    if (response.error) {
      throw response.error;
    }

    toast.success(t("mailing_list_deleted"));
    onComplete(undefined);
  } catch (error) {
    toast.error(appendErrorMessage(t("error_deleting_mailing_list"), error));
  } finally {
    setLoading(false);
  }
};
