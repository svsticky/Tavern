import React from "react";
import { Outlet } from "react-router";
import type { IAuthService } from "~/auth/IAuthService";
import { KeycloakAuthService } from "~/auth/KeycloakService";
import { getEnv } from "~/util/config.utils";

let activeAuthService: IAuthService | null = null;

const createAuthService = (): IAuthService | null => {
  const authServiceVar = (getEnv("AUTH_SYSTEM") ?? "keycloak")
    .trim()
    .toLowerCase();

  if (authServiceVar === "keycloak") {
    return new KeycloakAuthService();
  }
  return null;
};

/**
 * Returns the active authentication service instance, constructing it on
 * first call. This must not depend on `AuthServiceLayout` having rendered:
 * `clientLoader`s for the whole matched route tree run before any layout
 * component does, so a route's `clientLoader` can be the very first caller.
 */
export const getActiveAuthService = (): IAuthService | null => {
  if (!activeAuthService && typeof window !== "undefined") {
    activeAuthService = createAuthService();
  }
  return activeAuthService;
};

/**
 * Resolves once the active auth service has finished its init flow (Keycloak
 * SSO check, etc). Safe to call from a `clientLoader` - `IAuthService.init`
 * is idempotent, so this piggybacks on the same init the `AuthProvider`
 * mount effect triggers instead of racing it.
 */
export const waitForAuthReady = async (): Promise<IAuthService | null> => {
  const authService = getActiveAuthService();
  if (!authService) return null;
  await authService.init();
  return authService;
};

/**
 * Layout component responsible for providing authentication context to the app.
 * It initializes the appropriate authentication service based on environment configuration and wraps the app's routes with the corresponding provider.
 * @component
 */
export default function AuthServiceLayout() {
  const authService = React.useMemo(getActiveAuthService, []);

  if (!authService) {
    const authServiceVar = (getEnv("AUTH_SYSTEM") ?? "keycloak")
      .trim()
      .toLowerCase();
    return <div>Unsupported authentication system: {authServiceVar}</div>;
  }

  const Provider = authService.AuthProvider;

  return (
    <Provider>
      <Outlet />
    </Provider>
  );
}
