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
import {
  ACTIVITIES_CACHE_KEY,
  ANNOUNCEMENTS_CACHE_KEY,
  HOME_ENROLLED_ACTIVITIES_CACHE_KEY,
  HOME_GROUP_MEMBERSHIPS_CACHE_KEY,
  setCachedResource,
} from "~/util/resourceCache.util";

/**
 * Arguments for the loadHomePageData handler.
 */
type loadHomePageArgs = {
  authenticated: boolean | undefined;
  userId: string | undefined;
  cachedActivities?: ActivityResponseDto[];
  cachedAnnouncements?: GetAnnouncementResponseDto[];
  cachedEnrolledActivities?: ActivityResponseDto[];
  cachedGroupMemberships?: GroupMembershipResponseDto[];
  setLoading: (loading: boolean) => void;
  setActivities: (activities: ActivityResponseDto[]) => void;
  setAnnouncements: (announcements: GetAnnouncementResponseDto[]) => void;
  setGroupMemberships: (memberships: GroupMembershipResponseDto[]) => void;
  setEnrolledActivities: (activities: ActivityResponseDto[]) => void;
};

/** Reuses `cached` if given, otherwise fetches and caches it under `cacheKey`. */
function fetchOrCached<T>(
  cached: T | undefined,
  cacheKey: string,
  fetcher: () => Promise<T>,
): Promise<T> {
  if (cached) return Promise.resolve(cached);
  return fetcher().then((data) => {
    setCachedResource(cacheKey, data);
    return data;
  });
}

/**
 * Orchestrates the data hydration for the main user home page.
 *
 * Each of the four data sets is cached - a `cached*` value passed in is
 * reused as-is instead of being re-fetched.
 *
 * @async
 * @param {loadHomePageArgs} args - Configuration object containing:
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
  cachedActivities,
  cachedAnnouncements,
  cachedEnrolledActivities,
  cachedGroupMemberships,
  setLoading,
  setActivities,
  setAnnouncements,
  setGroupMemberships,
  setEnrolledActivities,
}: loadHomePageArgs) => {
  if (!authenticated) return;

  const activitiesPromise = fetchOrCached(
    cachedActivities,
    ACTIVITIES_CACHE_KEY,
    () =>
      getActivities({
        query: { IncludePast: false, IncludeFuture: true },
      }).then((res) => {
        if (res.error || !res.data)
          throw new Error("Failed to load activities");
        return res.data as ActivityResponseDto[];
      }),
  );

  const announcementsPromise = fetchOrCached(
    cachedAnnouncements,
    ANNOUNCEMENTS_CACHE_KEY,
    () =>
      getAnnouncements().then((res) => {
        if (res.error || !res.data)
          throw new Error("Failed to load announcements");
        return res.data as GetAnnouncementResponseDto[];
      }),
  );

  const enrolledActivitiesPromise = fetchOrCached(
    cachedEnrolledActivities,
    HOME_ENROLLED_ACTIVITIES_CACHE_KEY,
    () =>
      getActivities({
        query: { UserId: userId, IncludePast: false, IncludeFuture: true },
      }).then((res) => {
        if (res.error || !res.data)
          throw new Error("Failed to load enrolled activities");
        return res.data as ActivityResponseDto[];
      }),
  );

  const groupMembershipsPromise = fetchOrCached(
    cachedGroupMemberships,
    HOME_GROUP_MEMBERSHIPS_CACHE_KEY,
    () =>
      getGroupmemberships({ query: { MemberId: userId } }).then((res) => {
        if (res.error || !res.data)
          throw new Error("Failed to load group memberships");
        return res.data;
      }),
  );

  try {
    setLoading(true);
    const [activities, enrolledActivities, announcements, groupMemberships] =
      await Promise.all([
        activitiesPromise,
        enrolledActivitiesPromise,
        announcementsPromise,
        groupMembershipsPromise,
      ]);

    setActivities(activities);
    setEnrolledActivities(enrolledActivities);
    setAnnouncements(announcements);
    setGroupMemberships(groupMemberships);
  } catch (error) {
    console.error("Error while loading data:", error);
    toast.error(appendErrorMessage(t("loading_failed"), error));
  } finally {
    setLoading(false);
  }
};
