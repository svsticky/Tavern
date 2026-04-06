import { ReactKeycloakProvider } from "@react-keycloak/web";
import { Outlet } from "react-router";
import Keycloak from "keycloak-js";

const keycloak = new Keycloak({
  url: import.meta.env.KeycloakUrl ?? "https://localhost:8085/",
  realm: import.meta.env.KeycloakRealm ?? "master",
  clientId: import.meta.env.KeycloakClientId ?? "react",
});


export default function KeycloakLayout() {
  return (
    <ReactKeycloakProvider 
      authClient={keycloak}
      initOptions={{ 
        onLoad: 'check-sso',
      }}
    >
      <Outlet />
    </ReactKeycloakProvider>
  );
}