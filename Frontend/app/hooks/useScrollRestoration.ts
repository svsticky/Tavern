import { useEffect, useLayoutEffect, useRef } from "react";
import { useLocation, useNavigationType } from "react-router";

const scrollPositions = new Map<string, number>();

/**
 * Restores scroll position on a page whose content is fetched client-side
 * after mount (no route loader) - React Router's built-in
 * `<ScrollRestoration>` restores too early for these, before the re-fetch has
 * rendered. Call this once data is actually loaded (`ready`); on a back
 * navigation it restores the last scroll position for this path, otherwise
 * it scrolls to top. Runs in a layout effect to avoid a visible flash.
 */
export function useScrollRestoration(ready: boolean) {
  const { pathname } = useLocation();
  const navigationType = useNavigationType();
  const appliedRef = useRef(false);

  useEffect(() => {
    const onScroll = () => {
      scrollPositions.set(pathname, window.scrollY);
    };
    window.addEventListener("scroll", onScroll, { passive: true });
    return () => window.removeEventListener("scroll", onScroll);
  }, [pathname]);

  useLayoutEffect(() => {
    if (!ready || appliedRef.current) return;
    appliedRef.current = true;

    const saved =
      navigationType === "POP" ? scrollPositions.get(pathname) : undefined;
    window.scrollTo({ top: saved ?? 0 });
  }, [ready, navigationType, pathname]);
}
