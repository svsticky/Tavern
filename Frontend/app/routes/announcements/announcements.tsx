import { t } from "i18next";
import { PlusIcon } from "lucide-react";
import { useEffect, useState } from "react";
import { useNavigate } from "react-router";
import type { GetAnnouncementResponseDto } from "~/api";
import AnnouncementsList from "~/components/Announcement/AnnouncementsList";
import { NoContentTile } from "~/components/Tiles/NoContentTile";
import Button from "~/components/UI/Button";
import { PageHeader } from "~/components/UI/PageHeader/PageHeader";
import { useApp } from "~/context/AppContext";
import { useAuth } from "~/context/AuthContext";
import type { TokenParsed } from "~/types/TokenParsed";
import { isBoardOrCandidateBoard } from "~/util/group.util";
import {
  handleCreateAnnouncementClick,
  loadAnnouncements,
} from "./announcements.handlers";

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
  const authService = useAuth();
  const { boardGroupId, candidateBoardGroupId } = useApp();
  const [tokenParsed, setTokenParsed] = useState<TokenParsed | null>(null);

  const [loading, setLoading] = useState(true);

  const [isBoard, setIsBoard] = useState(false);

  const [announcements, setAnnouncements] = useState<
    GetAnnouncementResponseDto[]
  >([]);

  const navigate = useNavigate();

  useEffect(() => {
    const loadToken = async () => {
      const tokenParsed = await authService.getTokenParsed();
      setTokenParsed(tokenParsed);
      if (!tokenParsed) {
        console.error("User not authenticated");
        return;
      }
      setIsBoard(
        isBoardOrCandidateBoard(
          tokenParsed,
          boardGroupId,
          candidateBoardGroupId,
        ),
      );
    };
    loadToken();
  }, [authService, boardGroupId, candidateBoardGroupId]);

  useEffect(() => {
    if (tokenParsed) {
      loadAnnouncements({
        setLoading,
        setAnnouncements,
      });
    }
  }, [tokenParsed]);

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
      {loading ? (
        t("loading")
      ) : announcements.length === 0 ? (
        <NoContentTile text={t("no_announcements")} />
      ) : (
        <AnnouncementsList announcements={announcements} />
      )}
    </>
  );
}
