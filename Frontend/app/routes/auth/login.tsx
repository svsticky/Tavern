import { useKeycloak } from "@react-keycloak/web";
import { t } from "i18next";
import { useEffect } from "react";

/**
 * A dedicated authentication gateway component.
 *
 * This page acts as a router/sentinel for the login flow. It does not contain
 * a traditional UI; instead, it orchestrates the following logic:
 * - **Unauthenticated Users**: Automatically triggers the Keycloak redirect
 *   to the SSO login page. Upon successful login, Keycloak is instructed to
 *   redirect the user back to the application root.
 * - **Authenticated Users**: If a user navigates to this page while already
 *   logged in, they are immediately bounced back to the homepage to prevent
 *   unnecessary login prompts.
 *
 * Performance Note: This component uses a full page redirect (`window.location.href`)
 * to ensure the application state is completely reset and synchronized with
 * the new authentication token.
 *
 * @page
 * @component
 */
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
      <p className="text-xl font-semibold text-gray-700">
        {t("redirecting_to_login")}
      </p>
    </div>
  );
}
