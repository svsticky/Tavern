import { useEffect, useState } from "react";
import toast from "react-hot-toast";
import { Outlet } from "react-router";
import { client } from "~/api/client.gen";
import {
  deleteMembersById,
  getPaymentsMemberByFromUserIdStatus,
  getSettingsById,
  patchMembersById,
  postPaymentsBegunstiger,
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
  const [canPayMembership, setCanPayMembership] = useState<boolean | null>(
    null,
  );
  const [isBegunstiger, setIsBegunstiger] = useState(false);
  const [mainBoardMail, setMainBoardMail] = useState<string | null>(null);

  useEffect(() => {
    const loadToken = async () => {
      if (!authService) return;

      setTokenParsed(await authService.getTokenParsed());
    };

    loadToken();
  }, [authService]);

  useEffect(() => {
    if (!client.instance) return;

    getSettingsById({ path: { id: "MainBoardMail" } })
      .then((res) => {
        if (res.data?.value) {
          setMainBoardMail(res.data.value);
        }
      })
      .catch((err) =>
        console.error("Could not fetch main board mail setting", err),
      );
  }, []);

  const deleteAccount = async () => {
    if (!tokenParsed || !authService) return;
    if (!window.confirm(i18n.t("delete_account_confirmation"))) {
      return;
    }

    try {
      const response = await deleteMembersById({
        path: { id: tokenParsed.UserId },
      });

      if (response.error || response.status >= 400) {
        throw response.error ?? new Error("Failed to delete account");
      }

      toast.success(i18n.t("account_deleted_successfully"));
      authService.logout(`${window.location.origin}/login`);
    } catch (err) {
      console.error("Error deleting account:", err);
      toast.error(appendErrorMessage(i18n.t("delete_account_error"), err));
    }
  };

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
            setIsBegunstiger(res.data.isBegunstiger);
            setCanPayMembership(res.data.canPayMembership);
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

    // Members who are neither a begunstiger nor have ever done a study aren't eligible to pay
    // membership at all - don't offer them a checkout.
    if (!paymentUrl && paymentStatus === false && canPayMembership !== false) {
      console.log("User has not paid for membership, loading payment url...");

      // Begunstigers pay their own separate fee
      const createPayment = isBegunstiger
        ? postPaymentsBegunstiger
        : postPaymentsMembership;

      createPayment({
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
  }, [tokenParsed, paymentUrl, paymentStatus, canPayMembership, isBegunstiger]);

  if (!tokenParsed) return null;

  const contactAndDelete = (
    <>
      {mainBoardMail && (
        <p className="mb-6">
          {i18n.t("contact_board_for_questions")}{" "}
          <a href={`mailto:${mainBoardMail}`} className="underline">
            {mainBoardMail}
          </a>
        </p>
      )}
      <Button variant="danger" onClick={deleteAccount}>
        {i18n.t("delete_account")}
      </Button>
    </>
  );

  if (canPayMembership === false) {
    return (
      <div className="flex flex-col items-center justify-center h-screen">
        <h1 className="text-2xl font-bold mb-4">
          {i18n.t("membership_payment_not_eligible_title")}
        </h1>
        <p className="mb-6">
          {i18n.t("membership_payment_not_eligible_description")}
        </p>
        {contactAndDelete}
      </div>
    );
  }

  if (paymentStatus === false) {
    return (
      <div className="flex flex-col items-center justify-center h-screen">
        <h1 className="text-2xl font-bold mb-4">
          {i18n.t("membership_payment_required")}
        </h1>
        <p className="mb-6">{i18n.t("membership_payment_description")}</p>
        {paymentUrl && (
          <Button
            onClick={async () => {
              window.location.href = paymentUrl;
            }}
          >
            {i18n.t("pay")}
          </Button>
        )}
        <p className="text-red-500 text-sm font-bold ">
          {i18n.t("delete_account_instead_of_paying")}
        </p>
        {contactAndDelete}
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
        {contactAndDelete}
      </div>
    );
  }

  return <Outlet />;
}
