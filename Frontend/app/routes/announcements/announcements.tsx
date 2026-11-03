import { t } from "i18next";
import { PlusIcon } from "lucide-react";
import { useLoaderData, useNavigate } from "react-router";
import { getAnnouncements } from "~/api";
import AnnouncementsList from "~/components/Announcement/AnnouncementsList";
import StickyLoadingLogo from "~/components/StickyLoadingLogo";
import { NoContentTile } from "~/components/Tiles/NoContentTile";
import Button from "~/components/UI/Button";
import { PageHeader } from "~/components/UI/PageHeader";
import { isBoardOrCandidateBoard } from "~/util/group.util";
import { requireTokenParsed } from "~/util/loaderAuth.util";
import { handleCreateAnnouncementClick } from "./announcements.handlers";

/**
 * Loads the user's token and all announcements.
 * Redirects to the login page when the user isn't logged in.
 *
 * @throws {Error} When the announcements fail to load, which shows the route's error page.
 */
export async function clientLoader() {
  const tokenParsed = await requireTokenParsed();
  const announcements = await getAnnouncements();
  if (announcements.error || !announcements.data)
    throw new Error("Failed to load announcements");
  return { tokenParsed, announcements: announcements.data };
}

/**
 * Shown while the route's `clientLoader` runs on the first page load.
 *
 * @component
 */
export function HydrateFallback() {
  return <StickyLoadingLogo />;
}

/**
 * The public-facing and administrative announcements page.
 *
 * This component displays a list of association-wide announcements. It features:
 * - **Permission-based Actions**: Board and Candidate Board members see a
 *   plus icon in the header to create new announcements.
 * - **Dynamic Data Loading**: Uses the `loadAnnouncements` handler to fetch data
 * - **State-driven Rendering**: Handles loading states, empty list scenarios
 *   (via `NoContentTile`), and populated list views (via `AnnouncementsList`).
 *
 * @page
 * @component
 */
export default function AnnouncementsPage() {
  const { tokenParsed, announcements } = useLoaderData<typeof clientLoader>();

  const navigate = useNavigate();

  const isBoard = isBoardOrCandidateBoard(tokenParsed);

  return (
    <>
      <div className="flex justify-between items-center">
        <PageHeader
          title={t("announcements")}
          action={
            isBoard && (
              <Button
                variant="secondary"
                onClick={() => handleCreateAnnouncementClick(navigate)}
                className="items-center px-3 py-1"
              >
                <PlusIcon className="w-5 h-5" />
              </Button>
            )
          }
        />
      </div>
      {announcements.length === 0 ? (
        <NoContentTile text={t("no_announcements")} />
      ) : (
        <AnnouncementsList announcements={announcements} />
      )}
    </>
  );
}
