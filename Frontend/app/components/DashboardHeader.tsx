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
 * @property {number} outstandingPayments - The total outstanding balance across unpaid activity enrollments, as of the route's `clientLoader` fetch.
 * @property {number[]} unpaidActivityIds - The activity ids backing `outstandingPayments`, sent to the payment endpoint when the user pays.
 * @property {number} pastEnrollmentAmount - How many of the user's enrollments are for activities that have already ended.
 * @property {number} comingEnrollmentAmount - How many of the user's enrollments are for activities still upcoming.
 */
type DashboardHeaderProps = {
  name: string;
  nextActivity?: ActivityResponseDto;
  outstandingPayments: number;
  unpaidActivityIds: number[];
  pastEnrollmentAmount: number;
  comingEnrollmentAmount: number;
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
 * Payment/enrollment totals arrive as props from the route's `clientLoader`; this
 * component only re-fetches on its own when returning from a Mollie checkout, to
 * poll for the payment webhook landing.
 *
 * @component
 * @param {DashboardHeaderProps} props - The component properties.
 */
export default function DashboardHeader({
  name,
  nextActivity,
  outstandingPayments: initialOutstandingPayments,
  unpaidActivityIds: initialUnpaidActivityIds,
  pastEnrollmentAmount,
  comingEnrollmentAmount,
}: DashboardHeaderProps) {
  const { t } = useTranslation();
  const authService = useAuth();
  const [tokenParsed, setTokenParsed] = useState<TokenParsed | null>(null);
  const navigate = useNavigate();

  const [confirmingPayment, setConfirmingPayment] = useState<boolean>(false);
  const [outstandingPayments, setOutstandingPayments] = useState<number>(
    initialOutstandingPayments,
  );
  const [unpaidActivityIds, setUnpaidActivityIds] = useState<number[]>(
    initialUnpaidActivityIds,
  );

  useEffect(() => {
    let cancelled = false;
    authService.getTokenParsed().then((parsedToken) => {
      if (!cancelled) setTokenParsed(parsedToken);
    });
    return () => {
      cancelled = true;
    };
  }, [authService]);

  // After returning from a Mollie checkout, the payment webhook may not have landed yet, so the
  // route's clientLoader snapshot can still show the activity as unpaid. Poll just the payments
  // endpoint (not the whole loader - enrollment counts can't change from a payment webhook)
  // instead of trusting that first snapshot - webhook delivery has been observed to take up to
  // ~30s, so the window needs enough margin to reliably cover that rather than giving up early.
  // t is intentionally omitted from the deps below: i18next-http-backend loads translations over
  // HTTP, so t gets a new reference shortly after mount once that resolves - depending on it here
  // would restart this poll for an unrelated reason. t is still used inside via closure for the
  // (rare) error toast.
  // biome-ignore lint/correctness/useExhaustiveDependencies: see comment above
  useEffect(() => {
    const returningFromPayment =
      new URLSearchParams(window.location.search).get("paymentReturn") ===
      "activity";
    if (!returningFromPayment) return;

    const controller = new AbortController();
    const { signal } = controller;

    async function pollUntilSettled() {
      try {
        let attempt = 0;

        while (true) {
          const outstandingPaymentsResponse = await getPaymentsUnpaid({
            signal,
          });

          if (outstandingPaymentsResponse.error) {
            throw new Error(
              String(outstandingPaymentsResponse.message) ||
                "Failed to load outstanding payments",
            );
          }

          attempt++;
          const stillUnpaid =
            (outstandingPaymentsResponse.data?.length ?? 0) > 0;
          const isLastAttempt =
            !stillUnpaid || attempt >= PAYMENT_RETURN_MAX_POLL_ATTEMPTS;

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

            break;
          }

          setConfirmingPayment(true);
          await sleep(PAYMENT_RETURN_POLL_INTERVAL_MS, signal);
        }

        const url = new URL(window.location.href);
        url.searchParams.delete("paymentReturn");
        window.history.replaceState({}, "", url.toString());
      } catch (error) {
        // A superseded run (aborted on cleanup) rejects here via the aborted
        // fetches/sleep - bail out silently instead of writing stale state or an
        // error toast for a run nobody's waiting on anymore.
        if (signal.aborted) return;

        console.error("Error while confirming payment:", error);
        toast.error(appendErrorMessage(t("dashboard_data_load_error"), error));
      } finally {
        if (!signal.aborted) {
          setConfirmingPayment(false);
        }
      }
    }

    pollUntilSettled();
    return () => {
      controller.abort();
    };
  }, []);

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
                <p className="text-2xl">{comingEnrollmentAmount}</p>
                <CircleCheckBig />
              </div>
            </Tile>

            {/* Attended Activities */}
            <Tile className="bg-(--board-primary-light) border-2 border-white/20 grow">
              <p>{t("attended")}</p>
              <div className="flex items-center gap-2">
                <p className="text-2xl">{pastEnrollmentAmount}</p>
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
                  {confirmingPayment
                    ? t("confirming_payment")
                    : `€${outstandingPayments.toFixed(2)}`}
                </p>
              </div>
              <Button
                onClick={payActivities}
                variant="secondary"
                disabled={
                  confirmingPayment ||
                  loadingPayments ||
                  unpaidActivityIds.length === 0
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
