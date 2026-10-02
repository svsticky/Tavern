import { useLayoutEffect, useRef } from "react";
import { useLocation, useNavigationType } from "react-router";
import { restoreScroll } from "~/util/scrollPosition.util";

/**
 * Restores scroll position on a page whose content is fetched client-side
 * after mount. Call this once data is loaded AND actually rendering
 * (`ready`) - a page can still render blank after the fetch resolves (e.g.
 * behind a separate auth check), which would waste the one-shot restore.
 * Pairs with `GlobalScrollRestoration` in root.tsx, which records scroll
 * position for every page and handles the immediate fallback restore.
 */
export function useScrollRestoration(ready: boolean) {
  const { pathname } = useLocation();
  const navigationType = useNavigationType();
  const appliedRef = useRef(false);

  useLayoutEffect(() => {
    if (!ready || appliedRef.current) return;
    appliedRef.current = true;
    return restoreScroll(pathname, navigationType);
  }, [ready, navigationType, pathname]);
}
