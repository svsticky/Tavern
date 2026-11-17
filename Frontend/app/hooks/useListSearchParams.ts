import { useCallback, useEffect, useState } from "react";
import { useSearchParams } from "react-router";
import { PAGES_PARAM } from "~/util/infiniteList.util";

/**
 * Gives access to the search parameters that hold an infinite list's search and
 * filters. `updateListParams` changes them without adding a history entry or
 * scrolling to the top, and drops `pages` so the list starts again at page 1.
 *
 * @returns The current search parameters and the function to update them.
 */
export function useListSearchParams() {
  const [searchParams, setSearchParams] = useSearchParams();

  const updateListParams = useCallback(
    (update: (params: URLSearchParams) => void) => {
      setSearchParams(
        (prev) => {
          const next = new URLSearchParams(prev);
          update(next);
          next.delete(PAGES_PARAM);
          return next;
        },
        { replace: true, preventScrollReset: true },
      );
    },
    [setSearchParams],
  );

  return { searchParams, updateListParams };
}

/**
 * Keeps a search input in sync with a URL search parameter: the input updates
 * right away, and the URL (which reruns the route's loader) 300 ms after the
 * last keystroke.
 *
 * @param {string} [name="search"] - The search parameter to store the value in.
 * @param {number} [delay=300] - How long to wait after the last keystroke, in milliseconds.
 * @returns {[string, (value: string) => void]} The input's value and its setter.
 */
export function useDebouncedSearchParam(
  name = "search",
  delay = 300,
): [string, (value: string) => void] {
  const { searchParams, updateListParams } = useListSearchParams();
  const urlValue = searchParams.get(name) ?? "";
  const [value, setValue] = useState(urlValue);

  useEffect(() => {
    if (value === urlValue) return;

    const handler = setTimeout(() => {
      updateListParams((params) => {
        if (value) params.set(name, value);
        else params.delete(name);
      });
    }, delay);

    return () => clearTimeout(handler);
  }, [value, urlValue, name, delay, updateListParams]);

  return [value, setValue];
}
