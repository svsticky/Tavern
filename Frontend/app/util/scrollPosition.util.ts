import type { NavigationType } from "react-router";

/**
 * Shared scroll-position store keyed by pathname, used by both
 * `useScrollRestoration` and `GlobalScrollRestoration` so they stay
 * consistent instead of each guessing from a different source.
 */
const scrollPositions = new Map<string, number>();

export function recordScrollPosition(pathname: string, scrollY: number) {
  scrollPositions.set(pathname, scrollY);
}

export function getScrollPosition(pathname: string): number | undefined {
  return scrollPositions.get(pathname);
}

const RETRY_WINDOW_MS = 1000;

/**
 * Scrolls to `target`, retrying while the page is still too short to reach
 * it (e.g. late-loading images) instead of getting clamped short. Stops once
 * the target is reached, or once the scroll position no longer matches what
 * this last set (the user has taken over).
 */
export function scrollToWithRetry(target: number): () => void {
  window.scrollTo({ top: target });
  let lastSetScrollY = window.scrollY;

  if (lastSetScrollY === target) {
    return () => {};
  }

  const observer = new ResizeObserver(() => {
    if (Math.abs(window.scrollY - lastSetScrollY) > 1) {
      observer.disconnect();
      return;
    }

    window.scrollTo({ top: target });
    lastSetScrollY = window.scrollY;
    if (lastSetScrollY === target) {
      observer.disconnect();
    }
  });
  observer.observe(document.body);

  const timeout = window.setTimeout(
    () => observer.disconnect(),
    RETRY_WINDOW_MS,
  );

  return () => {
    observer.disconnect();
    window.clearTimeout(timeout);
  };
}

/** Restores the saved scroll position for `pathname` on a POP navigation, top otherwise. */
export function restoreScroll(
  pathname: string,
  navigationType: NavigationType,
): () => void {
  const saved =
    navigationType === "POP" ? getScrollPosition(pathname) : undefined;
  return scrollToWithRetry(saved ?? 0);
}
