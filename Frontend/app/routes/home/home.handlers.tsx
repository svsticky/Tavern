import {
  type ActivityResponseDto,
  type GetAnnouncementResponseDto,
  type GroupMembershipResponseDto,
  getActivities,
  getAnnouncements,
  getGroupmemberships,
} from "~/api";

export type HomeLoaderData = {
  activities: ActivityResponseDto[];
  enrolledActivities: ActivityResponseDto[];
  announcements: GetAnnouncementResponseDto[];
  groupMemberships: GroupMembershipResponseDto[];
};

/**
 * Fetches the three core data sets the home dashboard needs, in parallel:
 * upcoming activities, the user's own enrolled activities, announcements,
 * and the user's group memberships.
 *
 * Called from the route's `clientLoader` - throws on failure so React
 * Router's error boundary handles it, rather than each caller having to
 * check `.error` itself.
 */
export async function loadHomeLoaderData(
  userId: string,
): Promise<HomeLoaderData> {
  const [
    activitiesResponse,
    enrolledActivitiesResponse,
    announcementsResponse,
    groupMembershipsResponse,
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
        IncludePast: false,
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

  if (enrolledActivitiesResponse.error || !enrolledActivitiesResponse.data)
    throw new Error("Failed to load enrolled activities");

  if (announcementsResponse.error || !announcementsResponse.data)
    throw new Error("Failed to load announcements");

  if (groupMembershipsResponse.error || !groupMembershipsResponse.data)
    throw new Error("Failed to load group memberships");

  return {
    activities: activitiesResponse.data as ActivityResponseDto[],
    enrolledActivities:
      enrolledActivitiesResponse.data as ActivityResponseDto[],
    announcements: announcementsResponse.data as GetAnnouncementResponseDto[],
    groupMemberships: groupMembershipsResponse.data,
  };
}
