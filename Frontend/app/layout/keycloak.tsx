import { ReactKeycloakProvider } from "@react-keycloak/web";
import Keycloak from "keycloak-js";
import { Outlet } from "react-router";

/**
 * Keycloak configuration instance.
 *
 * Initializes the connection to the Keycloak server using environment variables
 * with local development fallbacks. This instance is used to manage the
 * authentication state, token refreshing, and user profile access.
 */
const keycloak = new Keycloak({
  url: import.meta.env.KeycloakUrl ?? "https://localhost:8085/",
  realm: import.meta.env.KeycloakRealm ?? "master",
  clientId: import.meta.env.KeycloakClientId ?? "react",
});

/**
 * A root-level layout component that initializes the Keycloak authentication provider.
 *
 * This component wraps the application's route tree with `ReactKeycloakProvider`,
 * ensuring that all nested components have access to the Keycloak state (authenticated,
 * token, profile, etc.) via the `useKeycloak` hook.
 *
 * Initialization Configuration:
 * - **onLoad: 'check-sso'**: This allows the application to check if a user is
 *   already logged in with the Identity Provider (IdP) without forcing a
 *   redirect to the login page immediately. If the user is logged in,
 *   it authenticates them; otherwise, it leaves them as unauthenticated.
 *
 * @component
 */
export default function KeycloakLayout() {
  return (
    <ReactKeycloakProvider
      authClient={keycloak}
      initOptions={{
        onLoad: "check-sso",
      }}
    >
      <Outlet />
    </ReactKeycloakProvider>
  );
}
