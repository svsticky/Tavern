import type { ShouldRevalidateFunctionArgs } from "react-router";

/** The search parameter that holds how many pages of an infinite list are loaded. */
export const PAGES_PARAM = "pages";

/** The most pages a loader restores at once, so a hand-edited URL can't request thousands of rows. */
export const MAX_RESTORED_PAGES = 20;

/**
 * Returns how many pages of an infinite list are loaded according to the URL,
 * so a loader can restore them all in one request after navigating back.
 *
 * @param {URL | URLSearchParams} source - The request URL or its search parameters.
 * @returns {number} The number of loaded pages, between 1 and `MAX_RESTORED_PAGES`.
 */
export const getLoadedPages = (source: URL | URLSearchParams): number => {
  const searchParams = source instanceof URL ? source.searchParams : source;
  const pages = Number.parseInt(searchParams.get(PAGES_PARAM) ?? "", 10);
  if (!Number.isInteger(pages) || pages < 1) return 1;
  return Math.min(pages, MAX_RESTORED_PAGES);
};

/**
 * Returns the URL's search parameters without `pages`, to compare list filters.
 *
 * @param {URL} url - The URL to read.
 * @returns {string} The sorted search parameters without `pages`.
 */
const withoutPages = (url: URL): string => {
  const searchParams = new URLSearchParams(url.searchParams);
  searchParams.delete(PAGES_PARAM);
  searchParams.sort();
  return `${url.pathname}?${searchParams}`;
};

/**
 * A `shouldRevalidate` for routes with an infinite list: loading more pages only
 * updates `pages` in the URL, which must not rerun the loader (the page already
 * appended the new rows). Any other change, like the search or a filter, does.
 *
 * @param {ShouldRevalidateFunctionArgs} args - The revalidation arguments from React Router.
 * @returns {boolean} Whether the route's loader should run again.
 */
export const shouldRevalidateIgnoringPages = ({
  currentUrl,
  nextUrl,
  defaultShouldRevalidate,
}: ShouldRevalidateFunctionArgs): boolean => {
  const onlyPagesChanged =
    currentUrl.href !== nextUrl.href &&
    withoutPages(currentUrl) === withoutPages(nextUrl);
  return onlyPagesChanged ? false : defaultShouldRevalidate;
};
