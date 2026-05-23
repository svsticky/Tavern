import Cookies from "js-cookie";
import { useEffect, useState } from "react";
import { Navigate, Outlet, useNavigate } from "react-router";
import { client } from "~/api/client.gen";
import {
  getMembersById,
  getSettingsById,
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

  const [paymentUrl, setPaymentUrl] = useState<string | null>(null);

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

    if (tokenParsed.access_level === "not_paid") {
      console.log(
        "User has not paid for membership, redirecting to payment page if payment isn't expired...",
      );

      postPaymentsMembership({ body: { memberId: tokenParsed.UserId } })
        .then((res) => {
          console.log("Received response from payment API:", res);
          if (res.data?.checkoutUrl) {
            console.log("Redirecting to checkout URL:", res.data.checkoutUrl);
            setPaymentUrl(res.data.checkoutUrl);
          } else {
            throw new Error(
              "No checkout URL received, cannot redirect to payment page.",
            );
          }
        })
        .catch((err) => {
          console.error("Error checking membership payment status:", err);
          authService.logout('/login');
        });

      return;
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
            console.log("Board group id loaded:", res.data);
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
            console.log("Candidate Board Group ID loaded:", res.data);
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

    const resInterceptor = client.instance.interceptors.response.use(
      async (response) => {
        return response;
      },
      (error) => {
        if (error.response) {
          if (error.response.status === 401) {
            console.warn("Unauthorized, redirecting...");
            window.location.href = `/logout`;
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
    authService,
    member,
    setBoardGroupId,
    setCandidateBoardGroupId,
    setMember,
  ]);

  if (!tokenParsed) return null;

  if (tokenParsed.access_level === "not_paid") {
    return (
      <div className="flex flex-col items-center justify-center h-screen">
        <h1 className="text-2xl font-bold mb-4">
          {i18n.t("membership_payment_required")}
        </h1>
        <p className="mb-6">{i18n.t("membership_payment_description")}</p>
      </div>
    );
  }

  if (!authService.isAuthenticated()) {
    return <Navigate to="/login" replace />;
  }

  return <Outlet />;
}
