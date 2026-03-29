import type { Announcement } from "~/api";
import AnnouncementTile from "./Tiles/AnnouncementTile";
import { NoContentTile } from "./Tiles/NoContentTile";
import { t } from "i18next";

type AnnouncementsListProps = {
  announcements: Announcement[];
};

export default function AnnouncementsList({
  announcements,
}: AnnouncementsListProps) {

 if (announcements.length === 0) {
    return (
      <NoContentTile text={t("no_announcements")} />
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
