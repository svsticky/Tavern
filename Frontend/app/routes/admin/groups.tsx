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
import { usePersistentPageState } from "~/hooks/usePersistentPageState";
import { useScrollRestoration } from "~/hooks/useScrollRestoration";
import { appendErrorMessage } from "~/util/error.util";

type GroupsPageState = {
  groups: GroupResponseDto[] | null;
  filteredGroups: GroupResponseDto[] | null;
  searchQuery: string;
};

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

  const { initial, isRestored, save } = usePersistentPageState<GroupsPageState>(
    () => ({ groups: null, filteredGroups: null, searchQuery: "" }),
  );

  const [loading, setLoading] = useState(!isRestored);
  const [groups, setGroups] = useState<GroupResponseDto[] | null>(
    initial.groups,
  );
  const [filteredGroups, setFilteredGroups] = useState<
    GroupResponseDto[] | null
  >(initial.filteredGroups);
  const [searchQuery, setSearchQuery] = useState(initial.searchQuery);

  const [createGroupModalIsOpen, setCreateGroupModalIsOpen] = useState(false);

  useEffect(() => {
    if (isRestored) return;

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
  }, [isRestored]);

  useEffect(() => {
    if (!groups) return;

    const filtered = groups.filter(
      (g) =>
        g.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
        g.type.toLowerCase().includes(searchQuery.toLowerCase()),
    );

    setFilteredGroups(filtered);
  }, [searchQuery, groups]);

  useEffect(() => {
    if (loading) return;
    save({ groups, filteredGroups, searchQuery });
  }, [loading, groups, filteredGroups, searchQuery, save]);

  useScrollRestoration(!loading);

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
          <Button
            variant="secondary"
            onClick={() => setCreateGroupModalIsOpen(true)}
            className="items-center px-3 py-1"
          >
            <PlusIcon className="w-5 h-5" />
          </Button>
        }
      />

      <BorderedTile>
        <div className="flex flex-col sm:flex-row items-center w-full gap-4">
          <div className="flex flex-col flex-1 w-full sm:w-auto">
            <Input
              label={t("search")}
              placeholder={t("search_groups")}
              value={searchQuery}
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
