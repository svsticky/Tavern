import { t } from "i18next";
import toast from "react-hot-toast";
import {
  type Activity,
  type ActivityResponseDto,
  type EnrollmentBalance,
  getActivities,
  getPaymentsExport,
  getPaymentsOverpaid,
  getPaymentsUnpaid,
  type Member,
  postPaymentsActivity,
} from "~/api";
import { appendErrorMessage } from "~/util/error.util";
import { loadCommitteeYear } from "~/util/loaderAuth.util";

/**
 * Arguments for the internal setUnpaidPaymentState utility.
 * @typedef {Object} SetUnpaidStateArgs
 * @property {EnrollmentBalance[]} balances - The unpaid enrollment balances to process.
 * @property {(value: EnrollmentBalance[] | null) => void} setUnpaidBalances - Setter for the unsettled balances.
 * @property {(value: number) => void} setTotalUnpaid - Setter for the total amount still owed.
 * @property {(value: number) => void} setOpenPayments - Setter for the number of open payments.
 * @property {(value: Activity[] | null) => void} setUnpaidActivities - Setter for the activities with unpaid enrollments.
 * @property {(value: MemberWithOverduePayment[] | null) => void} setMembersWithOverduePayment - Setter for the unpaid enrollments grouped by member.
 */
type SetUnpaidStateArgs = {
  balances: EnrollmentBalance[];
  setUnpaidBalances: (value: EnrollmentBalance[] | null) => void;
  setTotalUnpaid: (value: number) => void;
  setOpenPayments: (value: number) => void;
  setUnpaidActivities: (value: Activity[] | null) => void;
  setMembersWithOverduePayment: (
    value: MemberWithOverduePayment[] | null,
  ) => void;
};

/**
 * A member together with their unpaid enrollments.
 * @typedef {Object} MemberWithOverduePayment
 * @property {Member} member - The member with a debt.
 * @property {EnrollmentBalance[]} enrollments - The member's unpaid enrollments.
 */
export type MemberWithOverduePayment = {
  member: Member;
  enrollments: EnrollmentBalance[];
};

/**
 * The unpaid balances, categorized for the UI.
 * @typedef {Object} UnpaidPaymentSummary
 * @property {EnrollmentBalance[]} unpaidBalances - The balances that aren't settled.
 * @property {number} totalUnpaid - The total amount that is still owed.
 * @property {number} openPayments - The number of open payments.
 * @property {Activity[]} unpaidActivities - The activities that have unpaid enrollments.
 * @property {MemberWithOverduePayment[]} membersWithOverduePayment - The unpaid enrollments grouped by member.
 */
export type UnpaidPaymentSummary = {
  unpaidBalances: EnrollmentBalance[];
  totalUnpaid: number;
  openPayments: number;
  unpaidActivities: Activity[];
  membersWithOverduePayment: MemberWithOverduePayment[];
};

/**
 * Processes raw enrollment balances and categorizes them for the UI.
 * It calculates total debt, identifies unique activities with debts, and groups debts by member.
 *
 * @param {EnrollmentBalance[]} balances - The unpaid enrollment balances.
 * @returns {UnpaidPaymentSummary} The categorized balances.
 */
export const summarizeUnpaidBalances = (
  balances: EnrollmentBalance[],
): UnpaidPaymentSummary => {
  const unpaidActivities = balances.reduce(
    (activities: Activity[], payment) => {
      if (
        payment.enrollment.activity &&
        !activities.some((a) => a.id === payment.enrollment.activity?.id)
      ) {
        activities.push(payment.enrollment.activity);
      }
      return activities;
    },
    [],
  );

  const membersMap: Record<string, MemberWithOverduePayment> = {};
  balances.forEach((payment) => {
    const member = payment.enrollment.member;
    if (member?.id) {
      if (!membersMap[member.id]) {
        membersMap[member.id] = { member, enrollments: [] };
      }
      membersMap[member.id].enrollments.push(payment);
    }
  });

  return {
    unpaidBalances: balances.filter((b) => b.balance !== 0),
    totalUnpaid: balances.reduce((sum, payment) => sum + payment.balance, 0),
    openPayments: balances.length,
    unpaidActivities,
    membersWithOverduePayment: Object.values(membersMap),
  };
};

/**
 * Processes raw enrollment balances (see `summarizeUnpaidBalances`) and puts the
 * result in the page's state.
 *
 * @param {SetUnpaidStateArgs} args - The balances and the state setters to update.
 */
export const setUnpaidPaymentState = ({
  balances,
  setUnpaidBalances,
  setTotalUnpaid,
  setOpenPayments,
  setUnpaidActivities,
  setMembersWithOverduePayment,
}: SetUnpaidStateArgs) => {
  const summary = summarizeUnpaidBalances(balances);
  setUnpaidBalances(summary.unpaidBalances);
  setTotalUnpaid(summary.totalUnpaid);
  setOpenPayments(summary.openPayments);
  setUnpaidActivities(summary.unpaidActivities);
  setMembersWithOverduePayment(summary.membersWithOverduePayment);
};

/**
 * Arguments for the refreshUnpaidPayments handler.
 * @typedef {Object} RefreshUnpaidArgs
 * @property {(value: EnrollmentBalance[] | null) => void} setUnpaidBalances - Setter for the unsettled balances.
 * @property {(value: number) => void} setTotalUnpaid - Setter for the total amount still owed.
 * @property {(value: number) => void} setOpenPayments - Setter for the number of open payments.
 * @property {(value: Activity[] | null) => void} setUnpaidActivities - Setter for the activities with unpaid enrollments.
 * @property {(value: MemberWithOverduePayment[] | null) => void} setMembersWithOverduePayment - Setter for the unpaid enrollments grouped by member.
 */
type RefreshUnpaidArgs = {
  setUnpaidBalances: (value: EnrollmentBalance[] | null) => void;
  setTotalUnpaid: (value: number) => void;
  setOpenPayments: (value: number) => void;
  setUnpaidActivities: (value: Activity[] | null) => void;
  setMembersWithOverduePayment: (
    value: MemberWithOverduePayment[] | null,
  ) => void;
};

/**
 * Fetches the latest unpaid balance data from the API and refreshes the state.
 *
 * @async
 * @param {RefreshUnpaidArgs} args - State setters to update after fetching.
 */
export const refreshUnpaidPayments = async ({
  setUnpaidBalances,
  setTotalUnpaid,
  setOpenPayments,
  setUnpaidActivities,
  setMembersWithOverduePayment,
}: RefreshUnpaidArgs) => {
  const unpaidBalances = await getPaymentsUnpaid({
    query: {
      allUsers: true,
    },
  });

  if (unpaidBalances.data) {
    setUnpaidPaymentState({
      balances: unpaidBalances.data,
      setUnpaidBalances,
      setTotalUnpaid,
      setOpenPayments,
      setUnpaidActivities,
      setMembersWithOverduePayment,
    });
  }
};

/**
 * Generates a WhatsApp message and opens a chat window to remind a member of their debts.
 *
 * Logic:
 * - Filters for activities past their payment deadline.
 * - Scales the "severity" of the language based on how many days overdue the oldest debt is.
 * - Uses the member's `preferredLanguage` (NL/EN) for the message body.
 * - Formats phone numbers to the international Dutch format (31).
 *
 * @param {Object} args
 * @param {Member} args.member - The member to message.
 * @param {EnrollmentBalance[]} args.enrollments - The member's specific outstanding enrollments.
 */
export const handleWhatsAppClick = ({
  member,
  enrollments,
}: {
  member: Member;
  enrollments: EnrollmentBalance[];
}) => {
  const unpaidEnrollments = enrollments.filter(
    (e) =>
      e.balance > 0 &&
      e.enrollment.activity &&
      new Date(e.enrollment.activity.paymentDeadline) < new Date(),
  );

  const totalAmount = unpaidEnrollments
    .reduce((sum, e) => sum + e.balance, 0)
    .toFixed(2);
  const activityList = unpaidEnrollments
    .map((e) => `- ${e.enrollment.activity?.name} - €${e.balance.toFixed(2)}`)
    .join("\n");

  const oldestDeadline = new Date(
    Math.min(
      ...unpaidEnrollments.map((e) =>
        new Date(e.enrollment.activity!.paymentDeadline).getTime(),
      ),
    ),
  );
  const daysOverdue = Math.floor(
    (Date.now() - oldestDeadline.getTime()) / (1000 * 60 * 60 * 24),
  );

  let deadlineTextNL = "";
  let deadlineTextEN = "";

  if (daysOverdue > 14) {
    deadlineTextNL = "vandaag";
    deadlineTextEN = "today";
  } else if (daysOverdue > 7) {
    deadlineTextNL = "binnen 7 dagen";
    deadlineTextEN = "within 7 days";
  } else {
    deadlineTextNL = "binnen 14 dagen";
    deadlineTextEN = "within 14 days";
  }

  const isNL = member.preferredLanguage === "NL";

  const message = isNL
    ? `Hey ${member.firstName}! De penningmeester van Sticky hier :)\n\nDe volgende activiteiten staan open:\n${activityList}\n\nDit geeft een totaal van €${totalAmount}\n\nZou je dit bedrag ${deadlineTextNL} via het betalingsportaal op Koala willen betalen voor je wordt geschorst? Deze kun je vinden via: https://koala.svsticky.nl`
    : `Hi ${member.firstName}! This is the treasurer of Sticky :)\n\nThe following activities are still unpaid:\n${activityList}\n\nThis totals €${totalAmount}\n\nWould you mind paying this ${deadlineTextEN} via the payment portal on Koala before you get suspended? You can find it here: https://koala.svsticky.nl`;

  const cleanPhone = member.phoneNumber.replace(/\D/g, "").replace(/^0/, "31");
  window.open(
    `https://wa.me/${cleanPhone}?text=${encodeURIComponent(message)}`,
    "_blank",
  );
};

/**
 * Arguments for the handleMarkAsPaid handler.
 * @typedef {Object} MarkPaidArgs
 * @property {Member} member - The member whose enrollments are marked as paid.
 * @property {EnrollmentBalance[]} enrollments - The enrollments to mark as paid.
 * @property {(loading: boolean) => void} setLoading - Setter for the loading state.
 * @property {() => Promise<void>} refreshUnpaid - Reloads the unpaid balances afterwards.
 */
type MarkPaidArgs = {
  member: Member;
  enrollments: EnrollmentBalance[];
  setLoading: (loading: boolean) => void;
  refreshUnpaid: () => Promise<void>;
};

/**
 * Manually marks a set of enrollments as paid in the system (e.g., if the user paid in cash).
 *
 * @async
 * @param {MarkPaidArgs} args - Context and refresh logic.
 */
export const handleMarkAsPaid = ({
  member,
  enrollments,
  setLoading,
  refreshUnpaid,
}: MarkPaidArgs) => {
  const process = async () => {
    try {
      setLoading(true);
      const response = await postPaymentsActivity({
        body: {
          memberId: member.id,
          activityIds: enrollments.map((e) => e.enrollment.activityId),
          manuallyMarkedAsPaid: true,
        },
      });

      if (response.error) {
        throw response.error ?? new Error("Failed to mark as paid");
      }

      await refreshUnpaid();
    } catch (error) {
      console.error("Error while marking as paid:", error);
      throw error;
    } finally {
      setLoading(false);
    }
  };

  toast.promise(process(), {
    loading: t("marking_as_paid"),
    success: t("marked_as_paid"),
    error: (error) => appendErrorMessage(t("mark_as_paid_failed"), error),
  });
};

/**
 * Arguments for the handleOverpaidProcessed handler.
 * @typedef {Object} OverpaidProcessedArgs
 * @property {EnrollmentBalance} balance - The overpaid balance that has been processed.
 * @property {(value: EnrollmentBalance[] | null) => void} setOverpaidBalances - Setter for the overpaid balances.
 */
type OverpaidProcessedArgs = {
  balance: EnrollmentBalance;
  setOverpaidBalances: (value: EnrollmentBalance[] | null) => void;
};

/**
 * Marks an overpaid enrollment as processed (e.g. after refunding the member) and reloads the overpaid balances.
 *
 * @param {OverpaidProcessedArgs} args - The overpaid balance and the state setter to refresh.
 */
export const handleOverpaidProcessed = ({
  balance,
  setOverpaidBalances,
}: OverpaidProcessedArgs) => {
  const process = async () => {
    const response = await postPaymentsActivity({
      body: {
        memberId: balance.enrollment.memberId,
        activityIds: [balance.enrollment.activityId],
        manuallyMarkedAsPaid: true,
      },
    });

    if (response.error) {
      throw response.error ?? new Error("Failed to mark as processed");
    }

    const overpaidBalances = await getPaymentsOverpaid();
    if (overpaidBalances.data) {
      setOverpaidBalances(overpaidBalances.data.filter((b) => b.balance !== 0));
    }
  };

  toast.promise(process(), {
    loading: t("marking_as_processed"),
    success: t("marked_as_processed"),
    error: (error) => appendErrorMessage(t("mark_as_processed_failed"), error),
  });
};

/**
 * Generates a CSV export of payments within a specific date range and triggers a browser download.
 *
 * @async
 * @param {string} exportStartDate - Start of the range (YYYY-MM-DD).
 * @param {string} exportEndDate - End of the range (YYYY-MM-DD).
 * @param {Function} setExporting - State setter for loading indicators.
 */
export const handlePaymentsExport = (
  exportStartDate: string,
  exportEndDate: string,
  setExporting: (exporting: boolean) => void,
) => {
  const exportAction = async () => {
    try {
      setExporting(true);
      const response = await getPaymentsExport({
        query: {
          startDate: exportStartDate,
          endDate: exportEndDate,
        },
        responseType: "blob",
      });

      if (response.error || !response.data) {
        throw response.error ?? new Error("Failed to export payments");
      }

      const blob = new Blob([response.data as any], { type: "text/csv" });
      const url = window.URL.createObjectURL(blob);

      const link = document.createElement("a");
      link.href = url;
      link.setAttribute(
        "download",
        `payments_${exportStartDate}_to_${exportEndDate}.csv`,
      );
      document.body.appendChild(link);
      link.click();

      link.parentNode?.removeChild(link);
      window.URL.revokeObjectURL(url);
    } catch (error) {
      console.error("Error while exporting payments:", error);
      throw error;
    } finally {
      setExporting(false);
    }
  };

  toast.promise(exportAction(), {
    loading: t("exporting"),
    success: t("export_success"),
    error: (error) => appendErrorMessage(t("export_failed"), error),
  });
};

/**
 * The data the finances page's `clientLoader` provides.
 * Besides the fields below, it holds the `UnpaidPaymentSummary` fields.
 * @typedef {Object} FinancesLoaderData
 * @property {EnrollmentBalance[]} overpaidBalances - The balances where a member paid too much.
 * @property {number} committeeYear - The current committee year, the default year for the expired activities.
 * @property {ActivityResponseDto[]} expiredActivities - The expired activities of `committeeYear`.
 */
export type FinancesLoaderData = UnpaidPaymentSummary & {
  overpaidBalances: EnrollmentBalance[];
  committeeYear: number;
  expiredActivities: ActivityResponseDto[];
};

/**
 * Loads everything the finances page needs: the unpaid debts, the overpaid
 * credits and the expired activities of the current committee year.
 *
 * @returns {Promise<FinancesLoaderData>} The loader data for the finances page.
 * @throws {Error} When any of the requests fail, which shows the route's error page.
 */
export const loadFinancesLoaderData = async (): Promise<FinancesLoaderData> => {
  const committeeYear = await loadCommitteeYear();

  const [unpaidBalancesResponse, overpaidBalancesResponse, expiredActivities] =
    await Promise.all([
      getPaymentsUnpaid({ query: { allUsers: true } }),
      getPaymentsOverpaid(),
      fetchExpiredActivities(committeeYear),
    ]);

  if (unpaidBalancesResponse.error || !unpaidBalancesResponse.data)
    throw (
      unpaidBalancesResponse.error ??
      new Error("Failed to load unpaid balances")
    );
  if (overpaidBalancesResponse.error || !overpaidBalancesResponse.data)
    throw (
      overpaidBalancesResponse.error ??
      new Error("Failed to load overpaid balances")
    );

  return {
    ...summarizeUnpaidBalances(unpaidBalancesResponse.data),
    overpaidBalances: overpaidBalancesResponse.data.filter(
      (b) => b.balance !== 0,
    ),
    committeeYear,
    expiredActivities,
  };
};

/**
 * Fetches past, closed-for-payment activities for a single association year.
 *
 * @param {number} year - The association year to fetch the activities of.
 * @returns {Promise<ActivityResponseDto[]>} The expired activities.
 * @throws {Error} When the activities fail to load.
 */
const fetchExpiredActivities = async (
  year: number,
): Promise<ActivityResponseDto[]> => {
  const response = await getActivities({
    query: {
      IncludePast: true,
      IncludeFuture: false,
      OpenForPayment: false,
      OnlyWithPaidEnrollments: true,
      Year: year,
      Page: 1,
      PageSize: 50,
    },
  });

  if (response.error || !response.data)
    throw response.error ?? new Error("Failed to load expired activities");

  return response.data;
};

/**
 * Arguments for the loadExpiredActivities handler.
 * @typedef {Object} LoadExpiredActivitiesArgs
 * @property {number} year - The association year to load the expired activities of.
 * @property {(loading: boolean) => void} setLoadingExpiredActivities - Setter for the expired activities' loading state.
 * @property {(value: ActivityResponseDto[] | null) => void} setExpiredActivities - Setter for the expired activities.
 */
type LoadExpiredActivitiesArgs = {
  year: number;
  setLoadingExpiredActivities: (loading: boolean) => void;
  setExpiredActivities: (value: ActivityResponseDto[] | null) => void;
};

/**
 * Fetches past, closed-for-payment activities for a single association year, for the
 * finance dashboard's "expired activities" review queue.
 *
 * Scoped to one year at a time (rather than every closed activity ever) so that
 * activities from long-settled years don't pile up in the queue indefinitely.
 *
 * @async
 * @param {LoadExpiredActivitiesArgs} args - The target year and state setters to update.
 */
export const loadExpiredActivities = async ({
  year,
  setLoadingExpiredActivities,
  setExpiredActivities,
}: LoadExpiredActivitiesArgs) => {
  try {
    setLoadingExpiredActivities(true);
    setExpiredActivities(await fetchExpiredActivities(year));
  } catch (error) {
    console.error("Error while fetching expired activities:", error);
    toast.error(appendErrorMessage(t("loading_failed"), error));
  } finally {
    setLoadingExpiredActivities(false);
  }
};
