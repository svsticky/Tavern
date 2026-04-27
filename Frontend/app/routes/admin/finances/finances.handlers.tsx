import { t } from "i18next";
import toast from "react-hot-toast";
import {
  getApiActivities,
  getApiPaymentsExport,
  getApiPaymentsOverpaid,
  getApiPaymentsUnpaid,
  postApiPaymentsActivity,
  type Activity,
  type ActivityResponseDto,
  type EnrollmentBalance,
  type Member
} from "~/api";

type SetUnpaidStateArgs = {
  balances: EnrollmentBalance[];
  setUnpaidBalances: (value: EnrollmentBalance[] | null) => void;
  setTotalUnpaid: (value: number) => void;
  setOpenPayments: (value: number) => void;
  setUnpaidActivities: (value: Activity[] | null) => void;
  setMembersWithOverduePayment: (value: { member: Member; enrollments: EnrollmentBalance[] }[] | null) => void;
};

export const setUnpaidPaymentState = ({
  balances,
  setUnpaidBalances,
  setTotalUnpaid,
  setOpenPayments,
  setUnpaidActivities,
  setMembersWithOverduePayment
}: SetUnpaidStateArgs) => {
  setUnpaidBalances(balances.filter((b) => b.balance !== 0));
  const totalUnpaidAmount = balances.reduce((sum, payment) => sum + payment.balance, 0);
  setTotalUnpaid(totalUnpaidAmount);
  setOpenPayments(balances.length);

  const activitiesWithUnpaid = balances.reduce((activities: Activity[], payment) => {
    if (payment.enrollment.activity && !activities.some((a) => a.id === payment.enrollment.activity?.id)) {
      payment.enrollment.activity && activities.push(payment.enrollment.activity);
    }
    return activities;
  }, []);
  setUnpaidActivities(activitiesWithUnpaid);

  const membersMap: Record<string, { member: Member; enrollments: EnrollmentBalance[] }> = {};
  balances.forEach((payment) => {
    const member = payment.enrollment.member;
    if (member && member.id) {
      if (!membersMap[member.id]) {
        membersMap[member.id] = { member, enrollments: [] };
      }
      membersMap[member.id].enrollments.push(payment);
    }
  });
  setMembersWithOverduePayment(Object.values(membersMap));
};

type RefreshUnpaidArgs = {
  setUnpaidBalances: (value: EnrollmentBalance[] | null) => void;
  setTotalUnpaid: (value: number) => void;
  setOpenPayments: (value: number) => void;
  setUnpaidActivities: (value: Activity[] | null) => void;
  setMembersWithOverduePayment: (value: { member: Member; enrollments: EnrollmentBalance[] }[] | null) => void;
};

export const refreshUnpaidPayments = async ({
  setUnpaidBalances,
  setTotalUnpaid,
  setOpenPayments,
  setUnpaidActivities,
  setMembersWithOverduePayment
}: RefreshUnpaidArgs) => {
  const unpaidBalances = await getApiPaymentsUnpaid({
    query: {
      allUsers: true,
    }
  });

  if (unpaidBalances.data) {
    setUnpaidPaymentState({
      balances: unpaidBalances.data,
      setUnpaidBalances,
      setTotalUnpaid,
      setOpenPayments,
      setUnpaidActivities,
      setMembersWithOverduePayment
    });
  }
};

export const handleWhatsAppClick = ({ member, enrollments }: { member: Member; enrollments: EnrollmentBalance[] }) => {
  const unpaidEnrollments = enrollments.filter(
    (e) => e.balance > 0 && e.enrollment.activity && new Date(e.enrollment.activity.paymentDeadline) < new Date()
  );

  const totalAmount = unpaidEnrollments.reduce((sum, e) => sum + e.balance, 0).toFixed(2);
  const activityList = unpaidEnrollments
    .map((e) => `- ${e.enrollment.activity?.name} - €${e.balance.toFixed(2)}`)
    .join("\n");

  const oldestDeadline = new Date(Math.min(...unpaidEnrollments.map((e) => new Date(e.enrollment.activity!.paymentDeadline).getTime())));
  const daysOverdue = Math.floor((Date.now() - oldestDeadline.getTime()) / (1000 * 60 * 60 * 24));

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
  window.open(`https://wa.me/${cleanPhone}?text=${encodeURIComponent(message)}`, "_blank");
};

type MarkPaidArgs = {
  member: Member;
  enrollments: EnrollmentBalance[];
  setLoading: (loading: boolean) => void;
  refreshUnpaid: () => Promise<void>;
};

export const handleMarkAsPaid = ({ member, enrollments, setLoading, refreshUnpaid }: MarkPaidArgs) => {
  const process = async () => {
    try {
      setLoading(true);
      const response = await postApiPaymentsActivity({
        body: {
          memberId: member.id,
          activityIds: enrollments.map((e) => e.enrollment.activityId),
          manuallyMarkedAsPaid: true,
        }
      });

      if (response.error) throw new Error("Failed to mark as paid");

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
    error: t("mark_as_paid_failed"),
  });
};

export const handlePaymentsExport = (
  exportStartDate: string,
  exportEndDate: string,
  setExporting: (exporting: boolean) => void
) => {
  const exportAction = async () => {
    try {
      setExporting(true);
      const response = await getApiPaymentsExport({
        query: {
          startDate: exportStartDate,
          endDate: exportEndDate,
        },
        responseType: "blob",
      });

      if(response.error || !response.data) {
        throw new Error("Failed to export payments");
      }

      const blob = new Blob([response.data as any], { type: "text/csv" });
      const url = window.URL.createObjectURL(blob);

      const link = document.createElement("a");
      link.href = url;
      link.setAttribute("download", `payments_${exportStartDate}_to_${exportEndDate}.csv`);
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
    error: t("export_failed"),
  });
};

type LoadFinancesArgs = {
  setLoading: (loading: boolean) => void;
  setExpiredActivities: (value: ActivityResponseDto[] | null) => void;
  setUnpaidBalances: (value: EnrollmentBalance[] | null) => void;
  setTotalUnpaid: (value: number) => void;
  setOpenPayments: (value: number) => void;
  setUnpaidActivities: (value: Activity[] | null) => void;
  setMembersWithOverduePayment: (value: { member: Member; enrollments: EnrollmentBalance[] }[] | null) => void;
  setOverpaidBalances: (value: EnrollmentBalance[] | null) => void;
};

export const loadFinancesData = async ({
  setLoading,
  setExpiredActivities,
  setUnpaidBalances,
  setTotalUnpaid,
  setOpenPayments,
  setUnpaidActivities,
  setMembersWithOverduePayment,
  setOverpaidBalances
}: LoadFinancesArgs) => {
  try {
    setLoading(true);

    const expiredActivitiesResponse = await getApiActivities({
      query: {
        IncludePast: true,
        IncludeFuture: false,
        OpenForPayment: false,
      }
    });

    if(expiredActivitiesResponse.error || !expiredActivitiesResponse.data) throw new Error("Failed to load expired activities");

    setExpiredActivities(expiredActivitiesResponse.data || []);

    await refreshUnpaidPayments({
      setUnpaidBalances,
      setTotalUnpaid,
      setOpenPayments,
      setUnpaidActivities,
      setMembersWithOverduePayment
    });

    const overpaidBalances = await getApiPaymentsOverpaid();
    if (overpaidBalances.data) {
      setOverpaidBalances(overpaidBalances.data.filter((b) => b.balance !== 0));
    }
  } catch (error) {
    console.error("Error while fetching data:", error);
    toast.error(t("loading_failed"));
  } finally {
    setLoading(false);
  }
};
