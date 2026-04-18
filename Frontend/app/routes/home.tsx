import { useKeycloak } from "@react-keycloak/web";
import { useEffect, useState } from "react";
import { toast } from "react-hot-toast";
import { useTranslation } from "react-i18next";
import { useNavigate } from "react-router";
import { getApiActivities, getApiAnnouncements, getApiGroupmemberships, getApiGroupmembershipsById, getApiPaymentsUnpaid, type Activity, type ActivityResponseDto, type Announcement, type GetAnnouncementResponseDto, type GroupMembership } from "~/api";
import ActivityEnrollmentOverview from "~/components/Activity/ActivityEnrollmentOverview";
import AnnouncementsList from "~/components/Announcement/AnnouncementsList";
import CommitteeEnrollmentOverview from "~/components/CommitteeEnrollmentOverview";
import DashboardHeader from "~/components/DashboardHeader";
import Button from "~/components/UI/Button";
import UpcomingActivities from "~/components/Activity/UpcomingActivities";
import type { CommitteeEnrollment } from "~/types/CommitteeEnrollment";

export default function DashboardPage() {
  const { t } = useTranslation();
  const { keycloak, initialized } = useKeycloak();
  const navigate = useNavigate();

  const [activities, setActivities] = useState<ActivityResponseDto[]>([]);
  const [announcements, setAnnouncements] = useState<GetAnnouncementResponseDto[]>([]);
  const [committees, setCommittees] = useState<CommitteeEnrollment[]>([]);

  const [loading, setLoading] = useState(true);
  useEffect(() => {
    async function loadData() {
      if (!initialized || !keycloak.authenticated) return;

      try {
        setLoading(true);
        const activitiesResponse = await getApiActivities({
          query: {
            IncludePast: false,
            IncludeFuture: true,
          }
        });

        const announcementsResponse = await getApiAnnouncements();

        if (announcementsResponse.data) {
          setAnnouncements(announcementsResponse.data as GetAnnouncementResponseDto[]);
        }

        if (activitiesResponse.data) {
          setActivities(activitiesResponse.data as ActivityResponseDto[]);
        }

        const committeesResponse = await getApiGroupmemberships({
          query: {
            onlyOwnMemberships: true
          }
        });

        if (committeesResponse.data) {
          setCommittees(committeesResponse.data.map(c => ({
            id: c.id,
            name: c.groupName,
            role: c.roleAliasName ?? "",
            icon: "https://www.svgrepo.com/show/509977/group.svg"
          })));
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
    <div className="flex flex-col items-center gap-5 max-w-8xl mx-auto w-full">
      {/* Dashboard Header */}
      <DashboardHeader 
        name={keycloak.tokenParsed?.given_name} 
        nextActivity={activities[0]} 
      />

      {loading ? (
        <div className="flex flex-col items-center justify-center min-h-[400px] w-full gap-4">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-(--board-primary-light)"></div>
          <p className="text-gray-500 animate-pulse">{t("loading_dashboard")}...</p>
        </div>
      ) : (
        <div className="grid grid-cols-4 w-full gap-5 animate-in fade-in duration-500">
          <div className="flex flex-col w-full gap-y-8 col-span-4 lg:col-span-3">
            
            {/* Announcements */}
            <div className="flex flex-col w-full gap-y-3">
              <div className="flex w-full justify-between items-center">
                <p className="font-semibold text-lg">
                  {t("latest_announcements")}
                </p>
                <Button
                  showArrow
                  className="bg-transparent p-0 hover:bg-transparent text-(--board-primary) hover:text-(--board-primary-light) shadow-none"
                  onClick={() => navigate("/announcements")}
                >
                  {t("show_all")}
                </Button>
              </div>
              <AnnouncementsList announcements={announcements} />
            </div>
            
            {/* Upcoming Activities */}
            <div className="flex flex-col w-full gap-y-3"> {/* Toegevoegd: flex flex-col en gap-y-3 */}
              <div className="flex w-full justify-between items-center">
                <p className="font-semibold text-lg">
                  {t("upcoming_activities")}
                </p>
                <Button
                  showArrow
                  className="bg-transparent p-0 hover:bg-transparent text-(--board-primary) hover:text-(--board-primary-light) shadow-none"
                  onClick={() => navigate("/activities")}
                >
                  {t("show_all")}
                </Button>
              </div>
              <UpcomingActivities activities={activities} />
            </div>
          </div>

          {/* Enrollments and Committees */}
          <div className="flex flex-col col-span-4 lg:col-span-1 gap-3">
            <p className="text-md">{t("my_enrollments")}</p>
            <ActivityEnrollmentOverview 
              enrolledActivities={activities.filter(a => 
                a.enrollments.some(e => e.member.id === keycloak.tokenParsed?.UserId)
              )} 
            />

            <p className="text-md">{t("my_committees")}</p>
            <CommitteeEnrollmentOverview committeeEnrollments={committees} />
          </div>
        </div>
      )}
    </div>
  );
}