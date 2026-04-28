import type React from "react";
import { t } from "i18next";
import toast from "react-hot-toast";
import { getApiMailinglists, type Mailinglist } from "~/api";

export const fetchMailingLists = async (
  setLoading: (loading: boolean) => void,
  setMailingLists: React.Dispatch<React.SetStateAction<Mailinglist[]>>
) => {
  try {
    setLoading(true);
    const response = await getApiMailinglists();
    if (response.error || !response.data) throw new Error();
    setMailingLists(response.data);
  } catch (error) {
    toast.error(t("error_fetching_mailing_lists"));
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