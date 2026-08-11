import { t } from "i18next";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { useNavigate } from "react-router";
import type { ActivityResponseDto } from "~/api";
import BorderedTile from "~/components/Tiles/BorderedTile";
import type { Column } from "~/components/Tiles/DataTableTile";
import DataTable from "~/components/Tiles/DataTableTile";
import Button from "~/components/UI/Button";
import Input from "~/components/UI/Input";
import { PageHeader } from "~/components/UI/PageHeader/PageHeader";
import Select from "~/components/UI/Select";
import { formatDate, getCommitteeYear } from "~/util/date.util";
import { handleViewActivity, loadAdminActivities } from "./activities.handlers";

/** The number of activities to fetch per page for infinite scrolling. */
const PAGE_SIZE = 15;

/**
 * An administrative management page for viewing and filtering all association activities.
 *
 * This component provides a robust interface for board members to track events across
 * different association years. It features:
 * - **Yearly Archiving**: A selector to view activities as far back as 2007.
 * - **Infinite Scrolling**: Automatically loads more activities as the user scrolls down.
 * - **Real-time Filtering**: Search by activity name or location using a memoized filter.
 * - **Data Visualization**: A `DataTable` that summarizes key metrics such as
 *   participant counts (including limits), pricing, and scheduling.
 * - **Contextual Navigation**: Quick access to the administrative details of any specific event.
 *
 * @page
 * @component
 */
export default function Activities() {
  const navigate = useNavigate();

  const [loading, setLoading] = useState(false);
  const currentYear = getCommitteeYear();
  const [year, setYear] = useState(currentYear);
  const [activities, setActivities] = useState<ActivityResponseDto[]>([]);
  const [searchQuery, setSearchQuery] = useState("");

  const [page, setPage] = useState(1);
  const [hasMore, setHasMore] = useState(true);
  const loaderRef = useRef<HTMLDivElement>(null);

  const yearsSince2007 = Array.from(
    { length: currentYear - 2007 + 1 },
    (_, i) => currentYear - i,
  );

  const fetchActivities = useCallback(
    async (pageNum: number, isInitial: boolean, targetYear: number) => {
      loadAdminActivities(
        targetYear,
        setLoading,
        (fetched) => {
          setActivities((prev) =>
            isInitial ? fetched : [...prev, ...fetched],
          );
          if (fetched.length < PAGE_SIZE) {
            setHasMore(false);
          }
        },
        pageNum,
        PAGE_SIZE,
      );
    },
    [],
  );

  useEffect(() => {
    let isCurrent = true;

    setPage(1);
    setHasMore(true);

    loadAdminActivities(
      year,
      setLoading,
      (fetched) => {
        if (!isCurrent) return;
        setActivities(fetched);
        if (fetched.length < PAGE_SIZE) {
          setHasMore(false);
        }
      },
      1,
      PAGE_SIZE,
    );

    return () => {
      isCurrent = false;
    };
  }, [year]);

  useEffect(() => {
    const observer = new IntersectionObserver(
      (entries) => {
        if (entries[0].isIntersecting && hasMore && !loading) {
          const nextPage = page + 1;
          setPage(nextPage);
          fetchActivities(nextPage, false, year);
        }
      },
      { threshold: 1.0 },
    );

    if (loaderRef.current) {
      observer.observe(loaderRef.current);
    }

    return () => observer.disconnect();
  }, [hasMore, loading, page, year, fetchActivities]);

  const filteredActivities = useMemo(() => {
    if (!activities) return [];
    return activities.filter(
      (act) =>
        act.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
        act.location?.toLowerCase().includes(searchQuery.toLowerCase()),
    );
  }, [activities, searchQuery]);

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
              onChange={(e) => setYear(Number(e.target.value))}
            />
          </div>
        </div>
      </BorderedTile>

      <BorderedTile className="bg-white p-0">
        <DataTable data={filteredActivities} columns={columns} emptyText="" />

        <div ref={loaderRef} className="h-10 flex items-center justify-center">
          <span className="text-slate-400 text-sm">
            {loading
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
