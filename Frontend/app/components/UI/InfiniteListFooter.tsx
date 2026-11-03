import { t } from "i18next";
import type { RefObject } from "react";

/**
 * The bottom row of an infinite list: the sentinel that triggers loading the
 * next page when it scrolls into view (see `useInfiniteList`), with a status text.
 *
 * @param {Object} props - The component props.
 * @param {RefObject<HTMLDivElement | null>} props.sentinelRef - The ref from `useInfiniteList`.
 * @param {boolean} props.loading - Whether a page is loading.
 * @param {boolean} props.hasMore - Whether more pages exist.
 * @param {boolean} props.isEmpty - Whether the list has no rows.
 * @param {string} props.noMoreText - The text shown once every page is loaded.
 * @returns {JSX.Element} - The rendered footer.
 * @component
 */
export default function InfiniteListFooter({
  sentinelRef,
  loading,
  hasMore,
  isEmpty,
  noMoreText,
}: {
  sentinelRef: RefObject<HTMLDivElement | null>;
  loading: boolean;
  hasMore: boolean;
  isEmpty: boolean;
  noMoreText: string;
}) {
  return (
    <div ref={sentinelRef} className="h-10 flex items-center justify-center">
      <span className="text-slate-400 text-sm">
        {loading
          ? t("loading_more")
          : hasMore
            ? t("load_more")
            : isEmpty
              ? t("no_data")
              : noMoreText}
      </span>
    </div>
  );
}
