import type { Announcement } from "~/api";
import AnnouncementTile from "./Tiles/AnnouncementTile";
import { NoContentTile } from "./Tiles/NoContentTile";

type AnnouncementsListProps = {
  announcements: Announcement[];
};

export default function AnnouncementsList({
  announcements,
}: AnnouncementsListProps) {

 if (announcements.length === 0) {
    return (
      <NoContentTile text="Er zijn momenteel geen aankondigingen." />
    );
  }

  return (
    <div className="flex flex-col gap-5">
      {announcements.map((announcement) => (
        <AnnouncementTile key={announcement.id} announcement={announcement} />
      ))}
    </div>
  );
}
