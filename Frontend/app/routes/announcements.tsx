import { useKeycloak } from "@react-keycloak/web/lib/useKeycloak";
import { t } from "i18next";
import { PlusIcon } from "lucide-react";
import { useEffect, useState } from "react";
import { toast } from "react-hot-toast";
import { useNavigate } from "react-router";
import { getApiAnnouncements, type GetAnnouncementResponseDto } from "~/api";
import AnnouncementsList from "~/components/AnnouncementsList";
import { NoContentTile } from "~/components/Tiles/NoContentTile";
import Button from "~/components/UI/Button";
import { PageHeader } from "~/components/UI/PageHeader";
import { isInGroupWithId } from "~/util/group.util";

export default function AnnouncementsPage() {
  const {keycloak, initialized} = useKeycloak();
  
  const [loading, setLoading] = useState(true);

  const [announcements, setAnnouncements] = useState<GetAnnouncementResponseDto[]>([]);

  const navigate = useNavigate();

  useEffect(() => {
    async function loadData() {
      if (!initialized || !keycloak.authenticated) return;

      try {
        setLoading(true);
        const announcementsResponse = await getApiAnnouncements();

        if (announcementsResponse.data) {
          setAnnouncements(announcementsResponse.data as GetAnnouncementResponseDto[]);
        }
      } catch (error) {
        console.error("Error while loading data:", error);
        toast.error(t("loading_failed"));
      } finally {
        setLoading(false);
      }
    }

    loadData();
  }, [initialized, keycloak.authenticated]);

  return (
    <>
      <div className="flex justify-between items-center">
        <PageHeader title={t("announcements")}
          action={isInGroupWithId(keycloak.tokenParsed, import.meta.env.BOARD_GROUP_ID) && (
          <Button 
            onClick={() => (navigate("/announcements/create"))}
            className="flex items-center gap-2 px-3 py-1 rounded-lg transition-colors font-medium shadow-sm"
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
