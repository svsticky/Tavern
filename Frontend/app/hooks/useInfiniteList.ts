import { t } from "i18next";
import { useCallback, useEffect, useRef, useState } from "react";
import toast from "react-hot-toast";
import { useSearchParams } from "react-router";
import { appendErrorMessage } from "~/util/error.util";
import { PAGES_PARAM } from "~/util/infiniteList.util";

/**
 * Options for the useInfiniteList hook.
 * @typedef {Object} UseInfiniteListOptions
 * @property {T[]} initialItems - The rows the route's loader fetched (all loaded pages at once).
 * @property {number} initialPages - How many pages the loader fetched.
 * @property {number} pageSize - The number of rows per page.
 * @property {(page: number, pageSize: number) => Promise<T[]>} fetchPage - Fetches one page with the current search and filters; throws on failure.
 */
type UseInfiniteListOptions<T> = {
  initialItems: T[];
  initialPages: number;
  pageSize: number;
  fetchPage: (page: number, pageSize: number) => Promise<T[]>;
};

/**
 * Manages an infinite list whose first pages come from the route's loader.
 *
 * Loads the next page when the sentinel element scrolls into view, appends it,
 * and stores the number of loaded pages in the URL's `pages` parameter. That
 * update replaces the history entry and keeps the scroll position, so going back
 * to the list restores the same rows (and `ScrollRestoration` the same position)
 * without adding history entries. Pair it with `shouldRevalidateIgnoringPages`
 * so this URL update doesn't rerun the loader.
 *
 * Resets to the loader's rows whenever the loader runs again, e.g. after the
 * search or a filter changed.
 *
 * @param {UseInfiniteListOptions<T>} options - The loader data, page size and page fetcher.
 * @returns The rows, whether more pages exist, whether a page is loading, and the ref for the sentinel element.
 */
export function useInfiniteList<T>({
  initialItems,
  initialPages,
  pageSize,
  fetchPage,
}: UseInfiniteListOptions<T>) {
  const [, setSearchParams] = useSearchParams();
  const [items, setItems] = useState(initialItems);
  const [pages, setPages] = useState(initialPages);
  const [hasMore, setHasMore] = useState(
    initialItems.length >= initialPages * pageSize,
  );
  const [loading, setLoading] = useState(false);
  const sentinelRef = useRef<HTMLDivElement>(null);

  // Bumped on every reset, so a page that arrives after the search or filters
  // changed isn't appended to the new list.
  const generation = useRef(0);
  const fetchPageRef = useRef(fetchPage);
  fetchPageRef.current = fetchPage;

  useEffect(() => {
    generation.current += 1;
    setItems(initialItems);
    setPages(initialPages);
    setHasMore(initialItems.length >= initialPages * pageSize);
    setLoading(false);
  }, [initialItems, initialPages, pageSize]);

  const loadMore = useCallback(async () => {
    const currentGeneration = generation.current;
    const nextPage = pages + 1;
    setLoading(true);

    try {
      const fetched = await fetchPageRef.current(nextPage, pageSize);
      if (generation.current !== currentGeneration) return;

      setItems((prev) => [...prev, ...fetched]);
      setPages(nextPage);
      if (fetched.length < pageSize) setHasMore(false);
      setSearchParams(
        (prev) => {
          const next = new URLSearchParams(prev);
          next.set(PAGES_PARAM, String(nextPage));
          return next;
        },
        { replace: true, preventScrollReset: true },
      );
    } catch (error) {
      console.error("Error loading the next page:", error);
      toast.error(appendErrorMessage(t("loading_failed"), error));
    } finally {
      if (generation.current === currentGeneration) setLoading(false);
    }
  }, [pages, pageSize, setSearchParams]);

  useEffect(() => {
    const observer = new IntersectionObserver(
      (entries) => {
        if (entries[0].isIntersecting && hasMore && !loading) loadMore();
      },
      { threshold: 1.0 },
    );

    if (sentinelRef.current) observer.observe(sentinelRef.current);
    return () => observer.disconnect();
  }, [hasMore, loading, loadMore]);

  return { items, hasMore, loading, sentinelRef };
}
