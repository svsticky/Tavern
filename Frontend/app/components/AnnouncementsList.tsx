import type { GetAnnouncementDto } from "~/api";
import AnnouncementTile from "./Tiles/AnnouncementTile";
import { t } from "i18next";
import { NoContentTile } from "./Tiles/NoContentTile";

type AnnouncementsListProps = {
  announcements: GetAnnouncementDto[];
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
