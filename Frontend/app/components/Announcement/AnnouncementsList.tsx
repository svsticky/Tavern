import { t } from "i18next";
import type { GetAnnouncementResponseDto } from "~/api";
import { NoContentTile } from "../Tiles/NoContentTile";
import AnnouncementTile from "./AnnouncementTile";

/**
 * A vertical list component that renders a collection of association announcements.
 *
 * Features:
 * - **Empty State Handling**: Automatically detects if the announcement array is empty
 *   and renders a `NoContentTile` with a localized message.
 * - **Iterative Rendering**: Maps through the `announcements` array to generate
 *   individual `AnnouncementTile` components, providing a unique key for each.
 * - **Responsive Layout**: Uses a flex-column layout with standard gap spacing to
 *   ensure a consistent vertical flow.
 *
 * @component
 * @param {Object} props - The component props.
 * @param {GetAnnouncementResponseDto[]} props.announcements - An array of announcement
 * objects fetched from the API to be displayed to the user.
 *
 * @example
 * ```tsx
 * <AnnouncementsList
 *   announcements={latestAnnouncements}
 * />
 * ```
 */
export default function AnnouncementsList({
  announcements,
}: {
  announcements: GetAnnouncementResponseDto[];
}) {
  if (announcements.length === 0) {
    return <NoContentTile text={t("no_announcements")} />;
  }

  return (
    <div className="flex flex-col gap-5">
      {announcements.map((announcement) => (
        <AnnouncementTile key={announcement.id} announcement={announcement} />
      ))}
    </div>
  );
}
