import { t } from "i18next";
import { useCallback, useEffect, useState } from "react";
import { useLoaderData, useNavigate, useSearchParams } from "react-router";
import type { ActivityResponseDto } from "~/api";
import StickyLoadingLogo from "~/components/StickyLoadingLogo";
import BorderedTile from "~/components/Tiles/BorderedTile";
import type { Column } from "~/components/Tiles/DataTableTile";
import DataTable from "~/components/Tiles/DataTableTile";
import Button from "~/components/UI/Button";
import Input from "~/components/UI/Input";
import { PageHeader } from "~/components/UI/PageHeader";
import Select from "~/components/UI/Select";
import { useInfiniteLoadMore } from "~/hooks/useInfiniteLoadMore";
import { formatDate, getCommitteeYear } from "~/util/date.util";
import {
  fetchPages,
  PAGES_PARAM,
  readPages,
  shouldRevalidateIgnoring,
} from "~/util/infiniteList.util";
import { requireTokenParsed } from "~/util/loaderAuth.util";
import {
  fetchAdminActivitiesPage,
  handleViewActivity,
} from "./activities.handlers";

/** The number of activities to fetch per page for infinite scrolling. */
const PAGE_SIZE = 15;

type LoaderData = {
  activities: ActivityResponseDto[];
  year: number;
  search: string;
  hasMore: boolean;
};

/**
 * Reads `year`/`search`/`pages` from the URL (so a filtered, scrolled-down view
 * is shareable and restored for free on back-navigation) and fetches the
 * first `pages` pages for that combination. Further pages are appended as the
 * user scrolls (see `useInfiniteLoadMore`), which bumps `pages` in the URL.
 */
export async function clientLoader({
  request,
}: {
  request: Request;
}): Promise<LoaderData> {
  await requireTokenParsed();

  const url = new URL(request.url);
  const year = Number(url.searchParams.get("year")) || getCommitteeYear();
  const search = url.searchParams.get("search") ?? "";
  const pages = readPages(url.searchParams);

  const { items, hasMore } = await fetchPages(pages, PAGE_SIZE, (page) =>
    fetchAdminActivitiesPage(year, page, PAGE_SIZE, search),
  );

  return { activities: items, year, search, hasMore };
}

/** Bumping `pages` while scrolling must not refetch what's already loaded. */
export const shouldRevalidate = shouldRevalidateIgnoring(PAGES_PARAM);

export function HydrateFallback() {
  return <StickyLoadingLogo />;
}

/**
 * An administrative management page for viewing and filtering all association activities.
 *
 * This component provides a robust interface for board members to track events across
 * different association years. It features:
 * - **Yearly Archiving**: A selector to view activities as far back as 2007.
 * - **Infinite Scrolling**: Automatically loads more activities as the user scrolls down.
 * - **Debounced Search**: Waits 300ms after the last keystroke before syncing a
 *   server-side search (by activity name or location) into the URL.
 * - **Data Visualization**: A `DataTable` that summarizes key metrics such as
 *   participant counts (including limits), pricing, and scheduling.
 * - **Contextual Navigation**: Quick access to the administrative details of any specific event.
 *
 * @page
 * @component
 */
export default function Activities() {
  const loaderData = useLoaderData<typeof clientLoader>();
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();

  const currentYear = getCommitteeYear();
  const yearsSince2007 = Array.from(
    { length: currentYear - 2007 + 1 },
    (_, i) => currentYear - i,
  );

  const [searchInput, setSearchInput] = useState(loaderData.search);

  const {
    items: activities,
    hasMore,
    loadingMore,
    sentinelRef: loaderRef,
  } = useInfiniteLoadMore({
    loaderItems: loaderData.activities,
    loaderHasMore: loaderData.hasMore,
    pageSize: PAGE_SIZE,
    fetchPage: useCallback(
      (page: number) =>
        fetchAdminActivitiesPage(
          loaderData.year,
          page,
          PAGE_SIZE,
          loaderData.search,
        ),
      [loaderData.year, loaderData.search],
    ),
  });

  // The loader reruns whenever `year`/`search` change in the URL - keep the
  // box in sync with what it actually searched for.
  useEffect(() => {
    setSearchInput(loaderData.search);
  }, [loaderData.search]);

  useEffect(() => {
    const handler = setTimeout(() => {
      if (searchInput === loaderData.search) return;
      const next = new URLSearchParams(searchParams);
      if (searchInput) {
        next.set("search", searchInput);
      } else {
        next.delete("search");
      }
      next.delete(PAGES_PARAM);
      setSearchParams(next, { replace: true, preventScrollReset: true });
    }, 300);

    return () => clearTimeout(handler);
  }, [searchInput, loaderData.search, searchParams, setSearchParams]);

  const changeYear = (year: number) => {
    const next = new URLSearchParams(searchParams);
    next.set("year", String(year));
    next.delete(PAGES_PARAM);
    setSearchParams(next);
  };

  const columns: Column<ActivityResponseDto>[] = [
    {
      header: t("activity"),
      render: (act) => (
        <div className="flex items-center gap-3">
          <div className="flex flex-col">
            <span className="font-semibold text-slate-700">{act.name}</span>
            <span className="text-xs text-slate-400">{act.location}</span>
          </div>
        </div>
      ),
    },
    {
      header: t("date"),
      render: (act) => (
        <div className="flex flex-col">
          <span className="text-sm text-slate-600">
            {formatDate(new Date(act.dateTimeStart), "fullDateTime")}
          </span>
        </div>
      ),
    },
    {
      header: t("participants"),
      render: (act) => (
        <div className="flex flex-col">
          <span className="text-sm text-slate-600">
            👥 {act.enrollments.filter((e) => !e.isOnWaitingList).length}
            {act.participantLimit !== null ? `/${act.participantLimit}` : ""}
          </span>
        </div>
      ),
    },
    {
      header: t("price"),
      render: (act) => (
        <span className="font-medium text-slate-700">
          {act.price != null && act.price > 0
            ? `€${act.price.toFixed(2)}`
            : t("free")}
        </span>
      ),
    },
    {
      header: "",
      className: "w-full sm:w-px whitespace-nowrap text-right",
      render: (act) => (
        <Button
          variant="secondary"
          className="w-full sm:w-auto"
          onClick={(e) => {
            e.stopPropagation();
            handleViewActivity(navigate, act.id);
          }}
        >
          {t("view_activity")}
        </Button>
      ),
    },
  ];

  return (
    <div className="flex flex-col gap-4 p-4">
      <PageHeader title={t("activities")} backTo="/" />

      <BorderedTile>
        <div className="flex flex-col sm:flex-row items-center w-full gap-4">
          <div className="flex flex-col flex-1 w-full sm:w-auto">
            <Input
              label={t("search")}
              placeholder={t("search_activities")}
              value={searchInput}
              onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
                setSearchInput(e.target.value)
              }
            />
          </div>
          <div className="flex flex-col w-full sm:w-auto">
            <Select
              options={yearsSince2007.map((y) => ({
                label: `${y - 1}/${y}`,
                value: y,
              }))}
              label={t("year")}
              style={{ minWidth: "150px" }}
              value={loaderData.year}
              onChange={(e) => changeYear(Number(e.target.value))}
            />
          </div>
        </div>
      </BorderedTile>

      <BorderedTile className="bg-white p-0">
        <DataTable data={activities} columns={columns} emptyText="" />

        <div ref={loaderRef} className="h-10 flex items-center justify-center">
          <span className="text-slate-400 text-sm">
            {loadingMore
              ? t("loading_more")
              : hasMore
                ? t("load_more")
                : activities.length === 0
                  ? t("no_data")
                  : t("no_more_activities")}
          </span>
        </div>
      </BorderedTile>
    </div>
  );
}
