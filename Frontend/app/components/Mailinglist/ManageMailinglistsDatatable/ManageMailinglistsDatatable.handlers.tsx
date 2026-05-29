import type React from "react";
import { t } from "i18next";
import toast from "react-hot-toast";
import { getMailinglists, type Mailinglist } from "~/api";
import { appendErrorMessage } from "~/util/error.util";

export const fetchMailingLists = async (
  setLoading: (loading: boolean) => void,
  setMailingLists: React.Dispatch<React.SetStateAction<Mailinglist[]>>
) => {
  try {
    setLoading(true);
    const response = await getMailinglists();
    if (response.error || !response.data) {
      throw response.error ?? new Error("Failed to fetch mailing lists");
    }
    setMailingLists(response.data);
  } catch (error) {
    toast.error(appendErrorMessage(t("error_fetching_mailing_lists"), error));
  } finally {
    setLoading(false);
  }
};

type HandleMailingListEditedArgs = {
  list?: Mailinglist;
  editedList?: Mailinglist;
  setMailingLists: React.Dispatch<React.SetStateAction<Mailinglist[]>>;
  setIsEditModalOpen: (open: boolean) => void;
  setEditedList: (list: Mailinglist | undefined) => void;
};

export const handleMailingListEdited = ({ 
  list, 
  editedList, 
  setMailingLists, 
  setIsEditModalOpen, 
  setEditedList 
}: HandleMailingListEditedArgs) => {
  if (!list) {
    if (editedList) {
      setMailingLists((prev) => prev.filter((l) => l.id !== editedList.id));
    }
  } 
  else if (editedList) {
    setMailingLists((prev) => prev.map((l) => (l.id === list.id ? list : l)));
  } 
  else {
    setMailingLists((prev) => [...prev, list]);
  }

  setIsEditModalOpen(false);
  setEditedList(undefined);
};