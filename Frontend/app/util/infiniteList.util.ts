import type { ShouldRevalidateFunction } from "react-router";

/** URL search param holding how many pages of an infinite list are loaded. */
export const PAGES_PARAM = "pages";

/** Upper bound on pages a (possibly hand-edited) URL can make a loader fetch. */
export const MAX_PAGES = 25;

/** Reads the loaded-page count from the URL, defaulting to 1 and clamped to a sane range. */
export function readPages(searchParams: URLSearchParams): number {
  const pages = Math.floor(Number(searchParams.get(PAGES_PARAM)));
  if (!Number.isFinite(pages) || pages < 1) return 1;
  return Math.min(pages, MAX_PAGES);
}

/**
 * Fetches pages 1..`count` in parallel and joins them, so a route loader can
 * rebuild everything an infinite list had loaded before the user navigated
 * away. Restoring the whole list before the route renders is what lets
 * React Router's own scroll restoration land on the right spot - the page is
 * as tall as it was.
 */
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

/**
 * A `shouldRevalidate` that skips re-running a route's loader when the only
 * thing that changed in the URL is one of `ignoredParams`. Use it so state
 * that's mirrored into the URL purely for back-navigation (e.g. how many
 * pages of an infinite list are loaded) doesn't refetch data the page already
 * has. Anything else - including an explicit revalidation with an unchanged
 * URL, e.g. after a mutation - falls through to React Router's default.
 */
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
