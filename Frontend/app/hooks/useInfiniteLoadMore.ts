import { useEffect, useRef, useState } from "react";
import { useSearchParams } from "react-router";
import { PAGES_PARAM, readPages } from "~/util/infiniteList.util";

/**
 * Infinite-scroll "load more" on top of a route `clientLoader`.
 *
 * The loader supplies the first `pages` pages (see `fetchPages`); this hook
 * appends further pages as the returned `sentinelRef` scrolls into view, and
 * mirrors the loaded-page count into the URL's `pages` param with a
 * `replace` navigation. That way a back navigation to this entry re-runs the
 * loader for the same number of pages and the page is as tall as it was -
 * which is what makes scroll restoration work on a list that grew after the
 * first load.
 *
 * The route must export `shouldRevalidate = shouldRevalidateIgnoring("pages")`,
 * otherwise bumping the URL would refetch and reset everything just loaded.
 */
export function useInfiniteLoadMore<T>({
  loaderItems,
  loaderHasMore,
  pageSize,
  fetchPage,
  onError,
}: {
  loaderItems: T[];
  loaderHasMore: boolean;
  pageSize: number;
  fetchPage: (page: number) => Promise<T[]>;
  onError?: (error: unknown) => void;
}) {
  const [searchParams, setSearchParams] = useSearchParams();
  const pages = readPages(searchParams);

  const [items, setItems] = useState(loaderItems);
  const [hasMore, setHasMore] = useState(loaderHasMore);
  const [loadingMore, setLoadingMore] = useState(false);
  const [loadFailed, setLoadFailed] = useState(false);
  const sentinelRef = useRef<HTMLDivElement>(null);
  const inFlight = useRef(false);

  // A re-run loader (search/filter/year changed) hands back a fresh array:
  // drop whatever was appended on top of the old one.
  useEffect(() => {
    setItems(loaderItems);
    setHasMore(loaderHasMore);
    setLoadFailed(false);
  }, [loaderItems, loaderHasMore]);

  useEffect(() => {
    if (!hasMore || loadFailed) return;

    const observer = new IntersectionObserver(
      (entries) => {
        if (!entries[0].isIntersecting || inFlight.current) return;

        inFlight.current = true;
        setLoadingMore(true);
        const nextPage = pages + 1;

        fetchPage(nextPage)
          .then((fetched) => {
            setItems((prev) => [...prev, ...fetched]);
            setHasMore(fetched.length === pageSize);
            setSearchParams(
              (prev) => {
                const next = new URLSearchParams(prev);
                next.set(PAGES_PARAM, String(nextPage));
                return next;
              },
              { replace: true, preventScrollReset: true },
            );
          })
          .catch((error) => {
            setLoadFailed(true);
            onError?.(error);
          })
          .finally(() => {
            inFlight.current = false;
            setLoadingMore(false);
          });
      },
      { threshold: 1.0 },
    );

    if (sentinelRef.current) observer.observe(sentinelRef.current);
    return () => observer.disconnect();
  }, [
    hasMore,
    loadFailed,
    pages,
    pageSize,
    fetchPage,
    onError,
    setSearchParams,
  ]);

  return { items, hasMore, loadingMore, sentinelRef };
}
