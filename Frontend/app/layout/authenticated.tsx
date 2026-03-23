import { useKeycloak } from "@react-keycloak/web";
import { useEffect } from "react";
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
  useEffect(() => {
    if (initialized && keycloak.token) {
      const interceptor = client.instance.interceptors.request.use((config) => {
        config.headers.Authorization = `Bearer ${keycloak.token}`;
        return config;
      });

      return () => {
        client.instance.interceptors.request.eject(interceptor);
      };
    }
  }, [initialized, keycloak.token]);
    return <Outlet />;
}
