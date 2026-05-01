import { useKeycloak } from "@react-keycloak/web";
import { t } from "i18next";
import { useEffect } from "react";
import { useNavigate } from "react-router";

/**
 * A dedicated session termination component.
 * 
 * This page handles the secure teardown of the user's session. It orchestrates 
 * two primary scenarios:
 * - **Authenticated Users**: Triggers the Keycloak OIDC logout flow. This invalidates 
 *   the session on the SSO server and redirects the browser back to the login page 
 *   to ensure no stale tokens remain in memory.
 * - **Unauthenticated/Stale Sessions**: If the user is already logged out or the 
 *   session has expired, it performs a client-side redirect to the login page 
 *   using React Router to maintain a smooth UX.
 * 
 * The component remains visible only during the bridge between the application 
 * and the authentication server's logout endpoint.
 * 
 * @page
 * @component
 */
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
