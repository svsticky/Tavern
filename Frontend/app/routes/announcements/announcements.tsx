import { useKeycloak } from "@react-keycloak/web/lib/useKeycloak";
import { t } from "i18next";
import { PlusIcon } from "lucide-react";
import { useEffect, useState } from "react";
import { useNavigate } from "react-router";
import { type GetAnnouncementResponseDto } from "~/api";
import AnnouncementsList from "~/components/Announcement/AnnouncementsList";
import { NoContentTile } from "~/components/Tiles/NoContentTile";
import Button from "~/components/UI/Button";
import { PageHeader } from "~/components/UI/PageHeader/PageHeader";
import { isBoardOrCandidateBoard } from "~/util/group.util";
import { handleCreateAnnouncementClick, loadAnnouncements } from "./announcements.handlers";

/**
 * The public-facing and administrative announcements page.
 * 
 * This component displays a list of association-wide announcements. It features:
 * - **Permission-based Actions**: Board and Candidate Board members see a 
 *   plus icon in the header to create new announcements.
 * - **Dynamic Data Loading**: Uses the `loadAnnouncements` handler to fetch data 
 *   once Keycloak is initialized and the user is authenticated.
 * - **State-driven Rendering**: Handles loading states, empty list scenarios 
 *   (via `NoContentTile`), and populated list views (via `AnnouncementsList`).
 * 
 * @page
 * @component
 */
export default function AnnouncementsPage() {
  const {keycloak, initialized} = useKeycloak();
  
  const [loading, setLoading] = useState(true);

  const [announcements, setAnnouncements] = useState<GetAnnouncementResponseDto[]>([]);

  const navigate = useNavigate();

  useEffect(() => {
    loadAnnouncements({
      initialized,
      authenticated: keycloak.authenticated,
      setLoading,
      setAnnouncements
    });
  }, [initialized, keycloak.authenticated]);

  return (
    <>
      <div className="flex justify-between items-center">
        <PageHeader title={t("announcements")}
          action={isBoardOrCandidateBoard(keycloak.tokenParsed) && (
          <Button 
            variant="secondary"
            onClick={() => handleCreateAnnouncementClick(navigate)}
            className="items-center px-3 py-1"
          >
            <PlusIcon className="w-5 h-5" />
          </Button>
        )} />
      </div>
      {loading ? (
        t("loading")
      ) : (
        announcements.length === 0 ? (
          <NoContentTile text={t("no_announcements")} />
        ) : (
          <AnnouncementsList announcements={announcements} />
        ))}
    </>
  );
}
