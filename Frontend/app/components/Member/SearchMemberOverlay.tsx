import { useState, useEffect } from "react";
import { t } from "i18next";
import { getApiMembers, postApiEnrollments, type Member, type MemberSummaryDto } from "~/api";
import Input from "~/components/UI/Input";
import Button from "~/components/UI/Button";

export default function SearchMemberOverlay({ selectText, onSelect, loading }: { selectText: string, onSelect: (member: Member) => void, loading: boolean }) {
  const [query, setQuery] = useState("");
  const [results, setResults] = useState<MemberSummaryDto[]>([]);
  const [searching, setSearching] = useState(true);

  useEffect(() => {
    const searchMembers = async () => {
      setSearching(true);
      try {
        const res = await getApiMembers({ query: { Search: query } });
        if (res.data) {
          setResults(res.data);
        }
      } catch (error) {
        console.error("Search error:", error);
      } finally {
        setSearching(false);
      }
    };

    const timer = setTimeout(searchMembers, 300); 
    return () => clearTimeout(timer);
  }, [query]);

  return (
    <div className="space-y-4">
      <Input 
        placeholder={t("search_member_placeholder")} 
        value={query} 
        onChange={(e: React.ChangeEvent<HTMLInputElement>) => setQuery(e.target.value)}
        autoFocus
      />
      
      <div className="divide-y max-h-[60vh] overflow-y-auto">
        {results.map(member => (
          <div key={member.id} className="py-3 flex justify-between items-center gap-4">
            <div className="min-w-0">
              <p className="font-medium truncate">{member.firstName} {member.lastName}</p>
            </div>
            <Button variant="secondary" onClick={() => onSelect({ street: "", email: "", phoneNumber: "", houseNumber: "", postalCode: "", city: "", ...member } as Member)} disabled={loading}>
              {selectText}
            </Button>
          </div>
        ))}
        {results.length === 0 && (searching ? (
          <p className="text-center py-4 text-slate-400 text-sm">{t("searching")}</p>
        ) : results.length === 0 && !searching && (
          <p className="text-center py-4 text-slate-400 text-sm">{t("no_members_found")}</p>
        ))}
      </div>
    </div>
  );
}