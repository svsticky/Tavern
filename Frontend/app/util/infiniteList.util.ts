import type { ShouldRevalidateFunction } from "react-router";

export const PAGES_PARAM = "pages";

/** Caps what a hand-edited URL can make a loader fetch. */
export const MAX_PAGES = 25;

export function readPages(searchParams: URLSearchParams): number {
  const pages = Math.floor(Number(searchParams.get(PAGES_PARAM)));
  if (!Number.isFinite(pages) || pages < 1) return 1;
  return Math.min(pages, MAX_PAGES);
}

/** Rebuilds the whole list on back-navigation, so the page is as tall as before and scroll restores. */
export async function fetchPages<T>(
  count: number,
  pageSize: number,
  fetchPage: (page: number) => Promise<T[]>,
): Promise<{ items: T[]; hasMore: boolean }> {
  const pages = await Promise.all(
    Array.from({ length: count }, (_, i) => fetchPage(i + 1)),
  );
  return {
    items: pages.flat(),
    hasMore: pages[pages.length - 1].length === pageSize,
  };
}

/** Skips the loader when only the given params changed (e.g. `pages`), so URL bookkeeping doesn't refetch. */
export function shouldRevalidateIgnoring(
  ...ignoredParams: string[]
): ShouldRevalidateFunction {
  return ({ currentUrl, nextUrl, defaultShouldRevalidate }) => {
    if (currentUrl.pathname !== nextUrl.pathname)
      return defaultShouldRevalidate;

    const strip = (url: URL) => {
      const params = new URLSearchParams(url.search);
      for (const key of ignoredParams) params.delete(key);
      params.sort();
      return params.toString();
    };

    const onlyIgnoredChanged =
      currentUrl.search !== nextUrl.search &&
      strip(currentUrl) === strip(nextUrl);

    return onlyIgnoredChanged ? false : defaultShouldRevalidate;
  };
}
