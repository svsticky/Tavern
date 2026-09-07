import React from "react";
import { Outlet } from "react-router";
import type { IAuthService } from "~/auth/IAuthService";
import { KeycloakAuthService } from "~/auth/KeycloakService";
import { AdminModeProvider } from "~/context/AdminModeContext";
import { getEnv } from "~/util/config.utils";

let activeAuthService: IAuthService | null = null;

/**
 * Returns the currently active authentication service instance.
 */
export const getActiveAuthService = (): IAuthService | null =>
  activeAuthService;

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
    let service: IAuthService | null = null;
    if (authServiceVar === "keycloak") {
      service = new KeycloakAuthService();
    }
    activeAuthService = service;
    return service;
  }, [authServiceVar]);

  if (!authService) {
    return <div>Unsupported authentication system: {authServiceVar}</div>;
  }

  const Provider = authService.AuthProvider;

  return (
    <Provider>
      <AdminModeProvider>
        <Outlet />
      </AdminModeProvider>
    </Provider>
  );
}
