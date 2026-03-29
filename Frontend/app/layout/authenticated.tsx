import { useKeycloak } from "@react-keycloak/web";
import { useEffect } from "react";
import { Navigate, Outlet } from "react-router";
import { client } from "~/api/client.gen";
import { postApiPaymentsMembership } from "~/api/sdk.gen";
import i18n from "~/i18n";

export default function AuthenticatedLayout() {
  const { keycloak, initialized } = useKeycloak();

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
      postApiPaymentsMembership({
        body: { memberId: keycloak.tokenParsed?.UserId ?? "" }
      }).then(res => {
        if (res.data?.checkoutUrl) {
          window.location.href = res.data.checkoutUrl;
        }
      });
    }

    const resInterceptor = client.instance.interceptors.response.use(
      async (response) => {
        return response;
      },
      (error) => {
        if (error.response && error.response.status === 401) {
          console.warn("Unauthorized, redirecting...");
          window.location.href = "/logout"; 
        }
        
        return Promise.reject(error);
      }
    );

    return () => {
      client.instance.interceptors.request.eject(reqInterceptor);
      client.instance.interceptors.response.eject(resInterceptor);
    };
  }, [initialized, keycloak.token]);

  if (!initialized) return null;

  if (!keycloak.authenticated) {
    return <Navigate to="/login" replace />;
  }
    
  return <Outlet />;
}