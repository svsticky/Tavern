import { t } from "i18next";
import { Mail, Phone, PlusIcon } from "lucide-react";
import { useCallback, useEffect, useRef, useState } from "react";
import toast from "react-hot-toast";
import { useLoaderData, useNavigate } from "react-router";
import type { MemberResponseDto } from "~/api";
import FilterMemberOverlay from "~/components/Member/FilterMemberOverlay/FilterMemberOverlay";
import StickyLoadingLogo from "~/components/StickyLoadingLogo";
import BorderedTile from "~/components/Tiles/BorderedTile";
import type { Column } from "~/components/Tiles/DataTableTile";
import DataTable from "~/components/Tiles/DataTableTile";
import Button from "~/components/UI/Button";
import Input from "~/components/UI/Input";
import Modal from "~/components/UI/Modal/Modal";
import { PageHeader } from "~/components/UI/PageHeader";
import type { MembersFilterDto } from "~/types/MembersFilterDto";
import { appendErrorMessage } from "~/util/error.util";
import { requireTokenParsed } from "~/util/loaderAuth.util";
import { fetchMembersPage, PAGE_SIZE } from "./members.handlers";

type LoaderData = {
  members: MemberResponseDto[];
  hasMore: boolean;
};

/**
 * Fetches the first, unfiltered page of members before the route renders.
 * Search text and the filter panel stay local component state (not reflected
 * in the URL) exactly as before - only the initial mount's fetch moves ahead
 * of render here, to fix scroll restoration and drop the loading flash.
 */
export async function clientLoader(): Promise<LoaderData> {
  await requireTokenParsed();
  const members = await fetchMembersPage(1, "", null);
  return { members, hasMore: members.length === PAGE_SIZE };
}

export function HydrateFallback() {
  return <StickyLoadingLogo />;
}

/**
 * An administrative directory page for managing association members.
 *
 * Key Features:
 * - **Infinite Scrolling**: Uses the `IntersectionObserver` API to detect when the user
 *   has reached the end of the list and automatically fetches the next page.
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
  const loaderData = useLoaderData<typeof clientLoader>();
  const navigate = useNavigate();
  const [loading, setLoading] = useState(false);
  const [members, setMembers] = useState<MemberResponseDto[]>(
    loaderData.members,
  );
  const [searchQuery, setSearchQuery] = useState("");
  const [debouncedSearchQuery, setDebouncedSearchQuery] = useState("");
  const [isFiltersOpen, setIsFiltersOpen] = useState(false);
  const [filters, setFilters] = useState<MembersFilterDto | null>(null);

  const [page, setPage] = useState(1);
  const [hasMore, setHasMore] = useState(loaderData.hasMore);
  const loaderRef = useRef<HTMLDivElement>(null);
  const isInitialMount = useRef(true);

  const fetchMembers = useCallback(
    async (pageNum: number, search: string, isInitial: boolean) => {
      try {
        setLoading(true);
        const data = await fetchMembersPage(pageNum, search, filters);

        setMembers((prev) => (isInitial ? data : [...prev, ...data]));

        if (data.length < PAGE_SIZE) {
          setHasMore(false);
        }
      } catch (error) {
        console.error("Error fetching members:", error);
        toast.error(appendErrorMessage(t("loading_failed"), error));
      } finally {
        setLoading(false);
      }
    },
    [filters],
  );

  const applyFilters = (newFilters: MembersFilterDto) => {
    setFilters(newFilters);
    setIsFiltersOpen(false);
  };

  useEffect(() => {
    const handler = setTimeout(() => {
      setDebouncedSearchQuery(searchQuery);
    }, 300);

    return () => clearTimeout(handler);
  }, [searchQuery]);

  // The loader already fetched page 1 with no search/filters for the initial
  // mount - skip that first run so it isn't immediately refetched, and only
  // react to an actual later change to search or filters.
  useEffect(() => {
    if (isInitialMount.current) {
      isInitialMount.current = false;
      return;
    }
    setPage(1);
    setHasMore(true);
    fetchMembers(1, debouncedSearchQuery, true);
  }, [debouncedSearchQuery, fetchMembers]);

  useEffect(() => {
    const observer = new IntersectionObserver(
      (entries) => {
        if (entries[0].isIntersecting && hasMore && !loading) {
          const nextPage = page + 1;
          setPage(nextPage);
          fetchMembers(nextPage, debouncedSearchQuery, false);
        }
      },
      { threshold: 1.0 },
    );

    if (loaderRef.current) {
      observer.observe(loaderRef.current);
    }

    return () => observer.disconnect();
  }, [hasMore, loading, page, debouncedSearchQuery, fetchMembers]);

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

        <div ref={loaderRef} className="h-10 flex items-center justify-center">
          <span className="text-slate-400 text-sm">
            {loading
              ? t("loading_more")
              : hasMore
                ? t("load_more")
                : members.length === 0
                  ? t("no_data")
                  : t("no_more_members")}
          </span>
        </div>
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
