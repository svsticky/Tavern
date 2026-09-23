import type { NavigateFunction } from "react-router";
import { type GetAnnouncementResponseDto, getAnnouncements } from "~/api";

/**
 * Fetches the list of all announcements from the API, for use in the
 * route's `clientLoader`. Throws on failure so React Router's error
 * boundary handles it.
 */
export const loadAnnouncements = async (): Promise<
  GetAnnouncementResponseDto[]
> => {
  const announcementsResponse = await getAnnouncements();

  if (announcementsResponse.error || !announcementsResponse.data)
    throw new Error("Failed to load announcements");

  return announcementsResponse.data as GetAnnouncementResponseDto[];
};

/**
 * Navigates the user to the announcement creation form.
 *
 * @param {NavigateFunction} navigate - React Router navigation function.
 */
export const handleCreateAnnouncementClick = (navigate: NavigateFunction) => {
  navigate("/announcements/create");
};
