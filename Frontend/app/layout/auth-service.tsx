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

/** Built on first call, since route loaders run before any layout renders. */
export const getActiveAuthService = (): IAuthService | null => {
  if (!activeAuthService && typeof window !== "undefined") {
    activeAuthService = createAuthService();
  }
  return activeAuthService;
};

/** Test-only: lets each test observe a fresh construction. */
export const __resetAuthServiceForTests = () => {
  activeAuthService = null;
};

/** Resolves once auth init is done; `init()` is idempotent, so this doesn't race the provider's own init. */
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
