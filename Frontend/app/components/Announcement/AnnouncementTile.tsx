import { t } from "i18next";
import { Calendar, Megaphone, PencilIcon } from "lucide-react";
import { useEffect, useState } from "react";
import { useNavigate } from "react-router";
import type { GetAnnouncementResponseDto } from "~/api";
import Markdown from "~/components/UI/Markdown";
import { useApp } from "~/context/AppContext";
import { useAuth } from "~/context/AuthContext";
import type { TokenParsed } from "~/types/TokenParsed";
import { formatDate } from "~/util/date.util";
import { isBoardOrCandidateBoard } from "~/util/group.util";
import { cn } from "~/util/tailwind.util";
import Tile from "../Tiles/Tile";

/**
 * A detailed tile component for displaying an individual association announcement.
 *
 * Features:
 * - **Markdown Support**: Renders the announcement body text using a specialized
 *   `Markdown` component with tailored typography styles.
 * - **Administrative Controls**: Automatically displays a floating edit button if the
 *   authenticated user is a member of the Board or Candidate Board.
 * - **Metadata Display**: Shows the creation date and the name of the announcer
 *   with accompanying icons for better scannability.
 * - **Responsive Design**: Uses a flexible header layout that ensures the title
 *   and date/controls do not overlap on smaller screens.
 *
 * @component
 * @param {Object} props - The component props.
 * @param {GetAnnouncementResponseDto} props.announcement - The announcement data object including title, content, and metadata.
 * @param {string} [props.className] - Optional CSS classes to override or extend the root container styling.
 *
 * @example
 * ```tsx
 * <AnnouncementTile
 *   announcement={announcementData}
 *   className="shadow-lg"
 * />
 * ```
 */
export default function AnnouncementTile({
  announcement,
  className,
}: {
  announcement: GetAnnouncementResponseDto;
  className?: string;
}) {
  const authService = useAuth();
  const { boardGroupId, candidateBoardGroupId } = useApp();
  const navigate = useNavigate();
  const [tokenParsed, setTokenParsed] = useState<TokenParsed | null>(null);

  useEffect(() => {
    let cancelled = false;
    const loadToken = async () => {
      const token = await authService.getTokenParsed();
      if (!cancelled) {
        setTokenParsed(token);
      }
    };
    loadToken();
    return () => {
      cancelled = true;
    };
  }, [authService]);

  const isBoard = isBoardOrCandidateBoard(
    tokenParsed,
    boardGroupId,
    candidateBoardGroupId,
  );

  return (
    <Tile className={cn("border border-gray-200 p-6", className)}>
      {/* Header: Title (Links) | Date & Edit (Rechts) */}
      <div className="flex w-full justify-between items-start mb-4 gap-4">
        <h3 className="font-bold text-lg leading-tight">
          {announcement.title}
        </h3>

        <div className="flex items-center gap-3 shrink-0">
          <p className="flex items-center gap-1 text-sm text-gray-500 font-medium whitespace-nowrap">
            <Calendar className="w-4 h-4" />
            {formatDate(new Date(announcement.createdAt), "defaultDate")}
          </p>

          {isBoard && (
            <button
              onClick={() => navigate(`/announcements/edit/${announcement.id}`)}
              className="p-1.5 rounded-lg bg-gray-50 hover:bg-gray-100 text-gray-400 hover:text-(--board-primary) transition-colors border border-gray-100 hover:cursor-pointer"
              title={t("edit")}
            >
              <PencilIcon className="w-4 h-4" />
            </button>
          )}
        </div>
      </div>

      {/* Announcement content */}
      <div className="prose prose-sm max-w-none mb-4">
        <Markdown>{announcement.content}</Markdown>
      </div>

      {/* Divider */}
      <div className="my-4 h-[1px] w-full bg-gray-100" />

      {/* Announcer */}
      <div className="flex items-center">
        <p className="flex items-center gap-2 text-sm text-gray-600 font-semibold">
          <Megaphone className="w-4 h-4 text-(--board-primary)" />
          {announcement.createdByName}
        </p>
      </div>
    </Tile>
  );
}
