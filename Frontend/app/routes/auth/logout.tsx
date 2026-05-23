import { t } from "i18next";
import { useEffect } from "react";
import { useNavigate } from "react-router";
import { useAuth } from "~/context/AuthContext";

/**
 * A dedicated session termination component.
 *
 * This page handles the secure teardown of the user's session. It orchestrates
 * two primary scenarios:
 * - **Authenticated Users**: Triggers the auth logout flow. This invalidates
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
  const authService = useAuth();
  const navigate = useNavigate();

  useEffect(() => {
    if (!authService.isReady()) return;

    if (authService.isAuthenticated()) {
      authService.logout(`${window.location.origin}/login`);
    } else {
      navigate("/login", { replace: true });
    }
  }, [authService, navigate]);

  return (
    <div className="flex items-center justify-center min-h-screen">
      <p className="text-xl font-semibold text-gray-700">{t("logging_out")}</p>
    </div>
  );
}
