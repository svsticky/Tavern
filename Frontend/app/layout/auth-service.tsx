import React from "react";
import { Outlet } from "react-router";
import { KeycloakAuthService } from "~/auth/KeycloakService";
import { getEnv } from "~/util/config.utils";

/**
 * Layout component responsible for providing authentication context to the app.
 * It initializes the appropriate authentication service based on environment configuration and wraps the app's routes with the corresponding provider.
 * @component
 */
export default function AuthServiceLayout() {
  const authServiceVar = (getEnv("AUTH_SYSTEM") ?? "keycloak")
    .trim()
    .toLowerCase();

  const authService = React.useMemo(() => {
    if (authServiceVar === "keycloak") {
      return new KeycloakAuthService();
    }
    return null;
  }, [authServiceVar]);

  if (!authService) {
    return <div>Unsupported authentication system: {authServiceVar}</div>;
  }

  const Provider = authService.AuthProvider;

  return (
    <Provider>
      <Outlet />
    </Provider>
  );
}
