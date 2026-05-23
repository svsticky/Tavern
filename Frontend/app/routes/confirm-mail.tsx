import { t } from "i18next";
import { useEffect } from "react";
import NavBar from "~/components/Menu/NavBar/NavBar";
import { useAuth } from "~/context/AuthContext";

/**
 * A bridge component used during the email confirmation or password reset flow.
 *
 * This page serves as a temporary landing spot after a user clicks a link in a
 * system-generated email. Its primary purpose is to:
 * - **Transition to Identity Provider**: After a brief delay (1000ms), it constructs
 *   the necessary URL to redirect the user back into the auth credential reset flow.
 * - **Preserve Context**: It dynamically builds the `redirect_uri` using the current
 *   window origin to ensure the user returns to the application after finishing the
 *   flow on the auth server.
 * - **UI Continuity**: Displays a minimal navigation bar and a loading state to inform
 *   the user that a redirection is in progress.
 *
 * @page
 * @component
 */
export default function ConfirmMail() {
  const authService = useAuth();

  useEffect(() => {
    if (!authService) return;
    const redirectAction = async () => {
      // Give auth service some time to process the email token and set up the session before redirecting
      await new Promise((resolve) => setTimeout(resolve, 1000));

      window.location.href = await authService.resetCredentials();
    };
    redirectAction();
  }, [authService]);

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
