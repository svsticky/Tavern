import {
  type ActivityResponseDto,
  type EnrollmentBalance,
  type EnrollmentResponseDto,
  type GetAnnouncementResponseDto,
  type GroupMembershipResponseDto,
  getActivities,
  getAnnouncements,
  getEnrollments,
  getGroupmemberships,
  getPaymentsUnpaid,
} from "~/api";

/**
 * The data the home page's `clientLoader` provides.
 * @typedef {Object} HomeLoaderData
 * @property {ActivityResponseDto[]} activities - The upcoming activities.
 * @property {ActivityResponseDto[]} futureEnrollments - The upcoming activities the user is enrolled in.
 * @property {GetAnnouncementResponseDto[]} announcements - All announcements.
 * @property {GroupMembershipResponseDto[]} groupMemberships - The groups and committees the user belongs to.
 * @property {EnrollmentBalance[]} unpaidBalances - The user's unpaid activity enrollments.
 * @property {EnrollmentResponseDto[]} enrollments - The user's enrollments.
 */
export type HomeLoaderData = {
  activities: ActivityResponseDto[];
  futureEnrollments: ActivityResponseDto[];
  announcements: GetAnnouncementResponseDto[];
  groupMemberships: GroupMembershipResponseDto[];
  unpaidBalances: EnrollmentBalance[];
  enrollments: EnrollmentResponseDto[];
};

/**
 * Loads all data for the home page in parallel: upcoming activities, the user's
 * future enrolled activities, announcements, group memberships, unpaid balances
 * and enrollments.
 *
 * @param {string} userId - The ID of the logged-in user.
 * @returns {Promise<HomeLoaderData>} The loader data for the home page.
 * @throws {Error} When any of the requests fail, which shows the route's error page.
 */
export async function loadHomeLoaderData(
  userId: string,
): Promise<HomeLoaderData> {
  const [
    activitiesResponse,
    enrolledActivitiesResponse,
    announcementsResponse,
    groupMembershipsResponse,
    unpaidBalancesResponse,
    enrollmentsResponse,
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
    getPaymentsUnpaid(),
    getEnrollments({
      query: {
        FromMemberId: userId,
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

  if (unpaidBalancesResponse.error || !unpaidBalancesResponse.data)
    throw new Error("Failed to load unpaid balances");

  if (enrollmentsResponse.error || !enrollmentsResponse.data)
    throw new Error("Failed to load enrollments");

  return {
    activities: activitiesResponse.data,
    futureEnrollments: enrolledActivitiesResponse.data.filter(
      (a: ActivityResponseDto) =>
        new Date(a.dateTimeEnd) >= new Date(Date.now()),
    ),
    announcements: announcementsResponse.data.slice(0, 2),
    groupMemberships: groupMembershipsResponse.data,
    unpaidBalances: unpaidBalancesResponse.data,
    enrollments: enrollmentsResponse.data,
  };
}
