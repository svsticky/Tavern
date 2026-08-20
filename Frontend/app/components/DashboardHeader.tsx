import {
  Calendar,
  CircleCheckBig,
  Clock,
  TrendingUp,
  UsersRound,
} from "lucide-react";
import { useEffect, useState } from "react";
import toast from "react-hot-toast";
import { useTranslation } from "react-i18next";
import { useNavigate } from "react-router";
import {
  type ActivityResponseDto,
  getEnrollments,
  getPaymentsUnpaid,
  postPaymentsActivity,
} from "~/api";
import { useAuth } from "~/context/AuthContext";
import type { TokenParsed } from "~/types/TokenParsed";
import { formatDate } from "~/util/date.util";
import { appendErrorMessage } from "~/util/error.util";
import Tile from "./Tiles/Tile";
import Button from "./UI/Button";

/**
 * Props for the DashboardHeader component.
 * @interface DashboardHeaderProps
 * @property {string} name - The display name of the user to be greeted.
 * @property {ActivityResponseDto} [nextActivity] - Data for the user's next scheduled activity, if one exists.
 */
type DashboardHeaderProps = {
  name: string;
  nextActivity?: ActivityResponseDto;
};

// After returning from a Mollie checkout, the payment webhook may not have landed yet, so a
// single immediate status fetch can still show the activity as unpaid. Poll instead of trusting
// the first response - webhook delivery has been observed to take up to ~30s, so the window needs
// enough margin to reliably cover that rather than giving up early.
const PAYMENT_RETURN_POLL_INTERVAL_MS = 1500;
const PAYMENT_RETURN_MAX_POLL_ATTEMPTS = 24;

// Rejects with the abort reason if `signal` fires before `ms` elapses, so a superseded
// effect run doesn't sit out the wait before it can bail out.
function sleep(ms: number, signal: AbortSignal) {
  return new Promise<void>((resolve, reject) => {
    if (signal.aborted) {
      reject(signal.reason);
      return;
    }
    const timeout = setTimeout(resolve, ms);
    signal.addEventListener("abort", () => {
      clearTimeout(timeout);
      reject(signal.reason);
    });
  });
}

/**
 * The primary hero section for the user dashboard.
 *
 * This component provides a high-level summary of the user's account status, including:
 * - **Greeting**: Personalized welcome message.
 * - **Activity Stats**: Counts of upcoming and past enrollments.
 * - **Financial Summary**: Outstanding balance calculation with a "Pay" action that handles redirecting to a checkout URL.
 * - **Next Activity Highlight**: A specialized card showing details and a quick-link to the most immediate upcoming event.
 *
 * It manages its own data fetching state for payments and enrollment totals.
 *
 * @component
 * @param {DashboardHeaderProps} props - The component properties.
 */
export default function DashboardHeader({
  name,
  nextActivity,
}: DashboardHeaderProps) {
  const { t } = useTranslation();
  const authService = useAuth();
  const [tokenParsed, setTokenParsed] = useState<TokenParsed | null>(null);
  const navigate = useNavigate();

  const [loading, setLoading] = useState<boolean>(true);
  const [confirmingPayment, setConfirmingPayment] = useState<boolean>(false);

  const [outstandingPayments, setOutstandingPayments] = useState<number>(0);
  const [pastEnrollmentAmount, setPastEnrollmentAmount] = useState<number>(0);
  const [comingEnrollmentAmount, setComingEnrollmentAmount] =
    useState<number>(0);
  const [unpaidActivityIds, setUnpaidActivityIds] = useState<number[]>([]);

  // t is intentionally omitted from the deps below: i18next-http-backend loads
  // translations over HTTP, so t gets a new reference shortly after mount once that
  // resolves - depending on it here would restart this fetch/poll cycle for an
  // unrelated reason. t is still used inside via closure for the (rare) error toast.
  // biome-ignore lint/correctness/useExhaustiveDependencies: see comment above
  useEffect(() => {
    const controller = new AbortController();
    const { signal } = controller;

    async function loadData() {
      if (!authService.isAuthenticated()) {
        setLoading(false);
        return;
      }

      const parsedToken = await authService.getTokenParsed();
      if (signal.aborted) return;

      setTokenParsed(parsedToken);

      if (!parsedToken) {
        console.error("Failed to parse token");
        setLoading(false);
        return;
      }

      const returningFromPayment =
        new URLSearchParams(window.location.search).get("paymentReturn") ===
        "activity";

      try {
        let attempt = 0;

        while (true) {
          const [outstandingPaymentsResponse, enrollmentAmountResponse] =
            await Promise.all([
              getPaymentsUnpaid({ signal }),
              getEnrollments({
                query: {
                  FromMemberId: parsedToken.UserId,
                },
                signal,
              }),
            ]);

          if (outstandingPaymentsResponse.error) {
            throw new Error(
              String(outstandingPaymentsResponse.message) ||
                "Failed to load outstanding payments",
            );
          }

          if (enrollmentAmountResponse.error) {
            throw new Error(
              String(enrollmentAmountResponse.message) ||
                "Failed to load enrollments",
            );
          }

          attempt++;
          const stillUnpaid =
            (outstandingPaymentsResponse.data?.length ?? 0) > 0;
          const isLastAttempt =
            !returningFromPayment ||
            !stillUnpaid ||
            attempt >= PAYMENT_RETURN_MAX_POLL_ATTEMPTS;

          if (isLastAttempt) {
            if (outstandingPaymentsResponse.data) {
              setOutstandingPayments(
                outstandingPaymentsResponse.data.reduce(
                  (total, payment) => total + (payment.balance || 0),
                  0,
                ),
              );
              setUnpaidActivityIds(
                outstandingPaymentsResponse.data.map(
                  (payment) => payment.enrollment.activityId,
                ),
              );
            }

            if (enrollmentAmountResponse.data) {
              const now = Date.now();
              let past = 0;
              let coming = 0;
              for (let i = 0; i < enrollmentAmountResponse.data.length; i++) {
                const enrollment = enrollmentAmountResponse.data[i];
                const activityDate = new Date(
                  enrollment.activity.dateTimeEnd,
                ).getTime();
                if (activityDate < now) {
                  past++;
                } else {
                  coming++;
                }
              }
              setPastEnrollmentAmount(past);
              setComingEnrollmentAmount(coming);
            } else {
              throw new Error("No enrollment data returned from API");
            }

            break;
          }

          setConfirmingPayment(true);
          await sleep(PAYMENT_RETURN_POLL_INTERVAL_MS, signal);
        }

        setConfirmingPayment(false);

        if (returningFromPayment) {
          const url = new URL(window.location.href);
          url.searchParams.delete("paymentReturn");
          window.history.replaceState({}, "", url.toString());
        }
      } catch (error) {
        // A superseded run (aborted on cleanup) rejects here via the aborted
        // fetches/sleep - bail out silently instead of writing stale state or an
        // error toast for a run nobody's waiting on anymore.
        if (signal.aborted) return;

        console.error("Error while loading outstanding payments:", error);
        setOutstandingPayments(0);

        toast.error(appendErrorMessage(t("dashboard_data_load_error"), error));
      } finally {
        if (!signal.aborted) {
          setLoading(false);
          setConfirmingPayment(false);
        }
      }
    }

    loadData();
    return () => {
      controller.abort();
    };
  }, [authService]);

  const [loadingPayments, setLoadingPayments] = useState<boolean>(false);

  const payActivities = async () => {
    if (!tokenParsed) return;

    const payAction = async () => {
      try {
        setLoadingPayments(true);
        const urlResponse = await postPaymentsActivity({
          body: {
            memberId: tokenParsed.UserId,
            activityIds: unpaidActivityIds,
          },
        });

        if (urlResponse.error) {
          throw new Error("Failed to initiate payment process");
        }

        if (urlResponse.data?.checkoutUrl) {
          window.location.href = urlResponse.data.checkoutUrl;
          return;
        }

        throw new Error("No checkout URL returned from API");
      } finally {
        setLoadingPayments(false);
      }
    };

    toast.promise(payAction(), {
      loading: t("paying"),
      success: t("redirecting_to_payment"),
      error: (error) =>
        appendErrorMessage(t("payment_initiation_failed"), error),
    });
  };

  return (
    <Tile className="w-full m-0 bg-[linear-gradient(color-mix(in_srgb,var(--board-primary),white_20%),var(--board-primary))] text-white">
      <div className="flex lg:flex-row flex-col gap-5">
        <div className="flex flex-col gap-5 grow basis-0">
          {/* Greeting */}
          <p className="text-2xl font-semibold">Hey {name}!</p>

          {/* Stats */}
          <div className="flex flex-col min-[380px]:flex-row gap-5">
            {/* Activity Enrollments */}
            <Tile className="bg-(--board-primary-light) border-2 border-white/20 grow">
              <p>{t("enrollments")}</p>
              <div className="flex items-center gap-2">
                <p className="text-2xl">
                  {loading ? t("loading") : comingEnrollmentAmount}
                </p>
                <CircleCheckBig />
              </div>
            </Tile>

            {/* Attended Activities */}
            <Tile className="bg-(--board-primary-light) border-2 border-white/20 grow">
              <p>{t("attended")}</p>
              <div className="flex items-center gap-2">
                <p className="text-2xl">
                  {loading ? t("loading") : pastEnrollmentAmount}
                </p>
                <TrendingUp />
              </div>
            </Tile>
          </div>

          {/* Outstanding Payments */}
          <Tile className="bg-(--board-primary-light) border-2 border-white/20 grow">
            <div className="flex justify-between flex-col w-full min-[330px]:flex-row">
              <div>
                <p>{t("outstanding_payments")}</p>
                <p>
                  {loading
                    ? confirmingPayment
                      ? t("confirming_payment")
                      : t("loading")
                    : `€${outstandingPayments.toFixed(2)}`}
                </p>
              </div>
              <Button
                onClick={payActivities}
                variant="secondary"
                disabled={
                  loading || loadingPayments || unpaidActivityIds.length === 0
                }
              >
                {loadingPayments ? t("paying") : t("pay")}
              </Button>
            </div>
          </Tile>
        </div>

        {/* Next Activity Details */}
        {nextActivity && (
          <Tile className="flex flex-col gap-4 bg-(--board-primary-light) border-2 border-white/20 grow basis-0 hidden lg:flex">
            <div className="flex items-center gap-2">
              <Clock /> {t("upcoming_activity")}
            </div>
            <p className="truncate">{nextActivity.name}</p>
            <div className="flex items-center gap-2">
              <Calendar />{" "}
              {formatDate(new Date(nextActivity.dateTimeStart), "fullDateTime")}
            </div>
            <div className="flex items-center gap-2">
              <UsersRound />{" "}
              {nextActivity.participantLimit
                ? `${nextActivity.enrollments.filter((e) => !e.isOnWaitingList).length} ${t("of_the")} ${nextActivity.participantLimit} ${t("participants")}`
                : `${nextActivity.enrollments.filter((e) => !e.isOnWaitingList).length} ${t("participants")}`}
            </div>
            <Button
              variant="secondary"
              showArrow={true}
              onClick={() => navigate(`/activities/${nextActivity.id}`)}
            >
              {t("view_details")}
            </Button>
          </Tile>
        )}
      </div>
    </Tile>
  );
}
