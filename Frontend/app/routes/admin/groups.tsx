import { t } from "i18next";
import { PlusIcon } from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import {
  useLoaderData,
  useNavigate,
  useRevalidator,
  useSearchParams,
} from "react-router";
import { type GroupResponseDto, getGroups } from "~/api";
import CreateGroupOverlay from "~/components/Group/CreateGroupOverlay/CreateGroupOverlay";
import StickyLoadingLogo from "~/components/StickyLoadingLogo";
import BorderedTile from "~/components/Tiles/BorderedTile";
import type { Column } from "~/components/Tiles/DataTableTile";
import DataTable from "~/components/Tiles/DataTableTile";
import Button from "~/components/UI/Button";
import Input from "~/components/UI/Input";
import Modal from "~/components/UI/Modal/Modal";
import { PageHeader } from "~/components/UI/PageHeader";
import { shouldRevalidateIgnoring } from "~/util/infiniteList.util";
import { requireTokenParsed } from "~/util/loaderAuth.util";

const SEARCH_PARAM = "q";

export async function clientLoader(): Promise<{ groups: GroupResponseDto[] }> {
  await requireTokenParsed();

  const response = await getGroups();
  if (response.error || !response.data) {
    throw response.error ?? new Error("Failed to fetch groups");
  }

  return { groups: response.data };
}

/** Filtering is client-side, so typing must not refetch; the text is in the URL only so back-navigation restores it. */
export const shouldRevalidate = shouldRevalidateIgnoring(SEARCH_PARAM);

export function HydrateFallback() {
  return <StickyLoadingLogo />;
}

/**
 * An administrative management page for viewing, filtering, and creating association groups.
 *
 * This component provides a high-level overview of all organizational entities (Committees,
 * Working Groups, etc.). It features:
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
  const { groups } = useLoaderData<typeof clientLoader>();
  const navigate = useNavigate();
  const revalidator = useRevalidator();

  const [searchParams, setSearchParams] = useSearchParams();
  const urlSearch = searchParams.get(SEARCH_PARAM) ?? "";
  const [searchQuery, setSearchQuery] = useState(urlSearch);
  const [createGroupModalIsOpen, setCreateGroupModalIsOpen] = useState(false);

  useEffect(() => {
    const handler = setTimeout(() => {
      if (searchQuery === urlSearch) return;
      const next = new URLSearchParams(searchParams);
      if (searchQuery) {
        next.set(SEARCH_PARAM, searchQuery);
      } else {
        next.delete(SEARCH_PARAM);
      }
      setSearchParams(next, { replace: true, preventScrollReset: true });
    }, 300);

    return () => clearTimeout(handler);
  }, [searchQuery, urlSearch, searchParams, setSearchParams]);

  const filteredGroups = useMemo(
    () =>
      groups.filter(
        (g) =>
          g.name.toLowerCase().includes(searchQuery.toLowerCase()) ||
          g.type.toLowerCase().includes(searchQuery.toLowerCase()),
      ),
    [groups, searchQuery],
  );

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

      <BorderedTile className="bg-white p-0">
        <DataTable data={filteredGroups} columns={columns} />
      </BorderedTile>
      <Modal
        title={t("create_group")}
        isOpen={createGroupModalIsOpen}
        onClose={() => setCreateGroupModalIsOpen(false)}
      >
        <CreateGroupOverlay
          onSuccess={() => {
            setCreateGroupModalIsOpen(false);
            revalidator.revalidate();
          }}
        />
      </Modal>
    </div>
  );
}
