import { Calendar, Megaphone, PencilIcon } from "lucide-react";
import { formatDate } from "~/util/date.util";
import { cn } from "~/util/tailwind.util";
import Tile from "./Tile";
import Markdown from "~/components/UI/Markdown";
import { useKeycloak } from "@react-keycloak/web";
import { isInGroupWithId } from "~/util/group.util";
import { useNavigate } from "react-router";
import { t } from "i18next";
import type { GetAnnouncementResponseDto } from "~/api";

type AnnouncementTileProps = {
  announcement: GetAnnouncementResponseDto;
  className?: string;
};

export default function AnnouncementTile({
  announcement,
  className,
}: AnnouncementTileProps) {
  const { keycloak } = useKeycloak();
  const navigate = useNavigate();

  const isBoard = isInGroupWithId(keycloak.tokenParsed, 1);

  return (
    <Tile className={cn("border border-gray-200 p-6", className)}>
      {/* Header: Title (Links) | Date & Edit (Rechts) */}
      <div className="flex w-full justify-between items-start mb-4 gap-4">
        <h3 className="font-bold text-lg leading-tight">{announcement.title}</h3>
        
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