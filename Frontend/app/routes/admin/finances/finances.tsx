import { t } from "i18next";
import { Euro, MessageCircle } from "lucide-react";
import { useEffect, useState } from "react";
import { useLoaderData, useSearchParams } from "react-router";
import type { ActivityResponseDto } from "~/api";
import StickyLoadingLogo from "~/components/StickyLoadingLogo";
import BorderedTile from "~/components/Tiles/BorderedTile";
import Tile from "~/components/Tiles/Tile";
import Button from "~/components/UI/Button";
import Input from "~/components/UI/Input";
import { PageHeader } from "~/components/UI/PageHeader";
import Select from "~/components/UI/Select";
import { formatDate, getCommitteeYear } from "~/util/date.util";
import { requireTokenParsed } from "~/util/loaderAuth.util";
import {
  type FinancesData,
  fetchExpiredActivities,
  fetchFinancesData,
  handleMarkAsPaid,
  handlePaymentsExport,
  handleWhatsAppClick,
  refreshUnpaidPayments,
} from "./finances.handlers";

type LoaderData = FinancesData & {
  year: number;
  expiredActivities: ActivityResponseDto[];
};

/**
 * Fetches the finance dashboard's unpaid/overpaid data and the "expired
 * activities" queue for the year in the URL (so a selected year is
 * shareable and restored for free on back-navigation).
 */
export async function clientLoader({
  request,
}: {
  request: Request;
}): Promise<LoaderData> {
  await requireTokenParsed();

  const url = new URL(request.url);
  const year = Number(url.searchParams.get("year")) || getCommitteeYear();

  const [financesData, expiredActivities] = await Promise.all([
    fetchFinancesData(),
    fetchExpiredActivities(year),
  ]);

  return { ...financesData, expiredActivities, year };
}

export function HydrateFallback() {
  return <StickyLoadingLogo />;
}

/**
 * The administrative Finances dashboard for the association.
 *
 * This page provides a centralized overview of the association's treasury tasks:
 * - **Financial KPIs**: Displays total outstanding debt and the number of open payments.
 * - **Debt Recovery**: Identifies members with overdue payments and provides one-click WhatsApp
 *   messaging with pre-filled localized reminders.
 * - **Activity Reconciliation**: Groups unpaid balances by activity, allowing the treasurer
 *   to see which events have the most outstanding revenue.
 * - **Credit Management**: Lists members who have overpaid, facilitating refunds or processed adjustments.
 * - **Data Export**: Provides a date-range selector to export payment records to CSV for accounting.
 * - **Disciplinary Tracking**: Automatically highlights members who have been in debt for more than
 *   21 days (standard association suspension threshold) in red.
 *
 * @page
 * @component
 */
export default function Finances() {
  const loaderData = useLoaderData<typeof clientLoader>();
  const [searchParams, setSearchParams] = useSearchParams();

  const [loading, setLoading] = useState(false);
  const [exporting, setExporting] = useState(false);
  const [totalUnpaid, setTotalUnpaid] = useState(loaderData.totalUnpaid);
  const [openPayments, setOpenPayments] = useState(loaderData.openPayments);
  const [unpaidBalances, setUnpaidBalances] = useState(
    loaderData.unpaidBalances,
  );
  const [unpaidActivities, setUnpaidActivities] = useState(
    loaderData.unpaidActivities,
  );
  const [membersWithOverduePayment, setMembersWithOverduePayment] = useState(
    loaderData.membersWithOverduePayment,
  );
  const [exportStartDate, setExportStartDate] = useState<string>("");
  const [exportEndDate, setExportEndDate] = useState<string>("");

  // The loader reruns (and hands back new unpaid/overpaid data) on every
  // navigation, including a year change - resync the locally-patched state
  // (see refreshUnpaidPayments below) to that fresh data.
  useEffect(() => {
    setTotalUnpaid(loaderData.totalUnpaid);
    setOpenPayments(loaderData.openPayments);
    setUnpaidBalances(loaderData.unpaidBalances);
    setUnpaidActivities(loaderData.unpaidActivities);
    setMembersWithOverduePayment(loaderData.membersWithOverduePayment);
  }, [loaderData]);

  const currentYear = getCommitteeYear();
  const expiredActivitiesYears = Array.from(
    { length: 10 },
    (_, i) => currentYear - i,
  );

  const changeExpiredActivitiesYear = (year: number) => {
    const next = new URLSearchParams(searchParams);
    next.set("year", String(year));
    setSearchParams(next);
  };

  const deviceLocale =
    typeof navigator !== "undefined" ? navigator.language : undefined;

  return (
    <>
      <div className="flex flex-col lg:flex-row lg:items-center lg:items-start justify-between gap-3">
        <PageHeader title={t("finances")} backTo="/" />
        <Input
          type="date"
          label={t("start_date")}
          name="start_date"
          onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
            setExportStartDate(e.target.value)
          }
        />
        <Input
          type="date"
          label={t("end_date")}
          name="end_date"
          onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
            setExportEndDate(e.target.value)
          }
        />
        <div className="w-full sm:w-auto">
          <Button
            variant="secondary"
            className="w-full mb-4"
            disabled={
              exportStartDate === "" || exportEndDate === "" || exporting
            }
            onClick={() =>
              handlePaymentsExport(exportStartDate, exportEndDate, setExporting)
            }
          >
            {t("export")}
            {exporting && "..."}
          </Button>
        </div>
      </div>
      <div className="flex flex-col gap-4 w-full">
        <div className="flex flex-col sm:flex-row gap-4 w-full">
          <BorderedTile
            title={t("total_unpaid")}
            icon={Euro}
            className="flex-1"
          >
            <p className="text-2xl font-bold text-slate-800">
              {totalUnpaid.toLocaleString(deviceLocale, {
                style: "currency",
                currency: "EUR",
              })}
            </p>
            <p className="text-(--board-primary) text-sm font-semibold mt-1">
              {openPayments} {t("open_payments")}
            </p>
          </BorderedTile>

          <BorderedTile
            title={t("overpaid")}
            subtitle={
              loaderData.overpaidBalances.length === 0
                ? t("no_overpaid_balances")
                : ""
            }
            className="flex-1"
          >
            <div className="flex flex-col gap-2">
              {loaderData.overpaidBalances.map((balance, index) => (
                <>
                  <div
                    key={index}
                    className="p-3 bg-green-100 rounded-lg flex items-center justify-between"
                  >
                    <span className="text-sm text-slate-700">
                      {balance.enrollment.member?.firstName}{" "}
                      {balance.enrollment.member?.lastName}
                    </span>
                    <span className="font-bold text-green-600">{`€${Math.abs(balance.balance).toFixed(2)}`}</span>
                    <span className="text-xs text-green-500">
                      {balance.enrollment.activity?.name}
                    </span>
                  </div>
                  <Button variant="primary" className="self-end">
                    {t("processed")}
                  </Button>
                </>
              ))}
            </div>
          </BorderedTile>
        </div>

        <BorderedTile
          title={t("expired_activities")}
          subtitle={t("expired_activities_subtitle")}
        >
          <div className="w-full sm:w-auto self-end">
            <Select
              options={expiredActivitiesYears.map((y) => ({
                label: `${y - 1}/${y}`,
                value: y,
              }))}
              label={t("year")}
              style={{ minWidth: "150px" }}
              value={loaderData.year}
              onChange={(e) =>
                changeExpiredActivitiesYear(Number(e.target.value))
              }
            />
          </div>

          {loaderData.expiredActivities.length === 0 && (
            <span className="text-sm text-slate-400">{t("no_data")}</span>
          )}

          {loaderData.expiredActivities.map((activity) => (
            <Tile
              className="bg-gray-100 flex flex-col md:flex-row w-full justify-between items-start md:items-center p-4 rounded-lg gap-4"
              key={activity.id}
            >
              <div className="flex flex-col gap-1">
                <span className="text-slate-700 font-medium">
                  {activity.name}
                </span>
                <div className="flex flex-col md:flex-row md:items-center gap-1 md:gap-2 text-sm text-slate-500">
                  <span>
                    {formatDate(new Date(activity.dateTimeEnd), "fullDateTime")}
                  </span>
                  <span className="hidden md:inline">•</span>
                  <span>
                    {activity.enrollments.length} {t("participants")}
                  </span>
                  <span className="hidden md:inline">•</span>
                  <span>{`€${activity.price?.toFixed(2) || t("free")}`}</span>
                </div>
              </div>

              <Button
                variant="primary"
                className="w-full md:w-auto"
                href={`/activities/${activity.id}`}
              >
                {t("go_to_activity")}
              </Button>
            </Tile>
          ))}
        </BorderedTile>

        <BorderedTile
          title={t("finances_activity_overview")}
          subtitle={t("overdue_payment_subtitle")}
          className="flex flex-col gap-3"
        >
          {unpaidActivities.map((activity) => (
            <BorderedTile
              key={activity.id}
              title={activity.name}
              className="bg-gray-100"
              collapsibleContent={
                <div className="flex flex-col gap-3">
                  <span className="text-xs font-bold uppercase tracking-wider text-slate-400 mb-1">
                    {t("unpaid_members")} ({membersWithOverduePayment.length})
                  </span>
                  {membersWithOverduePayment.map((memberWithOverduePayment) => {
                    const member = memberWithOverduePayment.member;

                    return (
                      <div
                        key={member.id}
                        className="flex flex-col sm:flex-row sm:items-center justify-between p-3 border border-slate-100 rounded-xl gap-3"
                      >
                        <div className="flex items-center justify-between sm:justify-start sm:gap-6 flex-1">
                          <span className="font-semibold text-slate-700 text-sm">
                            {member.firstName} {member.lastName}
                          </span>

                          <span className="font-bold text-slate-600 sm:ml-auto sm:mr-4">
                            {`€${memberWithOverduePayment.enrollments.reduce((sum, enrollment) => sum + enrollment.balance, 0).toFixed(2)}`}
                          </span>
                        </div>

                        <div className="w-full sm:w-auto">
                          <Button
                            variant="primary"
                            className="w-full sm:w-auto"
                            onClick={() =>
                              handleMarkAsPaid({
                                member,
                                enrollments:
                                  memberWithOverduePayment.enrollments,
                                setLoading,
                                refreshUnpaid: () =>
                                  refreshUnpaidPayments({
                                    setUnpaidBalances,
                                    setTotalUnpaid,
                                    setOpenPayments,
                                    setUnpaidActivities,
                                    setMembersWithOverduePayment,
                                  }),
                              })
                            }
                            disabled={loading}
                          >
                            {t("mark_as_paid")}
                          </Button>
                        </div>
                      </div>
                    );
                  })}
                </div>
              }
            >
              <div className="text-xs flex gap-2 items-center">
                <span className="ml-4 text-(--board-primary) font-bold">
                  {t("outstanding")}: €
                  {unpaidBalances
                    .filter((b) => b.enrollment?.activityId === activity.id)
                    .reduce((sum, balance) => sum + balance.balance, 0)
                    .toFixed(2)}
                </span>
              </div>
            </BorderedTile>
          ))}
        </BorderedTile>

        <BorderedTile
          title={t("overdue_payment")}
          subtitle={t("overdue_payment_subtitle")}
          className=""
        >
          <div className="flex flex-col">
            {membersWithOverduePayment.map((memberWithOverduePayment) => {
              if (
                !memberWithOverduePayment.enrollments.some(
                  (enrollment) =>
                    enrollment.balance > 0 &&
                    enrollment.enrollment.activity &&
                    new Date(enrollment.enrollment.activity?.paymentDeadline) <
                      new Date(Date.now()),
                )
              )
                return null;
              const member = memberWithOverduePayment.member;
              const oldestDeadline = new Date(
                Math.min(
                  ...memberWithOverduePayment.enrollments.map((e) =>
                    new Date(
                      e.enrollment.activity?.paymentDeadline || "",
                    ).getTime(),
                  ),
                ),
              );
              const isOlderThan21Days =
                oldestDeadline <
                new Date(Date.now() - 21 * 24 * 60 * 60 * 1000);

              return (
                <div
                  key={member.id}
                  className={`p-3 first:rounded-t last:rounded-b border-b border-slate-100 last:border-0 ${isOlderThan21Days ? "bg-red-100" : ""}`}
                >
                  <div className="grid grid-cols-1 sm:grid-cols-[1fr_auto] gap-x-4">
                    <div className="font-semibold text-slate-700">
                      {member.firstName} {member.lastName}
                    </div>

                    <div className="mt-3 sm:mt-0 row-start-3 sm:row-start-1 sm:col-start-2">
                      <Button
                        variant="secondary"
                        className="flex flex-row items-center justify-center gap-2 w-full sm:w-auto"
                        onClick={() =>
                          handleWhatsAppClick(memberWithOverduePayment)
                        }
                      >
                        <MessageCircle className="w-4 h-4" />
                        WhatsApp
                      </Button>
                    </div>

                    <div className="flex flex-col gap-2 mt-2 sm:col-start-1">
                      {memberWithOverduePayment.enrollments.map(
                        (enrollment, index) => {
                          if (
                            enrollment.balance <= 0 ||
                            !enrollment.enrollment.activity ||
                            new Date(
                              enrollment.enrollment.activity?.paymentDeadline,
                            ) >= new Date(Date.now())
                          )
                            return null;
                          return (
                            <div key={index} className="text-sm text-slate-600">
                              • {enrollment.enrollment.activity.name} - €
                              {enrollment.balance.toFixed(2)}
                              <span className="text-xs text-slate-400 ml-1">
                                ({t("due_date")}:{" "}
                                {formatDate(
                                  new Date(
                                    enrollment.enrollment.activity
                                      ?.paymentDeadline || "",
                                  ),
                                  "shortDate",
                                )}
                                )
                              </span>
                            </div>
                          );
                        },
                      )}
                    </div>
                  </div>
                </div>
              );
            })}
          </div>
        </BorderedTile>
      </div>
    </>
  );
}
