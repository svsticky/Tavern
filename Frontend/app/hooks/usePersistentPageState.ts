import { useCallback, useState } from "react";
import { useLocation, useNavigationType } from "react-router";
import { hasInAppHistory } from "~/util/navigation.util";

const pageStateCache = new Map<string, unknown>();

/**
 * Persists a page's local state (fetched data, search/filter text, pagination)
 * across SPA navigations, keyed by pathname, and hands it back on a genuine
 * back navigation instead of the page resetting to defaults and re-fetching.
 *
 * Seed each piece of state from `initial`, skip the fetch effect when
 * `isRestored` is true, and call `save(...)` once loading finishes so the
 * cache stays current.
 */
export function usePersistentPageState<T>(createDefaults: () => T) {
  const { pathname } = useLocation();
  const navigationType = useNavigationType();

  const [restored] = useState<T | undefined>(() => {
    // "POP" also fires on a fresh page load, not just a real back navigation,
    // so also require in-app history.
    if (navigationType !== "POP" || !hasInAppHistory()) return undefined;
    return pageStateCache.get(pathname) as T | undefined;
  });

  const isRestored = restored !== undefined;
  const initial = restored ?? createDefaults();

  const save = useCallback(
    (state: T) => {
      pageStateCache.set(pathname, state);
    },
    [pathname],
  );

  return { initial, isRestored, save };
}
