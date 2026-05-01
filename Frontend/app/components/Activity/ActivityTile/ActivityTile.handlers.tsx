import type React from "react";
import type { NavigateFunction } from "react-router";
import type { ActivityResponseDto } from "~/api";

/**
 * Event handler to navigate a user to the edit page of a specific activity.
 *
 * This function explicitly calls `e.preventDefault()` and `e.stopPropagation()`.
 * This is essential for scenarios where an "Edit" button is nested inside a
 * clickable Activity Card or list item, as it prevents the parent's click
 * handler from being triggered.
 *
 * @function
 * @param {React.MouseEvent} e - The click event object from the React component.
 * @param {NavigateFunction} navigate - The navigation function from `react-router` (usually from `useNavigate`).
 * @param {ActivityResponseDto} activity - The activity object containing the unique identifier for the route.
 *
 * @example
 * ```tsx
 * const navigate = useNavigate();
 * // ...
 * <button onClick={(e) => handleEditClick(e, navigate, activity)}>
 *   {t("edit")}
 * </button>
 * ```
 */
export const handleEditClick = (
  e: React.MouseEvent,
  navigate: NavigateFunction,
  activity: ActivityResponseDto,
) => {
  e.preventDefault();
  e.stopPropagation();
  navigate(`/activities/edit/${activity.id}`);
};
