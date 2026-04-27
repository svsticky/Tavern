import { toast } from "react-hot-toast";
import { t } from "i18next";
import { getApiActivities, getApiAnnouncements, getApiGroupmemberships, type ActivityResponseDto, type GetAnnouncementResponseDto, type GroupMembershipResponseDto } from "~/api";

type LoadDashboardArgs = {
  initialized: boolean;
  authenticated: boolean | undefined;
  userId: string | undefined;
  setLoading: (loading: boolean) => void;
  setActivities: (activities: ActivityResponseDto[]) => void;
  setAnnouncements: (announcements: GetAnnouncementResponseDto[]) => void;
  setGroupMemberships: (memberships: GroupMembershipResponseDto[]) => void;
};

export const loadDashboardData = async ({
  initialized,
  authenticated,
  userId,
  setLoading,
  setActivities,
  setAnnouncements,
  setGroupMemberships
}: LoadDashboardArgs) => {
  if (!initialized || !authenticated) return;

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
        MemberId: userId
      }
    });

    if (committeesResponse.data) {
      setGroupMemberships(committeesResponse.data);
    }
  } catch (error) {
    console.error("Error while loading data:", error);
    toast.error(t("loading_failed"));
  } finally {
    setLoading(false);
  }
};
