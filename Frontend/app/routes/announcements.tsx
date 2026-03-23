import { useKeycloak } from "@react-keycloak/web/lib/useKeycloak";
import { useEffect, useState } from "react";
import { getApiAnnouncements, type Announcement } from "~/api";
import AnnouncementsList from "~/components/AnnouncementsList";
import { NoContentTile } from "~/components/Tiles/NoContentTile";

export default function AnnouncementsPage() {
  const {keycloak, initialized} = useKeycloak();
  
  const [loading, setLoading] = useState(true);

  const [announcements, setAnnouncements] = useState<Announcement[]>([]);

  useEffect(() => {
    async function loadData() {
      if (!initialized || !keycloak.authenticated) return;

      try {
        setLoading(true);
        const announcementsResponse = await getApiAnnouncements();

        if (announcementsResponse.data) {
          setAnnouncements(announcementsResponse.data as Announcement[]);
        }
      } catch (error) {
        console.error("Error while loading data:", error);
      } finally {
        setLoading(false);
      }
    }

    loadData();
  }, [initialized, keycloak.authenticated]);

  return (
    <div className="flex flex-col gap-5">
      <p className="text-2xl font-bold">Announcements</p>
      {loading ? (
        'Loading...'
      ) : (
        announcements.length === 0 ? (
          <NoContentTile text="Er zijn momenteel geen aankondigingen." />
        ) : (
          <AnnouncementsList announcements={announcements} />
        ))}
    </div>
  );
}
