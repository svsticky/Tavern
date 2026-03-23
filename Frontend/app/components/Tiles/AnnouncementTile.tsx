import { Calendar, Megaphone } from "lucide-react";
import { formatDate } from "~/util/date.util";
import { cn } from "~/util/tailwind.util";
import Tile from "./Tile";
import type { Announcement } from "~/api";

type AnnouncementTileProps = {
  announcement: Announcement;
  className?: string;
};

export default function AnnouncementTile({
  announcement,
  className,
}: AnnouncementTileProps) {
  return (
    <Tile className={cn("rounded-2xl border border-gray-200", className)}>
      {/* Title and date */}
      <div className="flex w-full justify-between">
        <p className="mb-2">{announcement.title}</p>
        <p className="flex gap-1 text-sm text-nowrap text-gray-600">
          <Calendar className="h-5" />
          {formatDate(new Date(announcement.createdAt ?? new Date()), "defaultDate")}
        </p>
      </div>

      {/* Announcement content */}
      <p className="text-gray-600">{announcement.content}</p>

      {/* Divider */}
      <div className="my-2 h-[0.5px] w-full bg-gray-200" />

      {/* Announcer */}
      <p className="flex items-center gap-2 text-gray-600">
        <Megaphone className="h-5" />
        {`${announcement.createdBy?.firstName} ${announcement.createdBy?.lastName}`}
      </p>
    </Tile>
  );
}
