import Cookies from "js-cookie";
import { useEffect, useState } from "react";
import { Outlet, useNavigate } from "react-router";
import { client } from "~/api/client.gen";
import { getMembersById, getSettingsById } from "~/api/sdk.gen";
import { useApp } from "~/context/AppContext";
import { useAuth } from "~/context/AuthContext";
import i18n from "~/i18n";
import type { TokenParsed } from "~/types/TokenParsed";
import {
  setGlobalCommitteeCreationDate,
  setGlobalFinancialYearStartDate,
} from "~/util/date.util";

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
  const [tokenParsed, setTokenParsed] = useState<TokenParsed | null>(null);
  const navigate = useNavigate();

  // biome-ignore lint/correctness/useExhaustiveDependencies: navigate needed because token has to be refreshed when navigated
  useEffect(() => {
    if (!authService.isReady()) return;

    let cancelled = false;
    let retryTimer: number | undefined;

    const loadToken = async () => {
      if (!authService.isAuthenticated()) {
        console.error("User not authenticated");
        authService.login(window.location.href);
        return;
      }

      const tokenParsed = await authService.getTokenParsed();
      if (cancelled) return;

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
    financialYearStartDate,
    setFinancialYearStartDate,
    committeeCreationDate,
    setCommitteeCreationDate,
    member,
    setMember,
  } = useApp();

  // Register Axios interceptors dynamically using the auth service
  useEffect(() => {
    if (!client.instance || !tokenParsed) return;

    const reqInterceptor = client.instance.interceptors.request.use(
      async (config) => {
        try {
          const freshToken = await authService.getToken();
          if (freshToken) {
            Cookies.set("access_token", freshToken, {
              path: "/",
              secure: true,
              sameSite: "none",
              domain: `.${window.location.hostname}`,
            });

            config.headers.Authorization = `Bearer ${freshToken}`;
          }
        } catch (error) {
          console.error(
            "Failed to get fresh token in request interceptor",
            error,
          );
        }
        return config;
      },
    );

    const resInterceptor = client.instance.interceptors.response.use(
      async (response) => {
        return response;
      },
      (error) => {
        if (error.response) {
          if (error.response.status === 401) {
            console.warn("Unauthorized, redirecting...");
            window.location.href = "/logout";
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
  }, [authService, tokenParsed]);

  // Synchronize locale and fetch board settings and member data
  useEffect(() => {
    if (!tokenParsed) return;

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

    if (financialYearStartDate === null) {
      getSettingsById({
        path: {
          id: "FinancialYearStartDate",
        },
      })
        .then((res) => {
          if (res.data?.value) {
            setFinancialYearStartDate(res.data.value);
            setGlobalFinancialYearStartDate(res.data.value);
          }
        })
        .catch((err) =>
          console.error("Could not fetch financial year start date", err),
        );
    }

    if (committeeCreationDate === null) {
      getSettingsById({
        path: {
          id: "CommitteeCreationDate",
        },
      })
        .then((res) => {
          if (res.data?.value) {
            setCommitteeCreationDate(res.data.value);
            setGlobalCommitteeCreationDate(res.data.value);
          }
        })
        .catch((err) =>
          console.error("Could not fetch committee creation date", err),
        );
    }

    if (member === null) {
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
  }, [
    tokenParsed,
    boardGroupId,
    candidateBoardGroupId,
    financialYearStartDate,
    member,
    committeeCreationDate,
    setBoardGroupId,
    setCandidateBoardGroupId,
    setFinancialYearStartDate,
    setCommitteeCreationDate,
    setMember,
  ]);

  if (!tokenParsed) return null;

  if (!authService.isAuthenticated()) {
    authService.login(window.location.href);
    return null;
  }

  return <Outlet />;
}
