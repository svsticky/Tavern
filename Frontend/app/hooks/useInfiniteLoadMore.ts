import { useEffect, useRef, useState } from "react";
import { useSearchParams } from "react-router";
import { PAGES_PARAM, readPages } from "~/util/infiniteList.util";

/** Load-more for loader-backed lists; the route must export `shouldRevalidate = shouldRevalidateIgnoring("pages")`. */
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

  // A re-run loader hands back a fresh array: drop what was appended to the old one.
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
