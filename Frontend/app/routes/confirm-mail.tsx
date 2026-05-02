import { t } from "i18next";
import { useEffect } from "react";
import NavBar from "~/components/Menu/NavBar/NavBar";
import { getEnv } from "~/util/config.utils";

/**
 * A bridge component used during the email confirmation or password reset flow.
 *
 * This page serves as a temporary landing spot after a user clicks a link in a
 * system-generated email. Its primary purpose is to:
 * - **Transition to Identity Provider**: After a brief delay (1000ms), it constructs
 *   the necessary URL to redirect the user back into the Keycloak credential reset flow.
 * - **Preserve Context**: It dynamically builds the `redirect_uri` using the current
 *   window origin to ensure the user returns to the application after finishing the
 *   flow on the auth server.
 * - **UI Continuity**: Displays a minimal navigation bar and a loading state to inform
 *   the user that a redirection is in progress.
 *
 * Note: The `tab_id` and specific query parameters are critical for Keycloak to
 * associate the browser session with the specific email action token.
 *
 * @page
 * @component
 */
export default function ConfirmMail() {
  useEffect(() => {
    const redirectAction = async () => {
      await new Promise((resolve) => setTimeout(resolve, 1000));

      const baseUrl = `${getEnv("KeycloakUrl")}/realms/${getEnv("KeycloakRealm")}/login-actions/reset-credentials`;

      const clientId = `${getEnv("KeycloakClientId")}`;
      const redirectUri = encodeURIComponent(`${window.location.origin}/`);

      window.location.href = `${baseUrl}?client_id=${clientId}&tab_id=...&redirect_uri=${redirectUri}`;
    };
    redirectAction();
  }, []);

  return (
    <>
      <section id="home">
        <NavBar className="px-[5%] sm:px-[10%]" maxWidthBeforeCompact={900}>
          <NavBar.Branding title="" homepage="/register" />
        </NavBar>
      </section>

      <div className="p-4">
        <p className="text-lg">{t("loading")}...</p>
      </div>
    </>
  );
}
