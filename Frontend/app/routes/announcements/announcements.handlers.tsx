import { t } from "i18next";
import { toast } from "react-hot-toast";
import type { NavigateFunction } from "react-router";
import { getApiAnnouncements, type GetAnnouncementResponseDto } from "~/api";

/**
 * Arguments for the loadAnnouncements handler.
 */
type LoadAnnouncementsArgs = {
  initialized: boolean;
  authenticated: boolean | undefined;
  setLoading: (loading: boolean) => void;
  setAnnouncements: (announcements: GetAnnouncementResponseDto[]) => void;
};

/**
 * Fetches the list of all announcements from the API.
 * 
 * This handler ensures that requests are only made when the application is
 * properly initialized and the user is authenticated. It handles the loading
 * state and provides visual feedback via toasts if an error occurs.
 * 
 * @async
 * @param {LoadAnnouncementsArgs} args - Configuration and state setter functions.
 */
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

/**
 * Navigates the user to the announcement creation form.
 * 
 * @param {NavigateFunction} navigate - React Router navigation function.
 */
export const handleCreateAnnouncementClick = (navigate: NavigateFunction) => {
  navigate("/announcements/create");
};
