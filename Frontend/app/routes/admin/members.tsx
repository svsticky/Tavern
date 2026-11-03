import { t } from "i18next";
import { Mail, Phone, PlusIcon } from "lucide-react";
import { useState } from "react";
import { useLoaderData, useNavigate } from "react-router";
import type { MemberResponseDto } from "~/api";
import FilterMemberOverlay from "~/components/Member/FilterMemberOverlay/FilterMemberOverlay";
import StickyLoadingLogo from "~/components/StickyLoadingLogo";
import BorderedTile from "~/components/Tiles/BorderedTile";
import type { Column } from "~/components/Tiles/DataTableTile";
import DataTable from "~/components/Tiles/DataTableTile";
import Button from "~/components/UI/Button";
import InfiniteListFooter from "~/components/UI/InfiniteListFooter";
import Input from "~/components/UI/Input";
import Modal from "~/components/UI/Modal/Modal";
import { PageHeader } from "~/components/UI/PageHeader";
import { useInfiniteList } from "~/hooks/useInfiniteList";
import {
  useDebouncedSearchParam,
  useListSearchParams,
} from "~/hooks/useListSearchParams";
import type { MembersFilterDto } from "~/types/MembersFilterDto";
import {
  getLoadedPages,
  shouldRevalidateIgnoringPages,
} from "~/util/infiniteList.util";
import { requireBoardOrCandidateBoard } from "~/util/loaderAuth.util";
import type { Route } from "./+types/members";
import {
  fetchAdminMembers,
  MEMBERS_PAGE_SIZE,
  membersFilterFromSearchParams,
  membersFilterToSearchParams,
} from "./members.handlers";

/**
 * Loads the members matching the search and filters in the URL. When the URL
 * says more pages were loaded (after navigating back), it fetches all of them in
 * one request so the list, and with it the scroll position, is restored.
 * Only board and candidate board members can open this page.
 *
 * @param {Route.ClientLoaderArgs} args - The loader arguments, containing the request.
 * @throws {Error} When the members fail to load, which shows the route's error page.
 */
export async function clientLoader({ request }: Route.ClientLoaderArgs) {
  await requireBoardOrCandidateBoard();
  const url = new URL(request.url);
  const pages = getLoadedPages(url);
  const members = await fetchAdminMembers({
    search: url.searchParams.get("search") ?? "",
    filters: membersFilterFromSearchParams(url.searchParams),
    page: 1,
    pageSize: pages * MEMBERS_PAGE_SIZE,
  });
  return { members, pages };
}

/**
 * Loading more members only updates `pages` in the URL, which must not reload the list.
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
 * An administrative directory page for managing association members.
 *
 * Key Features:
 * - **Infinite Scrolling**: Loads the next page when the user reaches the end of
 *   the list (see `useInfiniteList`). The number of loaded pages, the search and
 *   the filters are kept in the URL, so going back to this page restores the list
 *   and its scroll position.
 * - **Debounced Search**: Waits 300ms after the last keystroke before triggering an API
 *   call to reduce server load.
 * - **Complex Filtering**: Supports advanced server-side filtering (Studies, Status,
 *   Suspension, etc.) via a Modal overlay.
 * - **Responsive Design**: Uses a data table that collapses or adjusts for mobile
 *   viewports with full-width buttons.
 *
 * @page
 * @component
 */
export default function Members() {
  const navigate = useNavigate();
  const loaderData = useLoaderData<typeof clientLoader>();
  const { searchParams, updateListParams } = useListSearchParams();
  const [searchQuery, setSearchQuery] = useDebouncedSearchParam();
  const [isFiltersOpen, setIsFiltersOpen] = useState(false);
  const filters = membersFilterFromSearchParams(searchParams);

  const {
    items: members,
    hasMore,
    loading,
    sentinelRef,
  } = useInfiniteList({
    initialItems: loaderData.members,
    initialPages: loaderData.pages,
    pageSize: MEMBERS_PAGE_SIZE,
    fetchPage: (page, pageSize) =>
      fetchAdminMembers({
        search: searchParams.get("search") ?? "",
        filters,
        page,
        pageSize,
      }),
  });

  /**
   * Stores the chosen filters in the URL, which reloads the list, and closes the overlay.
   *
   * @param {MembersFilterDto} newFilters - The chosen filters.
   */
  const applyFilters = (newFilters: MembersFilterDto) => {
    updateListParams((params) =>
      membersFilterToSearchParams(params, newFilters),
    );
    setIsFiltersOpen(false);
  };

  const columns: Column<MemberResponseDto>[] = [
    {
      header: t("name"),
      render: (m) => (
        <div className="flex items-center gap-2 text-slate-500">
          <span>
            {m.firstName} {m.lastName}
          </span>
        </div>
      ),
    },
    {
      header: t("email"),
      render: (m) => (
        <div className="flex items-center gap-2 text-slate-500">
          <Mail className="w-4 h-4" />
          <span>{m.email}</span>
        </div>
      ),
    },
    {
      header: t("phone"),
      render: (m) => (
        <div className="flex items-center gap-2 text-slate-500">
          <Phone className="w-4 h-4" />
          <span>{m.phoneNumber}</span>
        </div>
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
            navigate(`/admin/members/${act.id}`);
          }}
        >
          {t("view_member")}
        </Button>
      ),
    },
  ];

  return (
    <div className="flex flex-col gap-4 p-4">
      <PageHeader
        title={t("members")}
        backTo="/"
        action={
          <Button
            variant="secondary"
            onClick={() => navigate("/admin/members/create-member")}
            className="items-center px-3 py-1"
          >
            <PlusIcon className="w-5 h-5" />
          </Button>
        }
      />

      <BorderedTile>
        <div className="flex flex-col sm:flex-row items-end w-full gap-4">
          <div className="flex flex-col flex-1 w-full sm:w-auto">
            <Input
              label={t("search")}
              placeholder={t("search_members")}
              value={searchQuery}
              onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
                setSearchQuery(e.target.value)
              }
            />
          </div>

          <Button
            variant="secondary"
            className="w-full sm:w-auto"
            onClick={() => setIsFiltersOpen(true)}
          >
            {t("filters")}
          </Button>
        </div>
      </BorderedTile>

      <BorderedTile className="bg-white p-0">
        <DataTable data={members} columns={columns} emptyText="" />

        <InfiniteListFooter
          sentinelRef={sentinelRef}
          loading={loading}
          hasMore={hasMore}
          isEmpty={members.length === 0}
          noMoreText={t("no_more_members")}
        />
      </BorderedTile>

      <Modal
        isOpen={isFiltersOpen}
        onClose={() => setIsFiltersOpen(false)}
        title={t("filter_members")}
      >
        <FilterMemberOverlay filters={filters} onFilter={applyFilters} />
      </Modal>
    </div>
  );
}
