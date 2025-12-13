import type { Announcement } from "~/types/Announcement";
import AnnouncementTile from "./Tiles/AnnouncementTile";

type AnnouncementsListProps = {
  announcements: Announcement[];
};

export default function AnnouncementsList({
  announcements,
}: AnnouncementsListProps) {
  return (
    <div className="flex flex-col gap-5">
      {announcements.map((announcement) => (
        <AnnouncementTile
          key={announcement.id}
          announcement={announcement}
        />
      ))}
    </div>
  );
}
