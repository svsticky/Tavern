import { useEffect, useState } from "react";
import toast from "react-hot-toast";
import { Outlet } from "react-router";
import { client } from "~/api/client.gen";
import {
  deleteMembersById,
  getPaymentsMemberByFromUserIdStatus,
  patchMembersById,
  postPaymentsMembership,
} from "~/api/sdk.gen";
import Button from "~/components/UI/Button";
import { useAuth } from "~/context/AuthContext";
import i18n from "~/i18n";
import type { TokenParsed } from "~/types/TokenParsed";
import { appendErrorMessage } from "~/util/error.util";

/**
 * The core layout wrapper for all routes you have to pay for in the application.
 *
 * @component
 */
export default function PaywallLayout() {
  const authService = useAuth();
  const [tokenParsed, setTokenParsed] = useState<TokenParsed | null>(null);
  const [paymentUrl, setPaymentUrl] = useState<string | null>(null);
  const [paymentStatus, setPaymentStatus] = useState<boolean | null>(null);

  useEffect(() => {
    const loadToken = async () => {
      if (!authService) return;

      setTokenParsed(await authService.getTokenParsed());
    };

    loadToken();
  }, [authService]);

  useEffect(() => {
    if (!client.instance || !tokenParsed) return;

    if (paymentStatus === null) {
      getPaymentsMemberByFromUserIdStatus({
        path: {
          fromUserId: tokenParsed.UserId,
        },
      })
        .then((res) => {
          if (res.data) {
            setPaymentStatus(
              res.data.hasEverPaidMembership &&
                res.data.hasPaidMembershipBeforeExpirationTime,
            );
          }
        })
        .catch((err) =>
          console.error(
            "Could not fetch payment status for authenticated user",
            err,
          ),
        );

      if (paymentStatus === true && tokenParsed.access_level === "not_paid") {
        console.warn(
          "User has paid for membership but access level is still 'not_paid'. This may indicate a delay in payment processing. Forcing payment status to false to redirect user to payment page.",
        );

        // Patch member to force refresh the payment status in keycloak.
        patchMembersById({
          path: { id: tokenParsed.UserId },
          body: [] as any,
        });
      }
    }

    if (!paymentUrl && paymentStatus === false) {
      console.log("User has not paid for membership, loading payment url...");

      postPaymentsMembership({
        body: { memberId: tokenParsed?.UserId ?? "" },
      })
        .then((res) => {
          if (res.data?.checkoutUrl) {
            setPaymentUrl(res.data.checkoutUrl);
          } else {
            throw new Error(
              "No checkout URL received, cannot redirect to payment page.",
            );
          }
        })
        .catch((err) => {
          console.error("Error checking membership payment status:", err);
        });

      return;
    }
  }, [tokenParsed, paymentUrl, paymentStatus]);

  if (!tokenParsed) return null;

  if (paymentStatus === false) {
    return (
      <div className="flex flex-col items-center justify-center h-screen">
        <h1 className="text-2xl font-bold mb-4">
          {i18n.t("membership_payment_required")}
        </h1>
        <p className="mb-6">{i18n.t("membership_payment_description")}</p>
        {paymentUrl && (
          <>
            <Button
              onClick={async () => {
                window.location.href = paymentUrl;
              }}
            >
              {i18n.t("pay")}
            </Button>
            <p className="text-red-500 text-sm font-bold ">
              {i18n.t("delete_account_instead_of_paying")}
            </p>
            <Button
              variant="danger"
              onClick={async () => {
                if (!window.confirm(i18n.t("delete_account_confirmation"))) {
                  return;
                }

                try {
                  const response = await deleteMembersById({
                    path: { id: tokenParsed.UserId },
                  });

                  if (response.error || response.status >= 400) {
                    throw (
                      response.error ?? new Error("Failed to delete account")
                    );
                  }

                  toast.success(i18n.t("account_deleted_successfully"));
                  authService.logout(`${window.location.origin}/login`);
                } catch (err) {
                  console.error("Error deleting account:", err);
                  toast.error(
                    appendErrorMessage(i18n.t("delete_account_error"), err),
                  );
                }
              }}
            >
              {i18n.t("delete_account")}
            </Button>
          </>
        )}
      </div>
    );
  }

  if (paymentStatus === true && tokenParsed.access_level === "not_paid") {
    return (
      <div className="flex flex-col items-center justify-center h-screen">
        <h1 className="text-2xl font-bold mb-4">
          {i18n.t("payment_not_processed")}
        </h1>
        <p className="mb-6">
          {i18n.t("membership_payment_not_processed_description")}
        </p>
      </div>
    );
  }

  return <Outlet />;
}
