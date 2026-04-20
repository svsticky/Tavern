import { t } from "i18next";
import { Mail, Phone, TrendingUp } from "lucide-react";
import type { Column } from "~/components/Tiles/DataTableTile";
import DataTable from "~/components/Tiles/DataTableTile";
import Input from "~/components/UI/Input";
import { PageHeader } from "~/components/UI/PageHeader";
import Button from "~/components/UI/Button";
import { useCallback, useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router";
import { getApiMembers, type MemberResponseDto } from "~/api";
import BorderedTile from "~/components/Tiles/BorderedTile";
import toast from "react-hot-toast";
import Modal from "~/components/UI/Modal";
import FilterMemberOverlay from "~/components/Member/FilterMemberOverlay";
import type { MembersFilterDto } from "~/types/MembersFilterDto";

const PAGE_SIZE = 20;

export default function Members() {
  const navigate = useNavigate();
  const [loading, setLoading] = useState(false);
  const [members, setMembers] = useState<MemberResponseDto[]>([]);
  const [searchQuery, setSearchQuery] = useState("");
  const [debouncedSearchQuery, setDebouncedSearchQuery] = useState("");
  const [isFiltersOpen, setIsFiltersOpen] = useState(false);
  const [filters, setFilters] = useState<MembersFilterDto | null>(null);
  
  const [page, setPage] = useState(1);
  const [hasMore, setHasMore] = useState(true);
  const loaderRef = useRef<HTMLDivElement>(null);

  const fetchMembers = useCallback(async (pageNum: number, search: string, isInitial: boolean) => {
    try {
      setLoading(true);
      const response = await getApiMembers({ 
        query: { Page: pageNum, PageSize: PAGE_SIZE, Search: search, 
          StudyId: filters?.studyId || undefined, 
          Gratie: filters?.gratie || undefined, 
          LidVanVerdienste: filters?.lidVanVerdienste || undefined, 
          EreLid: filters?.ereLid || undefined, 
          Begunstiger: filters?.begunstiger || undefined, 
          Suspended: filters?.suspended || undefined, 
          Inactive: filters?.inactive || undefined,
          StudyType: filters?.studyType || undefined }
      });
      
      if (response.data) {
        setMembers(prev => isInitial ? response.data! : [...prev, ...response.data!]);
        
        if (response.data.length < PAGE_SIZE) {
          setHasMore(false);
        }
      }
    } 
    catch (error) {
      console.error("Error fetching members:", error);
      toast.error(t("loading_failed"));
    } finally {
      setLoading(false);
    }
  }, [filters]);

  const applyFilters = (newFilters: MembersFilterDto) => {
    console.log(newFilters);
    setFilters(newFilters);
    console.log(filters);
    setIsFiltersOpen(false);
  };

  useEffect(() => {
    const handler = setTimeout(() => {
      setDebouncedSearchQuery(searchQuery);
    }, 300);

    return () => clearTimeout(handler);
  }, [searchQuery]);

  useEffect(() => {
    setPage(1);
    setHasMore(true);
    fetchMembers(1, debouncedSearchQuery, true);
  }, [debouncedSearchQuery, filters, fetchMembers]);

  useEffect(() => {
    const observer = new IntersectionObserver(
      (entries) => {
        if (entries[0].isIntersecting && hasMore && !loading) {
          const nextPage = page + 1;
          setPage(nextPage);
          fetchMembers(nextPage, debouncedSearchQuery, false);
        }
      },
      { threshold: 1.0 }
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
          <span>{m.firstName} {m.lastName}</span>
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
    }
  ];

  return (
    <div className="flex flex-col gap-4 p-4">
      <PageHeader title={t("members")} backTo="/" />

      <BorderedTile>
        <div className="flex flex-col sm:flex-row items-end w-full gap-4">
          
          <div className="flex flex-col flex-1 w-full sm:w-auto">
            <Input 
              label={t("search")}
              placeholder={t("search_members")} 
              onChange={(e: React.ChangeEvent<HTMLInputElement>) => setSearchQuery(e.target.value)} 
            />
          </div>

          <Button variant="secondary" className="h-[42px]" onClick={() => setIsFiltersOpen(true)}>
            {t("filters")}
          </Button>
          
        </div>
      </BorderedTile>

      <BorderedTile className="bg-white p-0">
        <DataTable data={members} columns={columns} emptyText="" />
        
        <div ref={loaderRef} className="h-10 flex items-center justify-center">
          <span className="text-slate-400 text-sm">{loading ? t("loading_more") : hasMore ? t("load_more") : members.length === 0 ? t("no_data") : t("no_more_members")}</span>
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