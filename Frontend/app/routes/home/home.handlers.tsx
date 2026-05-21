import { t } from "i18next";
import { toast } from "react-hot-toast";
import {
  type ActivityResponseDto,
  type GetAnnouncementResponseDto,
  type GroupMembershipResponseDto,
  getActivities,
  getAnnouncements,
  getGroupmemberships,
} from "~/api";

/**
 * Arguments for the loadDashboardData handler.
 */
type LoadDashboardArgs = {
  initialized: boolean;
  authenticated: boolean | undefined;
  userId: string | undefined;
  setLoading: (loading: boolean) => void;
  setActivities: (activities: ActivityResponseDto[]) => void;
  setAnnouncements: (announcements: GetAnnouncementResponseDto[]) => void;
  setGroupMemberships: (memberships: GroupMembershipResponseDto[]) => void;
};

/**
 * Orchestrates the data hydration for the main user dashboard.
 *
 * Fetches three core data sets in sequence:
 * 1. **Upcoming Activities**: Future events available for viewing or enrollment.
 * 2. **Announcements**: Recent association-wide notifications.
 * 3. **Personal Memberships**: Groups and committees the specific user belongs to.
 *
 * @async
 * @param {LoadDashboardArgs} args - Configuration object containing:
 * @param {boolean} args.initialized - Guard to ensure auth services are ready.
 * @param {boolean | undefined} args.authenticated - Guard to ensure the user is logged in.
 * @param {string | undefined} args.userId - The ID used to filter personal group memberships.
 * @param {Function} args.setLoading - Function to toggle the loading overlay.
 * @param {Function} args.setActivities - Function to update the activities state.
 * @param {Function} args.setAnnouncements - Function to update the announcements state.
 * @param {Function} args.setGroupMemberships - Function to update the user's committees state.
 */
export const loadDashboardData = async ({
  initialized,
  authenticated,
  userId,
  setLoading,
  setActivities,
  setAnnouncements,
  setGroupMemberships,
}: LoadDashboardArgs) => {
  if (!initialized || !authenticated) return;

  try {
    setLoading(true);
    const activitiesResponse = await getActivities({
      query: {
        IncludePast: false,
        IncludeFuture: true,
      },
    });
    if (activitiesResponse.error || !activitiesResponse.data)
      throw new Error("Failed to load activities");
    setActivities(activitiesResponse.data as ActivityResponseDto[]);

    const announcementsResponse = await getAnnouncements();
    if (announcementsResponse.error || !announcementsResponse.data)
      throw new Error("Failed to load announcements");
    setAnnouncements(
      announcementsResponse.data as GetAnnouncementResponseDto[],
    );

    const committeesResponse = await getGroupmemberships({
      query: {
        MemberId: userId,
      },
    });
    if (committeesResponse.error || !committeesResponse.data)
      throw new Error("Failed to load group memberships");
    setGroupMemberships(committeesResponse.data);
  } catch (error) {
    console.error("Error while loading data:", error);
    toast.error(t("loading_failed"));
  } finally {
    setLoading(false);
  }
};
