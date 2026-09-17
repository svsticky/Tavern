import type { NavigateFunction } from "react-router";

/**
 * Whether there's an in-app history entry to go back to - React Router
 * stores { idx } on history.state, and idx > 0 means this isn't the first
 * entry (a fresh/direct page load).
 */
export function hasInAppHistory(): boolean {
  const idx = (window.history.state as { idx?: number } | null)?.idx;
  return typeof idx === "number" && idx > 0;
}

/**
 * Redirects after a save/delete whose target URL already exists earlier in
 * history - goes back to it instead of replacing the current entry, which
 * would duplicate that URL and make the next "back" press a no-op. Falls
 * back to a normal replace when there's nothing to go back to.
 */
export function navigateBackOrReplace(
  navigate: NavigateFunction,
  fallbackPath: string,
) {
  if (hasInAppHistory()) {
    navigate(-1);
  } else {
    navigate(fallbackPath, { replace: true });
  }
}
