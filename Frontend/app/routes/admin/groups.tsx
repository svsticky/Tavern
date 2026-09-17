import { t } from "i18next";
import { PlusIcon } from "lucide-react";
import { useEffect, useState } from "react";
import toast from "react-hot-toast";
import { useNavigate } from "react-router";
import { type GroupResponseDto, getGroups } from "~/api";
import CreateGroupOverlay from "~/components/Group/CreateGroupOverlay/CreateGroupOverlay";
import BorderedTile from "~/components/Tiles/BorderedTile";
import type { Column } from "~/components/Tiles/DataTableTile";
import DataTable from "~/components/Tiles/DataTableTile";
import Button from "~/components/UI/Button";
import Input from "~/components/UI/Input";
import Modal from "~/components/UI/Modal/Modal";
import { PageHeader } from "~/components/UI/PageHeader";
import { useAuth } from "~/context/AuthContext";
import type { TokenParsed } from "~/types/TokenParsed";
import { appendErrorMessage } from "~/util/error.util";
import { hasPermission, isBoardOrCandidateBoard } from "~/util/group.util";

/**
 * An administrative management page for viewing, filtering, and creating association groups.
 *
 * This component provides a high-level overview of all organizational entities (Committees,
 * Working Groups, etc.). It features:
 * - **Asynchronous Loading**: Fetches group data from the API on mount with error handling.
 * - **Dynamic Filtering**: Client-side search that filters groups by name or type.
 * - **Creation Workflow**: Integrated `Modal` and `CreateGroupOverlay` to add new groups
 *   without leaving the page.
 * - **Data Visualization**: Utilizes a `DataTable` for a clean, sortable overview of group metadata.
 * - **Navigation**: Direct access to detailed group management via the "View Group" action.
 *
 * @page
 * @component
 */
export default function Groups() {
  const navigate = useNavigate();
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

  const canManageGroups =
    isBoardOrCandidateBoard(tokenParsed) ||
    hasPermission(tokenParsed, "ManageGroups");

  const [loading, setLoading] = useState(true);
  const [groups, setGroups] = useState<GroupResponseDto[] | null>(null);
  const [filteredGroups, setFilteredGroups] = useState<
    GroupResponseDto[] | null
  >(null);
  const [searchQuery, setSearchQuery] = useState("");

  const [createGroupModalIsOpen, setCreateGroupModalIsOpen] = useState(false);

  useEffect(() => {
    const fetchGroups = async () => {
      try {
        setLoading(true);
        const response = await getGroups();

        if (response.error || !response.data) {
          throw response.error ?? new Error("Failed to fetch groups");
        }

        setGroups(response.data);
        setFilteredGroups(response.data);
      } catch (error) {
        console.error("Error fetching groups:", error);
        toast.error(appendErrorMessage(t("loading_failed"), error));
      } finally {
        setLoading(false);
      }
    };

    fetchGroups();
  }, []);

  useEffect(() => {
    if (!groups) return;

    const filtered = groups.filter(
      (g) =>
        g.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
        g.type.toLowerCase().includes(searchQuery.toLowerCase()),
    );

    setFilteredGroups(filtered);
  }, [searchQuery, groups]);

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
    },
  ];

  return (
    <div className="flex flex-col gap-4 p-4">
      <PageHeader
        title={t("groups")}
        backTo="/"
        action={
          canManageGroups && (
            <Button
              variant="secondary"
              onClick={() => setCreateGroupModalIsOpen(true)}
              className="items-center px-3 py-1"
            >
              <PlusIcon className="w-5 h-5" />
            </Button>
          )
        }
      />

      <BorderedTile>
        <div className="flex flex-col sm:flex-row items-center w-full gap-4">
          <div className="flex flex-col flex-1 w-full sm:w-auto">
            <Input
              label={t("search")}
              placeholder={t("search_groups")}
              onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
                setSearchQuery(e.target.value)
              }
            />
          </div>
        </div>
      </BorderedTile>

      {loading ? (
        t("loading")
      ) : (
        <BorderedTile className="bg-white p-0">
          <DataTable data={filteredGroups ?? []} columns={columns} />
        </BorderedTile>
      )}
      <Modal
        title={t("create_group")}
        isOpen={createGroupModalIsOpen}
        onClose={() => setCreateGroupModalIsOpen(false)}
      >
        <CreateGroupOverlay onSuccess={() => window.location.reload()} />
      </Modal>
    </div>
  );
}
