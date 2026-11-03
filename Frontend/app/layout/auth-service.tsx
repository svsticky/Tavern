import React from "react";
import { Outlet } from "react-router";
import type { IAuthService } from "~/auth/IAuthService";
import { KeycloakAuthService } from "~/auth/KeycloakService";
import { getEnv } from "~/util/config.utils";

let activeAuthService: IAuthService | null = null;

/**
 * Creates the auth service picked by the `AUTH_SYSTEM` setting (Keycloak by default).
 *
 * @returns {IAuthService | null} The auth service, or `null` when the setting names an unsupported system.
 */
const createAuthService = (): IAuthService | null => {
  const authServiceVar = (getEnv("AUTH_SYSTEM") ?? "keycloak")
    .trim()
    .toLowerCase();

    if (authServiceVar === "keycloak") {
      return new KeycloakAuthService();
    }
    return null;
}

/**
 * Returns the currently active authentication service instance.
 */
export const getActiveAuthService = (): IAuthService | null => {
  if (!activeAuthService && typeof window !== "undefined") {
    activeAuthService = createAuthService();
  }
  return activeAuthService;
}

/**
 * Returns the active auth service once it has finished initializing, so route
 * loaders can check the login before the auth provider has rendered.
 *
 * @returns {Promise<IAuthService | null>} The ready auth service, or `null` when there is none.
 */
export const waitForAuthReady = async (): Promise<IAuthService | null> => {
  const authService = getActiveAuthService();
  if (!authService) return null;
  await authService.init();
  return authService;
}

/**
 * Layout component responsible for providing authentication context to the app.
 * It initializes the appropriate authentication service based on environment configuration and wraps the app's routes with the corresponding provider.
 * @component
 */
export default function AuthServiceLayout() {
  const authService = React.useMemo(getActiveAuthService, [])

  if (!authService) {
    return <div>Unsupported authentication system: {getEnv("AUTH_SYSTEM")}</div>;
  }

  const Provider = authService.AuthProvider;

  return (
    <Provider>
      <Outlet />
    </Provider>
  );
}
