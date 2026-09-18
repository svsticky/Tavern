import type { NavigateFunction } from "react-router";

/**
 * Whether there's an in-app history entry to go back to - React Router
 * stores { idx } on history.state for every entry it creates, and idx > 0
 * means this isn't the first one (a fresh/direct page load).
 */
export function hasInAppHistory(): boolean {
  const idx = (window.history.state as { idx?: number } | null)?.idx;
  return typeof idx === "number" && idx > 0;
}

/**
 * Redirects after a save/delete whose target URL already exists earlier in
 * history (e.g. the page you edited, or the list you came from) - going
 * back to it via `navigate(-1)` instead of `replace`-ing the current entry
 * avoids landing on a duplicate of the same URL, which makes the next
 * "back" press a no-op. Falls back to a normal replace when there's nothing
 * to go back to (e.g. the page was opened directly).
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
