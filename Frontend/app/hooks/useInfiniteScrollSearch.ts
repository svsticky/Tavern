import { type RefObject, useEffect, useRef, useState } from "react";

type UseInfiniteScrollSearchArgs = {
  /** Whether this page's state was just restored from a back navigation. */
  isRestored: boolean;
  loading: boolean;
  hasMore: boolean;
  page: number;
  initialSearchQuery: string;
  initialDebouncedSearchQuery: string;
  /** An extra value (besides search) that should reset to page 1 when it changes. */
  resetDep: unknown;
  /** Called to reset to page 1 and fetch it, once the debounced search or `resetDep` changes. */
  onReset: (debouncedSearchQuery: string) => void;
  /** Called when the loader comes into view and there's more to fetch. */
  onLoadMore: (nextPage: number, debouncedSearchQuery: string) => void;
};

type UseInfiniteScrollSearchResult = {
  searchQuery: string;
  setSearchQuery: (value: string) => void;
  debouncedSearchQuery: string;
  loaderRef: RefObject<HTMLDivElement | null>;
  /** True once the first page has loaded - pair with `useScrollRestoration`. */
  hasLoadedOnce: boolean;
};

/**
 * Shared orchestration for admin list pages with debounced server-side
 * search and `IntersectionObserver`-driven infinite scroll: the debounce
 * timer, the restore-safe reset, and `hasLoadedOnce` for scroll restoration.
 * Callers keep owning their items/loading/hasMore state and fetch function -
 * this only decides *when* to call `onReset`/`onLoadMore`.
 */
export function useInfiniteScrollSearch({
  isRestored,
  loading,
  hasMore,
  page,
  initialSearchQuery,
  initialDebouncedSearchQuery,
  resetDep,
  onReset,
  onLoadMore,
}: UseInfiniteScrollSearchArgs): UseInfiniteScrollSearchResult {
  const [searchQuery, setSearchQuery] = useState(initialSearchQuery);
  const [debouncedSearchQuery, setDebouncedSearchQuery] = useState(
    initialDebouncedSearchQuery,
  );
  const loaderRef = useRef<HTMLDivElement>(null);
  const [hasLoadedOnce, setHasLoadedOnce] = useState(!loading);

  // Stable snapshots of the restored values, never mutated - used below to
  // decide whether search/resetDep have actually changed since mount. A ref
  // that gets consumed (set to false) inside the effect would break under
  // React Strict Mode's double-invoked effects: the first pass would skip
  // and consume it, the second would find it already consumed and fire for
  // real, discarding the restored data.
  const initialSearchQueryRef = useRef(initialSearchQuery);
  const initialDebouncedSearchQueryRef = useRef(initialDebouncedSearchQuery);
  const initialResetDepRef = useRef(resetDep);

  useEffect(() => {
    if (!loading) setHasLoadedOnce(true);
  }, [loading]);

  useEffect(() => {
    if (searchQuery === initialSearchQueryRef.current) return;
    const handler = setTimeout(() => setDebouncedSearchQuery(searchQuery), 300);
    return () => clearTimeout(handler);
  }, [searchQuery]);

  // onReset/onLoadMore are recreated every render, so depending on them
  // would refire this on every render instead of only real changes.
  // biome-ignore lint/correctness/useExhaustiveDependencies: see comment above
  useEffect(() => {
    const isStillInitial =
      isRestored &&
      debouncedSearchQuery === initialDebouncedSearchQueryRef.current &&
      resetDep === initialResetDepRef.current;
    if (isStillInitial) return;
    onReset(debouncedSearchQuery);
  }, [debouncedSearchQuery, resetDep]);

  // biome-ignore lint/correctness/useExhaustiveDependencies: see comment above
  useEffect(() => {
    const observer = new IntersectionObserver(
      (entries) => {
        if (entries[0].isIntersecting && hasMore && !loading) {
          onLoadMore(page + 1, debouncedSearchQuery);
        }
      },
      { threshold: 1.0 },
    );

    if (loaderRef.current) {
      observer.observe(loaderRef.current);
    }

    return () => observer.disconnect();
  }, [hasMore, loading, page, debouncedSearchQuery]);

  return {
    searchQuery,
    setSearchQuery,
    debouncedSearchQuery,
    loaderRef,
    hasLoadedOnce,
  };
}
