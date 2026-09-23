import {
  type ActivityResponseDto,
  type GetAnnouncementResponseDto,
  type GroupMembershipResponseDto,
  getActivities,
  getAnnouncements,
  getEnrollments,
  getGroupmemberships,
  getPaymentsUnpaid,
} from "~/api";

export type HomeLoaderData = {
  activities: ActivityResponseDto[];
  enrolledActivities: ActivityResponseDto[];
  announcements: GetAnnouncementResponseDto[];
  groupMemberships: GroupMembershipResponseDto[];
  outstandingPayments: number;
  unpaidActivityIds: number[];
  pastEnrollmentAmount: number;
  comingEnrollmentAmount: number;
};

/**
 * Fetches the core data sets the home dashboard needs, in parallel: upcoming
 * activities, the user's own enrolled activities, announcements, the user's
 * group memberships, outstanding payments and enrollment counts (coming vs.
 * attended).
 *
 * Called from the route's `clientLoader` - throws on failure so React
 * Router's error boundary handles it, rather than each caller having to
 * check `.error` itself. This is the steady-state snapshot only:
 * `DashboardHeader` separately polls just the payments endpoint when
 * returning from a Mollie checkout, since the webhook confirming payment can
 * land up to ~30s after redirect - that can't block the whole page behind
 * this loader.
 */
export async function loadHomeLoaderData(
  userId: string,
): Promise<HomeLoaderData> {
  const [
    activitiesResponse,
    enrolledActivitiesResponse,
    announcementsResponse,
    groupMembershipsResponse,
    outstandingPaymentsResponse,
    enrollmentAmountResponse,
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

  if (outstandingPaymentsResponse.error || !outstandingPaymentsResponse.data)
    throw new Error("Failed to load outstanding payments");

  if (enrollmentAmountResponse.error || !enrollmentAmountResponse.data)
    throw new Error("Failed to load enrollments");

  const now = Date.now();
  let pastEnrollmentAmount = 0;
  let comingEnrollmentAmount = 0;
  for (const enrollment of enrollmentAmountResponse.data) {
    const activityDate = new Date(enrollment.activity.dateTimeEnd).getTime();
    if (activityDate < now) {
      pastEnrollmentAmount++;
    } else {
      comingEnrollmentAmount++;
    }
  }

  return {
    activities: activitiesResponse.data as ActivityResponseDto[],
    enrolledActivities:
      enrolledActivitiesResponse.data as ActivityResponseDto[],
    announcements: announcementsResponse.data as GetAnnouncementResponseDto[],
    groupMemberships: groupMembershipsResponse.data,
    outstandingPayments: outstandingPaymentsResponse.data.reduce(
      (total, payment) => total + (payment.balance || 0),
      0,
    ),
    unpaidActivityIds: outstandingPaymentsResponse.data.map(
      (payment) => payment.enrollment.activityId,
    ),
    pastEnrollmentAmount,
    comingEnrollmentAmount,
  };
}
