import { t } from "i18next";
import { useLoaderData, useNavigate } from "react-router";
import type { ActivityResponseDto } from "~/api";
import StickyLoadingLogo from "~/components/StickyLoadingLogo";
import BorderedTile from "~/components/Tiles/BorderedTile";
import type { Column } from "~/components/Tiles/DataTableTile";
import DataTable from "~/components/Tiles/DataTableTile";
import Button from "~/components/UI/Button";
import InfiniteListFooter from "~/components/UI/InfiniteListFooter";
import Input from "~/components/UI/Input";
import { PageHeader } from "~/components/UI/PageHeader";
import Select from "~/components/UI/Select";
import { useInfiniteList } from "~/hooks/useInfiniteList";
import {
  useDebouncedSearchParam,
  useListSearchParams,
} from "~/hooks/useListSearchParams";
import { formatDate } from "~/util/date.util";
import {
  getLoadedPages,
  shouldRevalidateIgnoringPages,
} from "~/util/infiniteList.util";
import {
  loadCommitteeYear,
  requireBoardOrCandidateBoard,
} from "~/util/loaderAuth.util";
import type { Route } from "./+types/activities";
import {
  ACTIVITIES_PAGE_SIZE,
  fetchAdminActivities,
  handleViewActivity,
} from "./activities.handlers";

/**
 * Loads the activities of the year and search in the URL (the current committee
 * year when none is set). When the URL says more pages were loaded (after
 * navigating back), it fetches all of them in one request so the list, and with
 * it the scroll position, is restored.
 * Only board and candidate board members can open this page.
 *
 * @param {Route.ClientLoaderArgs} args - The loader arguments, containing the request.
 * @throws {Error} When the activities fail to load, which shows the route's error page.
 */
export async function clientLoader({ request }: Route.ClientLoaderArgs) {
  await requireBoardOrCandidateBoard();
  const url = new URL(request.url);
  const pages = getLoadedPages(url);
  const committeeYear = await loadCommitteeYear();
  const urlYear = Number.parseInt(url.searchParams.get("year") ?? "", 10);
  const year =
    Number.isInteger(urlYear) && urlYear >= 2007 && urlYear <= committeeYear
      ? urlYear
      : committeeYear;

  const activities = await fetchAdminActivities({
    year,
    search: url.searchParams.get("search") ?? "",
    page: 1,
    pageSize: pages * ACTIVITIES_PAGE_SIZE,
  });
  return { activities, pages, year, committeeYear };
}

/**
 * Loading more activities only updates `pages` in the URL, which must not reload the list.
 */
export const shouldRevalidate = shouldRevalidateIgnoringPages;

/**
 * Shown while the route's `clientLoader` runs on the first page load.
 *
 * @component
 */
export function HydrateFallback() {
  return <StickyLoadingLogo />;
}

/**
 * An administrative management page for viewing and filtering all association activities.
 *
 * This component provides a robust interface for board members to track events across
 * different association years. It features:
 * - **Yearly Archiving**: A selector to view activities as far back as 2007.
 * - **Infinite Scrolling**: Loads the next page when the user reaches the end of
 *   the list (see `useInfiniteList`). The number of loaded pages, the search and
 *   the year are kept in the URL, so going back to this page restores the list
 *   and its scroll position.
 * - **Debounced Search**: Waits 300ms after the last keystroke before triggering a
 *   server-side search by activity name or location.
 * - **Data Visualization**: A `DataTable` that summarizes key metrics such as
 *   participant counts (including limits), pricing, and scheduling.
 * - **Contextual Navigation**: Quick access to the administrative details of any specific event.
 *
 * @page
 * @component
 */
export default function Activities() {
  const navigate = useNavigate();
  const loaderData = useLoaderData<typeof clientLoader>();
  const { searchParams, updateListParams } = useListSearchParams();
  const [searchQuery, setSearchQuery] = useDebouncedSearchParam();
  const { year, committeeYear: currentYear } = loaderData;

  const yearsSince2007 = Array.from(
    { length: currentYear - 2007 + 1 },
    (_, i) => currentYear - i,
  );

  const {
    items: activities,
    hasMore,
    loading,
    sentinelRef,
  } = useInfiniteList({
    initialItems: loaderData.activities,
    initialPages: loaderData.pages,
    pageSize: ACTIVITIES_PAGE_SIZE,
    fetchPage: (page, pageSize) =>
      fetchAdminActivities({
        year,
        search: searchParams.get("search") ?? "",
        page,
        pageSize,
      }),
  });

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
              value={searchQuery}
              onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
                setSearchQuery(e.target.value)
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
              value={year}
              onChange={(e) =>
                updateListParams((params) => params.set("year", e.target.value))
              }
            />
          </div>
        </div>
      </BorderedTile>

      <BorderedTile className="bg-white p-0">
        <DataTable data={activities} columns={columns} emptyText="" />

        <InfiniteListFooter
          sentinelRef={sentinelRef}
          loading={loading}
          hasMore={hasMore}
          isEmpty={activities.length === 0}
          noMoreText={t("no_more_activities")}
        />
      </BorderedTile>
    </div>
  );
}
