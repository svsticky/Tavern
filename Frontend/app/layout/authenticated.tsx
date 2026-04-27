import { useKeycloak } from "@react-keycloak/web";
import { useEffect, useState } from "react";
import { Navigate, Outlet } from "react-router";
import { client } from "~/api/client.gen";
import { getApiMembersById, getApiSettingsById, postApiPaymentsMembership } from "~/api/sdk.gen";
import Button from "~/components/UI/Button";
import { useApp } from "~/context/AppContext";
import i18n from "~/i18n";

export default function AuthenticatedLayout() {
  const { keycloak, initialized } = useKeycloak();

  const [paymentUrl, setPaymentUrl] = useState<string | null>(null);

  const { boardGroupId, setBoardGroupId, candidateBoardGroupId, setCandidateBoardGroupId, member, setMember } = useApp();

  useEffect(() => {
    if (!initialized || !client.instance) return;

    const reqInterceptor = client.instance.interceptors.request.use((config) => {
      if (keycloak.token) {
        document.cookie = `access_token=${keycloak.token}; path=/; Secure; SameSite=None`;
        config.headers.Authorization = `Bearer ${keycloak.token}`;
      }
      return config;
    });

    const userLocale = keycloak.tokenParsed?.locale?.toLowerCase();
    if (userLocale && i18n.language !== userLocale) {
      i18n.changeLanguage(userLocale);
    }

    if (keycloak.tokenParsed?.access_level === "not_paid") {
      console.log("User has not paid for membership, redirecting to payment page if payment isn't expired...");
      
      postApiPaymentsMembership({
        body: { memberId: keycloak.tokenParsed?.UserId ?? "" }
      }).then(res => {
        console.log("Received response from payment API:", res);
        if (res.data?.checkoutUrl) {
          console.log("Redirecting to checkout URL:", res.data.checkoutUrl);
          setPaymentUrl(res.data.checkoutUrl);
        } else{
          throw new Error("No checkout URL received, cannot redirect to payment page.");
        }
      }).catch(err => {
        console.error("Error checking membership payment status:", err);
        keycloak.logout({
          redirectUri: "/login"
        });
      });

      return;
    }

    if (boardGroupId === null) {
      getApiSettingsById({
        path: {
          id: "BoardGroupId"
        }
      })
        .then(res => {
          if (res.data) {
            if (!res.data.value || isNaN(parseInt(res.data.value))) {
              console.error("Invalid board group ID:", res.data.value);
              return;
            }
            setBoardGroupId(parseInt(res.data.value));
            console.log("Board group id loaded:", res.data);
          }
        })
        .catch(err => console.error("Could not fetch board group ID", err));
    }

    if(candidateBoardGroupId === null) {
      getApiSettingsById({
        path: {
          id: "CandidateBoardGroupId"
        }
      })
        .then(res => {
          if (res.data) {
            if (!res.data.value || isNaN(parseInt(res.data.value))) {
              console.error("Invalid candidate board group ID:", res.data.value);
              return;
            }
            setCandidateBoardGroupId(parseInt(res.data.value));
            console.log("Candidate Board Group ID loaded:", res.data);
          }
        })
        .catch(err => console.error("Could not fetch candidate board group ID", err));
    }

    if(member == null) {
      getApiMembersById({
        path: {
          id: keycloak.tokenParsed?.UserId ?? ""
        }
      }).then(res => {
        if(res.data) {
          setMember(res.data);
        }
      }).catch(err => console.error("Could not fetch member data for authenticated user", err));
    }

    const resInterceptor = client.instance.interceptors.response.use(
      async (response) => {
        return response;
      },
      (error) => {
        if (error.response) {
          if(error.response.status === 401) {
            console.warn("Unauthorized, redirecting...");
            window.location.href = `/logout`; 
          }
          else if (error.response.status === 403) {
            console.warn("Forbidden - user does not have access to this resource.");
            window.location.href = `/`;
          }
        }

        return Promise.reject(error);
      }
    );

    return () => {
      client.instance.interceptors.request.eject(reqInterceptor);
      client.instance.interceptors.response.eject(resInterceptor);
    };
  }, [initialized, keycloak.token, boardGroupId, candidateBoardGroupId]);

  if (!initialized) return null;

  if (keycloak.tokenParsed?.access_level === "not_paid") {
    return (
      <div className="flex flex-col items-center justify-center h-screen">
        <h1 className="text-2xl font-bold mb-4">{i18n.t("membership_payment_required")}</h1>
        <p className="mb-6">{i18n.t("membership_payment_description")}</p>
        {paymentUrl && (
          <Button
            onClick={() => keycloak.logout({
              redirectUri: paymentUrl 
            })} 
          >
            {i18n.t("pay")}
          </Button>
        )}
      </div>
    )
  }

  if (!keycloak.authenticated) {
    return <Navigate to="/login" replace />;
  }
    
  return <Outlet />;
}