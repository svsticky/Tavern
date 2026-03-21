import { useKeycloak } from "@react-keycloak/web";
import { Navigate, Outlet } from "react-router";
import { client } from "~/api/client.gen";

export default function AuthenticatedLayout() {
  const { keycloak, initialized } = useKeycloak();

  if (!initialized) {
    return null;
  }

  if (!keycloak.authenticated) {
    console.log("User is not authenticated, redirecting to login page...");
    return <Navigate to="/login" replace />;
  }
  client.setConfig({
    baseUrl: "https://localhost:8080",
    headers: {
      Authorization: keycloak.token ? `Bearer ${keycloak.token}` : undefined,
    },
  });

  return <Outlet />;
}
