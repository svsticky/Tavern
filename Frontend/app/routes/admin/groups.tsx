import { t } from "i18next";
import { Mail, Phone, PlusIcon, TrendingUp } from "lucide-react";
import type { Column } from "~/components/Tiles/DataTableTile";
import DataTable from "~/components/Tiles/DataTableTile";
import Input from "~/components/UI/Input";
import { PageHeader } from "~/components/UI/PageHeader";
import Button from "~/components/UI/Button";
import { useEffect, useState } from "react";
import { useNavigate } from "react-router";
import { getApiGroups, type GroupResponseDto, type MemberResponseDto } from "~/api";
import BorderedTile from "~/components/Tiles/BorderedTile";
import toast from "react-hot-toast";
import CreateGroupOverlay from "~/components/Group/CreateGroupOverlay";
import Modal from "~/components/UI/Modal";

export default function Groups() {
  const navigate = useNavigate();

  const [loading, setLoading] = useState(true);
  const [groups, setGroups] = useState<GroupResponseDto[] | null>(null);
  const [filteredGroups, setFilteredGroups] = useState<GroupResponseDto[] | null>(null);
  const [searchQuery, setSearchQuery] = useState("");

  const [createGroupModalIsOpen, setCreateGroupModalIsOpen] = useState(false);

  useEffect(() => {
    const fetchGroups = async () => {
      try{
        setLoading(true);
        const response = await getApiGroups();
        
        if(response.data) {
          setGroups(response.data);
          setFilteredGroups(response.data);
        }

      } catch (error) {
        console.error("Error fetching groups:", error);
        toast.error(t("loading_failed"));
      } finally {
        setLoading(false);
      }
    };
    
    fetchGroups();
  }, []);

  useEffect(() => {
    if (!groups) return;

    const filtered = groups.filter((g) =>
      g.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
      g.type.toLowerCase().includes(searchQuery.toLowerCase())
    );

    setFilteredGroups(filtered);
  }, [searchQuery]);
  
  const columns: Column<GroupResponseDto>[] = [
    {
      header: t("name"),
      render: (g) => (
        <div className="flex items-center gap-2 text-slate-500">
          <span>{g.name}</span>
        </div>
      ),
    },
    {
      header: t("type"),
      render: (g) => (
        <div className="flex items-center gap-2 text-slate-500">
          <span>{g.type}</span>
        </div>
      ),
    },
    {
        header: "",
        className: "w-full sm:w-px whitespace-nowrap text-right",
        render: (group) => (
        <Button 
          variant="secondary"
          className="w-full sm:w-auto" 
          onClick={(e) => {
          e.stopPropagation();
          navigate(`/admin/groups/${group.id}`);
          }}
        >
            {t("view_group")}
        </Button>
        ),
    }
  ];

  return (
    <div className="flex flex-col gap-4 p-4">
      <PageHeader title={t("groups")} backTo="/"
        action={
          <Button 
            variant="secondary"
            onClick={() => setCreateGroupModalIsOpen(true)}
            className="flex items-center gap-2 px-3 py-1 rounded-lg transition-colors font-medium shadow-sm"
          >
            <PlusIcon className="w-5 h-5" />
          </Button>
        } />

      <BorderedTile>
        <div className="flex flex-col sm:flex-row items-center w-full gap-4">
          <div className="flex flex-col flex-1 w-full sm:w-auto">
            <Input label={t("search")} placeholder={t("search_groups")} className="bg-slate-100 w-full" onChange={(e: React.ChangeEvent<HTMLInputElement>) => setSearchQuery(e.target.value)} />
          </div>
        </div>
      </BorderedTile>

      {loading ?  "loading": (
        <BorderedTile className="bg-white p-0">
          <DataTable data={filteredGroups ?? []} columns={columns} />
        </BorderedTile>
      )}
      <Modal title={t("create_group")} isOpen={createGroupModalIsOpen} onClose={() => setCreateGroupModalIsOpen(false)}>
        <CreateGroupOverlay onSuccess={() => window.location.reload()} />
      </Modal>
    </div>
  );
}