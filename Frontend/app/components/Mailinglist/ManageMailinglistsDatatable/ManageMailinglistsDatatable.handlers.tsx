import { t } from "i18next";
import type React from "react";
import toast from "react-hot-toast";
import { type CuratedMailinglistDto, getMailinglistsCurated } from "~/api";
import { appendErrorMessage } from "~/util/error.util";

export const fetchCuratedMailinglists = async (
  setLoading: (loading: boolean) => void,
  setCuratedLists: React.Dispatch<
    React.SetStateAction<CuratedMailinglistDto[]>
  >,
) => {
  try {
    setLoading(true);
    const response = await getMailinglistsCurated();
    if (response.error || !response.data) {
      throw (
        response.error ?? new Error("Failed to fetch curated mailing lists")
      );
    }
    setCuratedLists(response.data);
  } catch (error) {
    toast.error(appendErrorMessage(t("fetch_mailinglists_failed"), error));
  } finally {
    setLoading(false);
  }
};

type HandleMailinglistEditedArgs = {
  list?: CuratedMailinglistDto;
  editedList?: CuratedMailinglistDto;
  setCuratedLists: React.Dispatch<
    React.SetStateAction<CuratedMailinglistDto[]>
  >;
  setIsEditModalOpen: (open: boolean) => void;
  setEditedList: (list: CuratedMailinglistDto | undefined) => void;
};

export const handleMailinglistEdited = ({
  list,
  editedList,
  setCuratedLists,
  setIsEditModalOpen,
  setEditedList,
}: HandleMailinglistEditedArgs) => {
  if (!list) {
    if (editedList) {
      setCuratedLists((prev) => prev.filter((l) => l.id !== editedList.id));
    }
  } else if (editedList) {
    setCuratedLists((prev) => prev.map((l) => (l.id === list.id ? list : l)));
  } else {
    setCuratedLists((prev) => [...prev, list]);
  }

  setIsEditModalOpen(false);
  setEditedList(undefined);
};
