import { useKeycloak } from "@react-keycloak/web";
import { t } from "i18next";
import { useEffect } from "react";
import { useNavigate } from "react-router";

export default function LogoutPage() {
  const { keycloak, initialized } = useKeycloak();
  const navigate = useNavigate();

  useEffect(() => {
    if (initialized && keycloak.authenticated) {
      keycloak.logout({
        redirectUri: `${window.location.origin}/login`,
      });
    } else if (initialized && !keycloak.authenticated) {
      navigate("/login", { replace: true });
    }
  }, [initialized, keycloak, navigate]);

  return (
    <div className="flex items-center justify-center min-h-screen">
      <p className="text-xl font-semibold text-gray-700">{t("logging_out")}</p>
    </div>
  );
}
