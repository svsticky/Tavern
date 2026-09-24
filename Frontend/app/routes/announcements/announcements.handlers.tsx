import type { NavigateFunction } from "react-router";

// Shared with the home dashboard, so served from the resource cache when fresh.
export { loadAnnouncements } from "~/util/cachedResources.util";

/**
 * Navigates the user to the announcement creation form.
 *
 * @param {NavigateFunction} navigate - React Router navigation function.
 */
export const handleCreateAnnouncementClick = (navigate: NavigateFunction) => {
  navigate("/announcements/create");
};
