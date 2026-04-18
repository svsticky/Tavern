import { t } from "i18next";
import type { Column } from "~/components/Tiles/DataTableTile";
import DataTable from "~/components/Tiles/DataTableTile";
import Input from "~/components/UI/Input";
import { PageHeader } from "~/components/UI/PageHeader";
import { act, use, useEffect, useMemo, useState } from "react";
import { getApiActivities, type ActivityResponseDto } from "~/api";
import { formatDate, getAssociationYear } from "~/util/date.util";
import { useNavigate } from "react-router";
import Button from "~/components/UI/Button";
import BorderedTile from "~/components/Tiles/BorderedTile";
import toast from "react-hot-toast";
import Select from "~/components/UI/Select";

export default function Activities() {
  const navigate = useNavigate();

  const [loading, setLoading] = useState(true);
  const currentYear = getAssociationYear();
  const [year, setYear] = useState(currentYear);
  const [activities, setActivities] = useState<ActivityResponseDto[] | null>(null);
  const [searchQuery, setSearchQuery] = useState("");

  const yearsSince2007 = Array.from({ length: currentYear - 2007 + 1 }, (_, i) => currentYear - i);

  const filteredActivities = useMemo(() => {
    if (!activities) return [];
    return activities.filter((act) =>
      act.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
      act.location?.toLowerCase().includes(searchQuery.toLowerCase())
    );
  }, [activities, searchQuery]);

  useEffect(() => {
    const fetchActivities = async () => {
      try{
        setLoading(true);
        const response = await getApiActivities({
          query: {
            Year: year
          }
        });

        if(response.data) {
          setActivities(response.data);
        }
      } catch (error) {
        console.error("Error fetching activities:", error);
        toast.error(t("loading_failed"));
      } finally {
        setLoading(false);
      }
    };
    
    fetchActivities();
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
          <span className="text-sm text-slate-600">{formatDate(new Date(act.dateTimeStart), "fullDateTime")}</span>
        </div>
      ),
    },
    {
      header: t("participants"),
      render: (act) => (
        <div className="flex flex-col">
          <span className="text-sm text-slate-600">👥 {act.enrollments.filter(e => !e.isOnWaitingList).length}{act.participantLimit !== null ? `/${act.participantLimit}` : ""}</span>
        </div>
      ),
    },
    {
      header: t("price"),
      render: (act) => <span className="font-medium text-slate-700">{act.price != null && act.price > 0 ? `€${act.price.toFixed(2)}` : t("free")}</span>,
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
            navigate(`/admin/activities/${act.id}`);
          }}
        >
          {t("view_activity")}
        </Button>
      ),
    }
  ];

  return (
    <div className="flex flex-col gap-4 p-4">
      <PageHeader title={t("activities")} backTo="/" />

      <BorderedTile>
        <div className="flex flex-col sm:flex-row items-center w-full gap-4">
          <div className="flex flex-col flex-1 w-full sm:w-auto">
            <span className="text-sm text-slate-400">{t("search")}</span>
            <Input placeholder={t("search_activities")} className="bg-slate-100 w-full" onChange={(e: React.ChangeEvent<HTMLInputElement>) => setSearchQuery(e.target.value)} />
          </div>
          <div className="flex flex-col w-full sm:w-auto">
            <Select options={yearsSince2007.map((y) => ({ label: `${t("season")} ${y - 1}/${y}`, value: y }))} label={t("year")} style={{ minWidth: "150px" }} value={year} onChange={(e) => setYear(Number(e.target.value))}>
            </Select>
          </div>
        </div>
      </BorderedTile>

      {loading ? t("loading") : (
        <BorderedTile className="bg-white p-0">
          <DataTable data={filteredActivities ?? []} columns={columns} />
        </BorderedTile>
      )}
    </div>
  );
}