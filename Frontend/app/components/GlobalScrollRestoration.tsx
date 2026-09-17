import { useEffect, useLayoutEffect } from "react";
import { useLocation, useNavigationType } from "react-router";
import {
  recordScrollPosition,
  restoreScroll,
} from "~/util/scrollPosition.util";

/**
 * Replaces React Router's built-in `<ScrollRestoration>`, which restores too
 * early for client-fetched pages. Records scroll position for every page (the
 * single source of truth `useScrollRestoration` reads from) and provides the
 * immediate fallback restore for pages that don't call `useScrollRestoration`
 * themselves - those always win the final word, since their layout effect
 * only fires once their own data is ready.
 */
export function GlobalScrollRestoration() {
  const { pathname } = useLocation();
  const navigationType = useNavigationType();

  // The browser's own native scroll restoration (on by default) fights with
  // this - React Router's <ScrollRestoration> used to disable it for us.
  useEffect(() => {
    const previous = window.history.scrollRestoration;
    window.history.scrollRestoration = "manual";
    return () => {
      window.history.scrollRestoration = previous;
    };
  }, []);

  useEffect(() => {
    const onScroll = () => {
      // A scroll-triggering effect elsewhere (e.g. this restoring a
      // freshly-navigated page to 0) can fire before this listener's own
      // cleanup runs on navigation, misattributing that scroll to the page
      // being left. Guard against it with the actual current URL.
      if (window.location.pathname !== pathname) return;
      recordScrollPosition(pathname, window.scrollY);
    };
    window.addEventListener("scroll", onScroll, { passive: true });
    return () => window.removeEventListener("scroll", onScroll);
  }, [pathname]);

  useLayoutEffect(() => {
    return restoreScroll(pathname, navigationType);
  }, [pathname, navigationType]);

  return null;
}
