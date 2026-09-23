import { t } from "i18next";
import { PlusIcon } from "lucide-react";
import { useLoaderData, useNavigate } from "react-router";
import AnnouncementsList from "~/components/Announcement/AnnouncementsList";
import StickyLoadingLogo from "~/components/StickyLoadingLogo";
import { NoContentTile } from "~/components/Tiles/NoContentTile";
import Button from "~/components/UI/Button";
import { PageHeader } from "~/components/UI/PageHeader";
import { isBoardOrCandidateBoard } from "~/util/group.util";
import { requireTokenParsed } from "~/util/loaderAuth.util";
import type { Route } from "./+types/announcements";
import {
  handleCreateAnnouncementClick,
  loadAnnouncements,
} from "./announcements.handlers";

export async function clientLoader() {
  const tokenParsed = await requireTokenParsed();
  const announcements = await loadAnnouncements();
  return { tokenParsed, announcements };
}

export function HydrateFallback() {
  return <StickyLoadingLogo />;
}

/**
 * The public-facing and administrative announcements page.
 *
 * This component displays a list of association-wide announcements. It features:
 * - **Permission-based Actions**: Board and Candidate Board members see a
 *   plus icon in the header to create new announcements.
 * - **State-driven Rendering**: Handles empty list scenarios (via
 *   `NoContentTile`) and populated list views (via `AnnouncementsList`).
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
