import {
  Calendar,
  CircleCheckBig,
  Clock,
  TrendingUp,
  UsersRound,
} from "lucide-react";
import { useTranslation } from "react-i18next";
import { formatDate } from "~/util/date.util";
import Tile from "./Tiles/Tile";
import Button from "./UI/Button";
import { getApiEnrollments, getApiGroupmemberships, getApiPaymentsUnpaid, postApiPaymentsActivity, type Activity, type GroupMembership } from "~/api";
import { useEffect, useState } from "react";
import { useKeycloak } from "@react-keycloak/web";
import { useNavigate } from "react-router";

type DashboardHeaderProps = {
  name: string;
  nextActivity?: Activity;
};

export default function DashboardHeader({
  name,
  nextActivity,
}: DashboardHeaderProps) {
  const { t } = useTranslation();
  const { keycloak, initialized } = useKeycloak();
  const navigate = useNavigate();
  
  const [loading, setLoading] = useState<boolean>(true);

  const [outstandingPayments, setOutstandingPayments] = useState<number>(0);
  const [pastEnrollmentAmount, setPastEnrollmentAmount] = useState<number>(0);
  const [comingEnrollmentAmount, setComingEnrollmentAmount] = useState<number>(0);
  const [unpaidActivityIds, setUnpaidActivityIds] = useState<number[]>([]);

  useEffect(() => {
    async function loadData() {
      if (!initialized || !keycloak.authenticated) return;

      try {
        const outstandingPaymentsResponse = await getApiPaymentsUnpaid();

        if (outstandingPaymentsResponse.data) {
          setOutstandingPayments(outstandingPaymentsResponse.data.reduce((total, payment) => total + (payment.balance || 0), 0));
          setUnpaidActivityIds(outstandingPaymentsResponse.data.map(payment => payment.enrollment?.activityId ?? 0));
        }

        const enrollmentAmountResponse = await getApiEnrollments();

        if (enrollmentAmountResponse.data) {
          setPastEnrollmentAmount(enrollmentAmountResponse.data.filter(enrollment => {
            const activityDate = new Date(enrollment.activity?.dateTimeStart ?? new Date());
            return activityDate < new Date();
          }).length);
          setComingEnrollmentAmount(enrollmentAmountResponse.data.filter(enrollment => {
            const activityDate = new Date(enrollment.activity?.dateTimeStart ?? new Date());
            return activityDate > new Date();
          }).length);
        }
      } catch (error) {
        console.error("Error while loading outstanding payments:", error);
        setOutstandingPayments(0);
      } finally {
        setLoading(false);
      }
    }

    loadData();
  }, [initialized, keycloak.authenticated]);

  const [loadingPayments, setLoadingPayments] = useState<boolean>(false);

  const payActivities = async () => {
    try {
      setLoadingPayments(true);
      const urlResponse = await postApiPaymentsActivity({
        body: {
          memberId: keycloak.tokenParsed?.memberId ?? 0,
          activityIds: unpaidActivityIds
        }
      });

      if (urlResponse.data) {
        navigate(urlResponse.data);
      }
    } catch (error) {
      console.error("Error while initiating payment:", error);
    }
    finally {
      setLoadingPayments(false);
    }
  }

  return (
    <Tile className="w-full m-0 bg-[linear-gradient(color-mix(in_srgb,var(--board-primary),white_20%),var(--board-primary))] text-white">
      <div className="flex lg:flex-row flex-col gap-5">
        <div className="flex flex-col gap-5 grow basis-0">
          {/* Greeting */}
          <p className="text-2xl font-semibold">Hey {name}!</p>

          {/* Stats */}
          <div className="flex gap-5">
            {/* Activity Enrollments */}
            <Tile className="bg-(--board-primary-light) border-2 border-white/20 grow">
              <p>{t("enrollments")}</p>
              <div className="flex items-center gap-2">
                <p className="text-2xl">{loading ? t("loading") : comingEnrollmentAmount}</p>
                <CircleCheckBig />
              </div>
            </Tile>

            {/* Attended Activities */}
            <Tile className="bg-(--board-primary-light) border-2 border-white/20 grow">
              <p>{t("attended")}</p>
              <div className="flex items-center gap-2">
                <p className="text-2xl">{loading ? t("loading") : pastEnrollmentAmount}</p>
                <TrendingUp />
              </div>
            </Tile>
          </div>

          {/* Outstanding Payments */}
          <Tile className="bg-(--board-primary-light) border-2 border-white/20 grow">
            <div className="flex justify-between items-center">
              <div>
                <p>{t("outstanding_payments")}</p>
                <p>{loading ? t("loading") : `€${outstandingPayments.toFixed(2)}`}</p>
              </div>
              <Button onClick={payActivities} disabled={loadingPayments || unpaidActivityIds.length === 0}>
                {loadingPayments ? t("paying") : t("pay")}
              </Button>
            </div>
          </Tile>
        </div>

        {/* Next Activity Details */}
        {nextActivity && (
          <Tile className="flex flex-col gap-4 bg-(--board-primary-light) border-2 border-white/20 grow basis-0">
            <div className="flex items-center gap-2">
              <Clock /> {t("upcoming_activity")}
            </div>
            <p className="truncate">{nextActivity.name}</p>
            <div className="flex items-center gap-2">
              <Calendar /> {formatDate(new Date(nextActivity.dateTimeStart ?? new Date()), "fullDateTime")}
            </div>
            <div className="flex items-center gap-2">
              <UsersRound />{" "}
              {nextActivity.participantLimit
                ? nextActivity.participantLimit -
                  (nextActivity.enrollments?.length ?? 0)
                : 0}{" "}
              {t("of_the")} {nextActivity.participantLimit} {t("available")}
            </div>
            <Button showArrow={true}>{t("view_details")}</Button>
          </Tile>
        )}
      </div>
    </Tile>
  );
}
