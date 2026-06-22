import { t } from "i18next";
import { useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router";
import type { ActivityResponseDto } from "~/api";
import BorderedTile from "~/components/Tiles/BorderedTile";
import type { Column } from "~/components/Tiles/DataTableTile";
import DataTable from "~/components/Tiles/DataTableTile";
import Button from "~/components/UI/Button";
import Input from "~/components/UI/Input";
import { PageHeader } from "~/components/UI/PageHeader/PageHeader";
import Select from "~/components/UI/Select";
import { formatDate, getFinancialYear } from "~/util/date.util";
import { handleViewActivity, loadAdminActivities } from "./activities.handlers";

/**
 * An administrative management page for viewing and filtering all association activities.
 *
 * This component provides a robust interface for board members to track events across
 * different association years. It features:
 * - **Yearly Archiving**: A selector to view activities as far back as 2007.
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

  const [loading, setLoading] = useState(true);
  const currentYear = getFinancialYear();
  const [year, setYear] = useState(currentYear);
  const [activities, setActivities] = useState<ActivityResponseDto[] | null>(
    null,
  );
  const [searchQuery, setSearchQuery] = useState("");

  const yearsSince2007 = Array.from(
    { length: currentYear - 2007 + 1 },
    (_, i) => currentYear - i,
  );

  const filteredActivities = useMemo(() => {
    if (!activities) return [];
    return activities.filter(
      (act) =>
        act.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
        act.location?.toLowerCase().includes(searchQuery.toLowerCase()),
    );
  }, [activities, searchQuery]);

  useEffect(() => {
    loadAdminActivities(year, setLoading, (next) => setActivities(next));
  }, [year]);

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

      {loading ? (
        t("loading")
      ) : (
        <BorderedTile className="bg-white p-0">
          <DataTable data={filteredActivities ?? []} columns={columns} />
        </BorderedTile>
      )}
    </div>
  );
}
