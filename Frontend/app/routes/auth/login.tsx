import { useKeycloak } from "@react-keycloak/web";
import { t } from "i18next";
import { useEffect } from "react";

export default function Login() {
  const { keycloak } = useKeycloak();
  
  useEffect(() => {
    if (!keycloak.authenticated) {
      keycloak.login({
        redirectUri: `${window.location.origin}/`,
      });
    } else {
      window.location.href = "/";
    }
  }, [keycloak]);

  return (
    <div className="flex items-center justify-center min-h-screen">
      <p className="text-xl font-semibold text-gray-700">{t("redirecting_to_login")}</p>
    </div>
  );
}
