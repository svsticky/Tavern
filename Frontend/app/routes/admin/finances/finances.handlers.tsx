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

/**
 * Arguments for the internal setUnpaidPaymentState utility.
 */
type SetUnpaidStateArgs = {
  balances: EnrollmentBalance[];
  setUnpaidBalances: (value: EnrollmentBalance[]) => void;
  setTotalUnpaid: (value: number) => void;
  setOpenPayments: (value: number) => void;
  setUnpaidActivities: (value: Activity[]) => void;
  setMembersWithOverduePayment: (
    value: { member: Member; enrollments: EnrollmentBalance[] }[],
  ) => void;
};

/** Shared by the setter-based refresh and the loader. */
export const deriveUnpaidPaymentState = (balances: EnrollmentBalance[]) => {
  const unpaidBalances = balances.filter((b) => b.balance !== 0);
  const totalUnpaid = balances.reduce(
    (sum, payment) => sum + payment.balance,
    0,
  );
  const openPayments = balances.length;

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

  const membersMap: Record<
    string,
    { member: Member; enrollments: EnrollmentBalance[] }
  > = {};
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
    unpaidBalances,
    totalUnpaid,
    openPayments,
    unpaidActivities,
    membersWithOverduePayment: Object.values(membersMap),
  };
};

/**
 * Internal utility to process raw enrollment balances and categorize them for the UI.
 * It calculates total debt, identifies unique activities with debts, and groups debts by member.
 *
 * @param {SetUnpaidStateArgs} args - Configuration and state setters.
 */
export const setUnpaidPaymentState = ({
  balances,
  setUnpaidBalances,
  setTotalUnpaid,
  setOpenPayments,
  setUnpaidActivities,
  setMembersWithOverduePayment,
}: SetUnpaidStateArgs) => {
  const derived = deriveUnpaidPaymentState(balances);
  setUnpaidBalances(derived.unpaidBalances);
  setTotalUnpaid(derived.totalUnpaid);
  setOpenPayments(derived.openPayments);
  setUnpaidActivities(derived.unpaidActivities);
  setMembersWithOverduePayment(derived.membersWithOverduePayment);
};

/**
 * Arguments for the refreshUnpaidPayments handler.
 */
type RefreshUnpaidArgs = {
  setUnpaidBalances: (value: EnrollmentBalance[]) => void;
  setTotalUnpaid: (value: number) => void;
  setOpenPayments: (value: number) => void;
  setUnpaidActivities: (value: Activity[]) => void;
  setMembersWithOverduePayment: (
    value: { member: Member; enrollments: EnrollmentBalance[] }[],
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

export type FinancesData = ReturnType<typeof deriveUnpaidPaymentState> & {
  overpaidBalances: EnrollmentBalance[];
};

/** Expired activities are fetched separately (see `fetchExpiredActivities`) because they're filtered by year. */
export const fetchFinancesData = async (): Promise<FinancesData> => {
  const [unpaidBalancesResponse, overpaidBalancesResponse] = await Promise.all([
    getPaymentsUnpaid({
      query: {
        allUsers: true,
      },
    }),
    getPaymentsOverpaid(),
  ]);

  if (unpaidBalancesResponse.error || !unpaidBalancesResponse.data) {
    throw (
      unpaidBalancesResponse.error ??
      new Error("Failed to load unpaid payments")
    );
  }
  if (overpaidBalancesResponse.error || !overpaidBalancesResponse.data) {
    throw (
      overpaidBalancesResponse.error ??
      new Error("Failed to load overpaid payments")
    );
  }

  return {
    ...deriveUnpaidPaymentState(unpaidBalancesResponse.data),
    overpaidBalances: overpaidBalancesResponse.data.filter(
      (b) => b.balance !== 0,
    ),
  };
};

/**
 * Fetches past, closed-for-payment activities for a single association year, for the
 * finance dashboard's "expired activities" review queue.
 *
 * Scoped to one year at a time (rather than every closed activity ever) so that
 * activities from long-settled years don't pile up in the queue indefinitely.
 *
 * @async
 * @param {number} year - The association year to fetch expired activities for.
 */
export const fetchExpiredActivities = async (
  year: number,
): Promise<ActivityResponseDto[]> => {
  const response = await getActivities({
    query: {
      IncludePast: true,
      IncludeFuture: false,
      OpenForPayment: false,
      Year: year,
      Page: 1,
      PageSize: 50,
    },
  });

  if (response.error || !response.data) {
    throw response.error ?? new Error("Failed to load expired activities");
  }

  return response.data;
};
