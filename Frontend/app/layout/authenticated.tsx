import Cookies from "js-cookie";
import { useEffect, useState } from "react";
import toast from "react-hot-toast";
import { Navigate, Outlet, useNavigate } from "react-router";
import { client } from "~/api/client.gen";
import {
  deleteMembersById,
  getMembersById,
  getPaymentsMemberByFromUserIdStatus,
  getSettingsById,
  patchMembersById,
  postPaymentsMembership,
} from "~/api/sdk.gen";
import Button from "~/components/UI/Button";
import { useApp } from "~/context/AppContext";
import { useAuth } from "~/context/AuthContext";
import i18n from "~/i18n";
import type { TokenParsed } from "~/types/TokenParsed";

/**
 * The core layout wrapper for all authenticated routes in the application.
 *
 * This component orchestrates several critical middleware-like functions:
 * - **API Interceptors**: Synchronizes the token with the Axios client for all outgoing requests.
 * - **Locale Synchronization**: Updates the application language based on the user's profile.
 * - **Membership Enforcement**: Detects "not_paid" status and forces a redirect to a payment checkout.
 * - **Global Hydration**: Fetches essential system settings (Board IDs) and the user's full member profile
 *   into the `AppContext` on initial load.
 * - **Security Error Handling**: Manages global response interceptors to catch 401 (Unauthorized)
 *   and 403 (Forbidden) errors, triggering appropriate redirects.
 *
 * @component
 */
export default function AuthenticatedLayout() {
  const authService = useAuth();
  const [token, setToken] = useState<string | null>(null);
  const [tokenParsed, setTokenParsed] = useState<TokenParsed | null>(null);
  const [paymentUrl, setPaymentUrl] = useState<string | null>(null);
  const [paymentStatus, setPaymentStatus] = useState<boolean | null>(null);
  const navigate = useNavigate();

  useEffect(() => {
    if (!authService.isReady()) return;

    let cancelled = false;
    let retryTimer: number | undefined;

    const loadToken = async () => {
      if (!authService.isAuthenticated()) {
        console.error("User not authenticated");
        navigate("/login", { replace: true });
        return;
      }

      const token = await authService.getToken();
      const tokenParsed = await authService.getTokenParsed();
      if (cancelled) return;

      setToken(token);

      if (!tokenParsed) {
        retryTimer = window.setTimeout(() => {
          if (!cancelled) loadToken();
        }, 250);
        return;
      }

      setTokenParsed(tokenParsed);
    };

    loadToken();

    return () => {
      cancelled = true;
      if (retryTimer !== undefined) {
        window.clearTimeout(retryTimer);
      }
    };
  }, [authService, navigate]);

  const {
    boardGroupId,
    setBoardGroupId,
    candidateBoardGroupId,
    setCandidateBoardGroupId,
    member,
    setMember,
  } = useApp();

  useEffect(() => {
    if (!client.instance || !tokenParsed) return;

    const reqInterceptor = client.instance.interceptors.request.use(
      async (config) => {
        if (token) {
          Cookies.set("access_token", token, {
            path: "/",
            secure: true,
            sameSite: "none",
            domain: `.${window.location.hostname}`,
          });

          config.headers.Authorization = `Bearer ${token}`;
        }
        return config;
      },
    );

    const preferredLocale = member?.preferredLanguage?.toLowerCase();
    const userLocale = preferredLocale || tokenParsed.locale?.toLowerCase();
    if (userLocale && i18n.language !== userLocale) {
      i18n.changeLanguage(userLocale);
    }

    if (boardGroupId === null) {
      getSettingsById({
        path: {
          id: "BoardGroupId",
        },
      })
        .then((res) => {
          if (res.data) {
            if (!res.data.value || Number.isNaN(parseInt(res.data.value, 10))) {
              console.error("Invalid board group ID:", res.data.value);
              return;
            }
            setBoardGroupId(parseInt(res.data.value, 10));
          }
        })
        .catch((err) => console.error("Could not fetch board group ID", err));
    }

    if (candidateBoardGroupId === null) {
      getSettingsById({
        path: {
          id: "CandidateBoardGroupId",
        },
      })
        .then((res) => {
          if (res.data) {
            if (!res.data.value || Number.isNaN(parseInt(res.data.value, 10))) {
              console.error(
                "Invalid candidate board group ID:",
                res.data.value,
              );
              return;
            }
            setCandidateBoardGroupId(parseInt(res.data.value, 10));
          }
        })
        .catch((err) =>
          console.error("Could not fetch candidate board group ID", err),
        );
    }

    if (member == null) {
      getMembersById({
        path: {
          id: tokenParsed.UserId,
        },
      })
        .then((res) => {
          if (res.data) {
            setMember(res.data);
          }
        })
        .catch((err) =>
          console.error(
            "Could not fetch member data for authenticated user",
            err,
          ),
        );
    }

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

    const resInterceptor = client.instance.interceptors.response.use(
      async (response) => {
        return response;
      },
      (error) => {
        if (error.response) {
          if (error.response.status === 401) {
            console.warn("Unauthorized, redirecting...");
          } else if (error.response.status === 403) {
            console.warn(
              "Forbidden - user does not have access to this resource.",
            );
            window.location.href = `/`;
          }
        }

        return Promise.reject(error);
      },
    );

    return () => {
      client.instance.interceptors.request.eject(reqInterceptor);
      client.instance.interceptors.response.eject(resInterceptor);
    };
  }, [
    token,
    tokenParsed,
    boardGroupId,
    candidateBoardGroupId,
    member,
    setBoardGroupId,
    setCandidateBoardGroupId,
    setMember,
    paymentUrl,
    paymentStatus,
  ]);

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
                    throw new Error(
                      `Failed to delete account: ${response.error}`,
                    );
                  }

                  toast.success(i18n.t("account_deleted_successfully"));
                  authService.logout(`${window.location.origin}/login`);
                } catch (err) {
                  console.error("Error deleting account:", err);
                  toast.error(i18n.t("delete_account_error"));
                }
              }}
            >
              Delete Account
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

  if (!authService.isAuthenticated()) {
    return <Navigate to="/login" replace />;
  }

  return <Outlet />;
}
