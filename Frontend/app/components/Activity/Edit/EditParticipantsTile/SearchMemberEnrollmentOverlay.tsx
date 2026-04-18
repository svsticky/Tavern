import { useState, useEffect } from "react";
import { t } from "i18next";
import toast from "react-hot-toast";
import { getApiMembers, postApiEnrollments, type Member, type MemberSummaryDto } from "~/api";
import Input from "~/components/UI/Input";
import Button from "~/components/UI/Button";

export default function SearchMemberEnrollmentOverlay({ activityId, onEnrolled, onClose }: { activityId: number, onEnrolled: (member: Member, isOnWaitingList: boolean, price: number) => void, onClose: () => void }) {
  const [query, setQuery] = useState("");
  const [results, setResults] = useState<MemberSummaryDto[]>([]);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    const searchMembers = async () => {
      if (query.length < 2) {
        setResults([]);
        return;
      }
      const res = await getApiMembers({ query: { Search: query } });
      if (res.data) setResults(res.data);
    };

    const timer = setTimeout(searchMembers, 300); 
    return () => clearTimeout(timer);
  }, [query]);

  const handleEnroll = async (member: Member) => {
    setLoading(true); 
    try {
      await postApiEnrollments({
        body: { activityId, memberId: member.id }
      });
      toast.success(t("member_enrolled_success"));
      onEnrolled(member, false, 0);
      onClose();
    } catch (err) {
      toast.error(t("enroll_failed"));
    } finally {
      setLoading(false);
    }
  };

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
            <Button variant="secondary" onClick={() => handleEnroll({ street: "", email: "", phoneNumber: "", houseNumber: "", postalCode: "", city: "", ...member } as Member)} disabled={loading}>
              {t("enroll")}
            </Button>
          </div>
        ))}
        {query.length >= 2 && results.length === 0 && (
          <p className="text-center py-4 text-slate-400 text-sm">{t("no_members_found")}</p>
        )}
      </div>
    </div>
  );
}