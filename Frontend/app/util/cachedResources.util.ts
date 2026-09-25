import {
  type ActivityResponseDto,
  type GetAnnouncementResponseDto,
  type GroupMembershipResponseDto,
  getActivities,
  getAnnouncements,
  getGroupmemberships,
} from "~/api";
import {
  ACTIVITIES_CACHE_KEY,
  ANNOUNCEMENTS_CACHE_KEY,
  cachedResource,
  ENROLLED_ACTIVITIES_CACHE_KEY,
  GROUP_MEMBERSHIPS_CACHE_KEY,
} from "./resourceCache.util";

/** Served from the resource cache when fresh; throws on failure so loaders reach the error boundary. */

export const loadUpcomingActivities = () =>
  cachedResource(ACTIVITIES_CACHE_KEY, async () => {
    const response = await getActivities({
      query: { IncludePast: false, IncludeFuture: true },
    });
    if (response.error || !response.data)
      throw new Error("Failed to load activities");
    return response.data as ActivityResponseDto[];
  });

export const loadEnrolledActivities = (userId: string) =>
  cachedResource(`${ENROLLED_ACTIVITIES_CACHE_KEY}:${userId}`, async () => {
    const response = await getActivities({
      query: { UserId: userId, IncludePast: false, IncludeFuture: true },
    });
    if (response.error || !response.data)
      throw new Error("Failed to load enrolled activities");
    return response.data as ActivityResponseDto[];
  });

export const loadAnnouncements = () =>
  cachedResource(ANNOUNCEMENTS_CACHE_KEY, async () => {
    const response = await getAnnouncements();
    if (response.error || !response.data)
      throw new Error("Failed to load announcements");
    return response.data as GetAnnouncementResponseDto[];
  });

export const loadGroupMemberships = (userId: string) =>
  cachedResource(`${GROUP_MEMBERSHIPS_CACHE_KEY}:${userId}`, async () => {
    const response = await getGroupmemberships({ query: { MemberId: userId } });
    if (response.error || !response.data)
      throw new Error("Failed to load group memberships");
    return response.data as GroupMembershipResponseDto[];
  });
