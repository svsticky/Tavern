import { t } from "i18next";
import { toast } from "react-hot-toast";
import type { NavigateFunction } from "react-router";
import { getApiAnnouncements, type GetAnnouncementResponseDto } from "~/api";

type LoadAnnouncementsArgs = {
  initialized: boolean;
  authenticated: boolean | undefined;
  setLoading: (loading: boolean) => void;
  setAnnouncements: (announcements: GetAnnouncementResponseDto[]) => void;
};

export const loadAnnouncements = async ({ initialized, authenticated, setLoading, setAnnouncements }: LoadAnnouncementsArgs) => {
  if (!initialized || !authenticated) return;

  try {
    setLoading(true);
    const announcementsResponse = await getApiAnnouncements();

    if(announcementsResponse.error || !announcementsResponse.data) throw new Error("Failed to load announcements");

    setAnnouncements(announcementsResponse.data as GetAnnouncementResponseDto[]);
  } catch (error) {
    console.error("Error while loading data:", error);
    toast.error(t("loading_failed"));
  } finally {
    setLoading(false);
  }
};

export const handleCreateAnnouncementClick = (navigate: NavigateFunction) => {
  navigate("/announcements/create");
};
