import type { NavigateFunction } from "react-router";

/**
 * Navigates the user to the announcement creation form.
 *
 * @param {NavigateFunction} navigate - React Router navigation function.
 */
export const handleCreateAnnouncementClick = (navigate: NavigateFunction) => {
  navigate("/announcements/create");
};
