import { t } from "i18next";
import { useEffect, useState } from "react";
import toast from "react-hot-toast";
import { getApiMembers, type MemberResponseDto } from "~/api";
import Button from "~/components/UI/Button";
import Input from "~/components/UI/Input";

/**
 * A modal overlay component for searching and selecting members from the API.
 * It features a debounced search input to minimize API calls and displays
 * a scrollable list of member results.
 *
 * @component
 * @example
 * ```tsx
 * <SearchMemberOverlay
 *   selectText={t("add_member")}
 *   onSelect={(member) => handleAdd(member)}
 *   loading={isSubmitting}
 * />
 * ```
 */
export default function SearchMemberOverlay({
  selectText,
  onSelect,
  loading,
}: {
  selectText: string;
  onSelect: (member: MemberResponseDto) => void;
  loading: boolean;
}) {
  const [query, setQuery] = useState("");
  const [results, setResults] = useState<MemberResponseDto[]>([]);
  const [searching, setSearching] = useState(true);

  useEffect(() => {
    const searchMembers = async () => {
      setSearching(true);
      try {
        const res = await getApiMembers({ query: { Search: query } });

        if (res.error || !res.data) throw new Error("Search failed");

        setResults(res.data);
      } catch (error) {
        console.error("Search error:", error);
        toast.error(t("search_failed"));
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
        onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
          setQuery(e.target.value)
        }
        autoFocus
      />

      <div className="divide-y max-h-[60vh] overflow-y-auto">
        {results.map((member) => (
          <div
            key={member.id}
            className="py-3 flex justify-between items-center gap-4"
          >
            <div className="min-w-0">
              <p className="font-medium truncate">
                {member.firstName} {member.lastName}
              </p>
            </div>
            <Button
              variant="secondary"
              onClick={() => onSelect(member)}
              disabled={loading}
            >
              {selectText}
            </Button>
          </div>
        ))}
        {results.length === 0 &&
          (searching ? (
            <p className="text-center py-4 text-slate-400 text-sm">
              {t("searching")}
            </p>
          ) : (
            results.length === 0 &&
            !searching && (
              <p className="text-center py-4 text-slate-400 text-sm">
                {t("no_members_found")}
              </p>
            )
          ))}
      </div>
    </div>
  );
}
