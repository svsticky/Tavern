import { t } from "i18next";
import { useCallback, useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router";
import type { PagedResultDtoActivityListItemDto } from "~/api";
import BorderedTile from "~/components/Tiles/BorderedTile";
import type { Column } from "~/components/Tiles/DataTableTile";
import DataTable from "~/components/Tiles/DataTableTile";
import Button from "~/components/UI/Button";
import Input from "~/components/UI/Input";
import { PageHeader } from "~/components/UI/PageHeader/PageHeader";
import Select from "~/components/UI/Select";
import { formatDate, getCommitteeYear } from "~/util/date.util";
import type { ActivityListItemDto } from "./activities.handlers";
import { handleViewActivity, loadAdminActivities } from "./activities.handlers";

const ALL_YEARS = 0;
const PAGE_SIZE = 50;

export default function Activities() {
  const navigate = useNavigate();

  const [loading, setLoading] = useState(true);
  const currentYear = getCommitteeYear();
  const [year, setYear] = useState(currentYear);
  const [result, setResult] = useState<PagedResultDtoActivityListItemDto | null>(null);
  const [searchInput, setSearchInput] = useState("");
  const [search, setSearch] = useState("");
  const [sortBy, setSortBy] = useState("date");
  const [sortDir, setSortDir] = useState<"asc" | "desc">("desc");
  const [page, setPage] = useState(1);

  const debounceTimer = useRef<ReturnType<typeof setTimeout> | null>(null);

  const yearOptions = [
    { label: t("all_time"), value: ALL_YEARS },
    ...Array.from({ length: currentYear - 2007 + 1 }, (_, i) => {
      const y = currentYear - i;
      return { label: `${y - 1}/${y}`, value: y };
    }),
  ];

  const fetch = useCallback(() => {
    loadAdminActivities(
      {
        year: year === ALL_YEARS ? null : year,
        search,
        sortBy,
        sortDir,
        page,
        pageSize: PAGE_SIZE,
      },
      setLoading,
      setResult,
    );
  }, [year, search, sortBy, sortDir, page]);

  useEffect(() => {
    fetch();
  }, [fetch]);

  const handleSearchChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setSearchInput(e.target.value);
    if (debounceTimer.current) clearTimeout(debounceTimer.current);
    debounceTimer.current = setTimeout(() => {
      setSearch(e.target.value);
      setPage(1);
    }, 400);
  };

  const handleSort = (by: string) => {
    if (sortBy === by) {
      setSortDir((d) => (d === "asc" ? "desc" : "asc"));
    } else {
      setSortBy(by);
      setSortDir("asc");
    }
    setPage(1);
  };

  const activities = result?.items ?? [];
  const totalCount = result?.totalCount ?? 0;
  const totalPages = Math.ceil(totalCount / PAGE_SIZE);

  const sortIcon = (col: string) => {
    if (sortBy !== col) return " ⇅";
    return sortDir === "asc" ? " ▲" : " ▼";
  };

  const columns: Column<ActivityListItemDto>[] = [
    {
      header: (
        <button
          type="button"
          className="font-medium hover:text-slate-600 cursor-pointer select-none"
          onClick={() => handleSort("name")}
        >
          {t("activity")}{sortIcon("name")}
        </button>
      ),
      render: (act) => (
        <div className="flex flex-col">
          <span className="font-semibold text-slate-700">{act.name}</span>
          <span className="text-xs text-slate-400">{act.location}</span>
        </div>
      ),
    },
    {
      header: (
        <button
          type="button"
          className="font-medium hover:text-slate-600 cursor-pointer select-none"
          onClick={() => handleSort("date")}
        >
          {t("date")}{sortIcon("date")}
        </button>
      ),
      render: (act) => (
        <span className="text-sm text-slate-600">
          {formatDate(new Date(act.dateTimeStart), "fullDateTime")}
        </span>
      ),
    },
    {
      header: (
        <button
          type="button"
          className="font-medium hover:text-slate-600 cursor-pointer select-none"
          onClick={() => handleSort("participants")}
        >
          {t("participants")}{sortIcon("participants")}
        </button>
      ),
      render: (act) => {
        const count = act.enrolledCount;
        const limit = act.participantLimit;
        const waitlist = act.waitlistCount;
        const full = limit !== null && limit !== undefined && count >= limit;
        return (
          <div className="flex flex-col">
            <span className={`text-sm ${full ? "text-amber-600 font-medium" : "text-slate-600"}`}>
              {count}{limit != null ? `/${limit}` : ""}
              {full ? ` · ${t("full")}` : ""}
            </span>
            {waitlist > 0 && (
              <span className="text-xs text-slate-400">{waitlist} {t("on_waitlist")}</span>
            )}
          </div>
        );
      },
    },
    {
      header: (
        <button
          type="button"
          className="font-medium hover:text-slate-600 cursor-pointer select-none"
          onClick={() => handleSort("price")}
        >
          {t("price")}{sortIcon("price")}
        </button>
      ),
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
              onChange={handleSearchChange}
            />
          </div>
          <div className="flex flex-col w-full sm:w-auto">
            <Select
              options={yearOptions}
              label={t("year")}
              style={{ minWidth: "150px" }}
              value={year}
              onChange={(e) => {
                setYear(Number(e.target.value));
                setPage(1);
              }}
            />
          </div>
        </div>
      </BorderedTile>

      {loading ? (
        t("loading")
      ) : (
        <>
          <BorderedTile className="bg-white p-0">
            <DataTable
              data={activities}
              columns={columns}
            />
          </BorderedTile>

          {totalPages > 1 && (
            <div className="flex items-center justify-between px-2">
              <span className="text-sm text-slate-500">
                {(page - 1) * PAGE_SIZE + 1}–{Math.min(page * PAGE_SIZE, totalCount)} {t("of")} {totalCount}
              </span>
              <div className="flex gap-2">
                <Button
                  variant="secondary"
                  onClick={() => setPage((p) => Math.max(1, p - 1))}
                  disabled={page <= 1}
                >
                  ‹ {t("previous")}
                </Button>
                <Button
                  variant="secondary"
                  onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                  disabled={page >= totalPages}
                >
                  {t("next")} ›
                </Button>
              </div>
            </div>
          )}
        </>
      )}
    </div>
  );
}
