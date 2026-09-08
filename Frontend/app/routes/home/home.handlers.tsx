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
import { appendErrorMessage } from "~/util/error.util";

/**
 * Arguments for the loadHomePageData handler.
 */
type loadHomePageArgs = {
  authenticated: boolean | undefined;
  userId: string | undefined;
  setLoading: (loading: boolean) => void;
  setActivities: (activities: ActivityResponseDto[]) => void;
  setAnnouncements: (announcements: GetAnnouncementResponseDto[]) => void;
  setGroupMemberships: (memberships: GroupMembershipResponseDto[]) => void;
  setEnrolledActivities: (activities: ActivityResponseDto[]) => void;
};

/**
 * Orchestrates the data hydration for the main user home page.
 *
 * Fetches three core data sets in sequence:
 * 1. **Upcoming Activities**: Future events available for viewing or enrollment.
 * 2. **Announcements**: Recent association-wide notifications.
 * 3. **Personal Memberships**: Groups and committees the specific user belongs to.
 *
 * @async
 * @param {loadHomePageArgs} args - Configuration object containing:
 * @param {boolean} args.initialized - Guard to ensure auth services are ready.
 * @param {boolean | undefined} args.authenticated - Guard to ensure the user is logged in.
 * @param {string | undefined} args.userId - The ID used to filter personal group memberships.
 * @param {Function} args.setLoading - Function to toggle the loading overlay.
 * @param {Function} args.setActivities - Function to update the activities state.
 * @param {Function} args.setAnnouncements - Function to update the announcements state.
 * @param {Function} args.setGroupMemberships - Function to update the user's committees state.
 * @param {Function} args.setEnrolledActivities - Function to update the user's enrolled activities state.
 * @throws {Error} Throws an error if any of the API requests fail.
 * @returns {Promise<void>} Resolves when all data has been fetched and state updated, or rejects with an error.
 */
export const loadHomePageData = async ({
  authenticated,
  userId,
  setLoading,
  setActivities,
  setAnnouncements,
  setGroupMemberships,
  setEnrolledActivities,
}: loadHomePageArgs) => {
  if (!authenticated) return;

  try {
    setLoading(true);
    const [
      activitiesResponse,
      enrolledActivitiesResponse,
      announcementsResponse,
      committeesResponse,
    ] = await Promise.all([
      getActivities({
        query: {
          IncludePast: false,
          IncludeFuture: true,
        },
      }),
      getActivities({
        query: {
          UserId: userId,
          IncludePast: true,
          IncludeFuture: true,
        },
      }),
      getAnnouncements(),
      getGroupmemberships({
        query: {
          MemberId: userId,
        },
      }),
    ]);
    if (activitiesResponse.error || !activitiesResponse.data)
      throw new Error("Failed to load activities");
    setActivities(activitiesResponse.data as ActivityResponseDto[]);

    if (enrolledActivitiesResponse.error || !enrolledActivitiesResponse.data)
      throw new Error("Failed to load enrolled activities");
    setEnrolledActivities(
      enrolledActivitiesResponse.data as ActivityResponseDto[],
    );

    if (announcementsResponse.error || !announcementsResponse.data)
      throw new Error("Failed to load announcements");
    setAnnouncements(
      announcementsResponse.data as GetAnnouncementResponseDto[],
    );

    if (committeesResponse.error || !committeesResponse.data)
      throw new Error("Failed to load group memberships");
    setGroupMemberships(committeesResponse.data);
  } catch (error) {
    console.error("Error while loading data:", error);
    toast.error(appendErrorMessage(t("loading_failed"), error));
  } finally {
    setLoading(false);
  }
};
