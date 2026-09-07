import { t } from "i18next";
import { Archive, ArchiveRestore, Copy, Trash2Icon } from "lucide-react";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import toast from "react-hot-toast";
import { useNavigate } from "react-router";
import { type ActivityResponseDto, patchActivitiesById } from "~/api";
import BorderedTile from "~/components/Tiles/BorderedTile";
import type { Column } from "~/components/Tiles/DataTableTile";
import DataTable from "~/components/Tiles/DataTableTile";
import Button from "~/components/UI/Button";
import { useConfirm } from "~/components/UI/ConfirmModal/useConfirm";
import Input from "~/components/UI/Input";
import { PageHeader } from "~/components/UI/PageHeader";
import Select from "~/components/UI/Select";
import { useAuth } from "~/context/AuthContext";
import type { TokenParsed } from "~/types/TokenParsed";
import { formatDate, getCommitteeYear } from "~/util/date.util";
import { hasPermission, isBoardOrCandidateBoard } from "~/util/group.util";
import {
  handleDeleteAdminActivity,
  handleViewActivity,
  loadAdminActivities,
} from "./activities.handlers";

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
  const [confirmModal, confirm] = useConfirm();
  const authService = useAuth();
  const [tokenParsed, setTokenParsed] = useState<TokenParsed | null>(null);

  useEffect(() => {
    let cancelled = false;
    authService.getTokenParsed().then((token) => {
      if (!cancelled) setTokenParsed(token);
    });
    return () => {
      cancelled = true;
    };
  }, [authService]);

  const isBoard = isBoardOrCandidateBoard(tokenParsed);
  const canViewPastActivities =
    isBoard || hasPermission(tokenParsed, "ViewPastActivities");

  const [loading, setLoading] = useState(false);
  const currentYear = getCommitteeYear();
  const [year, setYear] = useState(currentYear);
  const [activities, setActivities] = useState<ActivityResponseDto[]>([]);
  const [searchQuery, setSearchQuery] = useState("");
  const [statusFilter, setStatusFilter] = useState<"active" | "archived">(
    "active",
  );

  const [page, setPage] = useState(1);
  const [hasMore, setHasMore] = useState(true);
  const loaderRef = useRef<HTMLDivElement>(null);

  const yearsSince2007 = Array.from(
    { length: currentYear - 2007 + 1 },
    (_, i) => currentYear - i,
  );

  const isArchivedQuery = statusFilter === "archived";

  const fetchActivities = useCallback(
    async (
      pageNum: number,
      isInitial: boolean,
      targetYear: number,
      targetArchived: boolean,
    ) => {
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
        canViewPastActivities,
        targetArchived,
      );
    },
    [canViewPastActivities],
  );

  useEffect(() => {
    if (!tokenParsed) return;

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
      canViewPastActivities,
      isArchivedQuery,
    );

    return () => {
      isCurrent = false;
    };
  }, [year, tokenParsed, canViewPastActivities, isArchivedQuery]);

  useEffect(() => {
    const observer = new IntersectionObserver(
      (entries) => {
        if (entries[0].isIntersecting && hasMore && !loading) {
          const nextPage = page + 1;
          setPage(nextPage);
          fetchActivities(nextPage, false, year, isArchivedQuery);
        }
      },
      { threshold: 1.0 },
    );

    if (loaderRef.current) {
      observer.observe(loaderRef.current);
    }

    return () => observer.disconnect();
  }, [hasMore, loading, page, year, isArchivedQuery, fetchActivities]);

  const filteredActivities = useMemo(() => {
    if (!activities) return [];
    return activities.filter(
      (act) =>
        act.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
        act.location?.toLowerCase().includes(searchQuery.toLowerCase()),
    );
  }, [activities, searchQuery]);

  const { publishedActivities, unpublishedActivities } = useMemo(() => {
    const published: ActivityResponseDto[] = [];
    const unpublished: ActivityResponseDto[] = [];

    for (const act of filteredActivities) {
      if (act.showInKoala || act.showOnWebsite) {
        published.push(act);
      } else {
        unpublished.push(act);
      }
    }

    return {
      publishedActivities: published,
      unpublishedActivities: unpublished,
    };
  }, [filteredActivities]);

  const columns: Column<ActivityResponseDto>[] = [
    {
      header: t("activity"),
      render: (act) => (
        <div className="flex items-center gap-3">
          <div className="flex flex-col">
            <div className="flex items-center gap-2">
              <span className="font-semibold text-slate-700">{act.name}</span>
              {act.isArchived && (
                <span className="text-[10px] uppercase font-bold tracking-wider px-1.5 py-0.5 rounded bg-stone-200 text-stone-700">
                  {t("archived_activity_badge")}
                </span>
              )}
            </div>
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
        <div className="flex items-center justify-end gap-2">
          {(isBoard ||
            hasPermission(tokenParsed, "EditAllActivities") ||
            (act.organizerId
              ? hasPermission(
                  tokenParsed,
                  "EditActivityForGroup",
                  act.organizerId,
                )
              : false)) && (
            <Button
              variant="secondary"
              className="px-2"
              aria-label={t("clone_activity")}
              title={t("clone_activity")}
              onClick={(e) => {
                e.stopPropagation();
                navigate(`/activities/create?cloneFrom=${act.id}`);
              }}
            >
              <Copy size={16} />
            </Button>
          )}
          {isBoard && (
            <Button
              variant="secondary"
              className="px-2"
              aria-label={
                act.isArchived ? t("unarchive_activity") : t("archive_activity")
              }
              title={
                act.isArchived ? t("unarchive_activity") : t("archive_activity")
              }
              onClick={async (e) => {
                e.stopPropagation();
                const confirmed = await confirm(
                  act.isArchived
                    ? t("confirm_unarchive_activity")
                    : t("confirm_archive_activity"),
                  {
                    title: act.isArchived
                      ? t("unarchive_activity")
                      : t("archive_activity"),
                    variant: "secondary",
                  },
                );
                if (!confirmed) return;

                const nextArchived = !act.isArchived;
                const res = await patchActivitiesById({
                  path: { id: act.id },
                  body: [
                    {
                      op: "replace",
                      path: "/isarchived",
                      value: nextArchived,
                    },
                  ],
                });
                if (res.error) {
                  toast.error(t("failed_updating"));
                  return;
                }
                setActivities((prev) => prev.filter((a) => a.id !== act.id));
                toast.success(
                  nextArchived
                    ? t("activity_archived")
                    : t("activity_unarchived"),
                );
              }}
            >
              {act.isArchived ? (
                <ArchiveRestore size={16} />
              ) : (
                <Archive size={16} />
              )}
            </Button>
          )}
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
          {isBoard && (
            <Button
              variant="danger"
              className="px-2"
              aria-label={t("delete_activity")}
              onClick={(e) => {
                e.stopPropagation();
                handleDeleteAdminActivity(act.id, confirm, () => {
                  setActivities((prev) => prev.filter((a) => a.id !== act.id));
                });
              }}
            >
              <Trash2Icon size={16} />
            </Button>
          )}
        </div>
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
              options={[
                { label: t("active_activities"), value: "active" },
                { label: t("archived_activities"), value: "archived" },
              ]}
              label={t("status")}
              style={{ minWidth: "160px" }}
              value={statusFilter}
              onChange={(e) =>
                setStatusFilter(e.target.value as "active" | "archived")
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

      {statusFilter === "archived" ? (
        <section className="flex flex-col gap-2">
          <div className="flex items-center gap-2">
            <h2 className="text-lg font-bold text-slate-800">
              {t("archived_activities")}
            </h2>
            <span className="bg-slate-100 text-slate-600 text-xs px-2 py-0.5 rounded-full font-bold">
              {filteredActivities.length}
            </span>
          </div>
          <BorderedTile className="bg-white p-0">
            <DataTable
              data={filteredActivities}
              columns={columns}
              emptyText={t("no_archived_activities")}
            />
          </BorderedTile>
        </section>
      ) : (
        <>
          <section className="flex flex-col gap-2">
            <div className="flex items-center gap-2">
              <h2 className="text-lg font-bold text-slate-800">
                {t("published_activities")}
              </h2>
              <span className="bg-slate-100 text-slate-600 text-xs px-2 py-0.5 rounded-full font-bold">
                {publishedActivities.length}
              </span>
            </div>
            <BorderedTile className="bg-white p-0">
              <DataTable
                data={publishedActivities}
                columns={columns}
                emptyText={t("no_published_activities")}
              />
            </BorderedTile>
          </section>

          <section className="flex flex-col gap-2">
            <div className="flex items-center gap-2">
              <h2 className="text-lg font-bold text-slate-800">
                {t("unpublished_activities")}
              </h2>
              <span className="bg-slate-100 text-slate-600 text-xs px-2 py-0.5 rounded-full font-bold">
                {unpublishedActivities.length}
              </span>
            </div>
            <BorderedTile className="bg-white p-0">
              <DataTable
                data={unpublishedActivities}
                columns={columns}
                emptyText={t("no_unpublished_activities")}
              />
            </BorderedTile>
          </section>
        </>
      )}

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
      {confirmModal}
    </div>
  );
}
