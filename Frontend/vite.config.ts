import { reactRouter } from "@react-router/dev/vite";
import tailwindcss from "@tailwindcss/vite";
import { defineConfig } from "vite";
import tsconfigPaths from "vite-tsconfig-paths";

export default defineConfig({
  server: {
    host: true,
    port: 5173,
    strictPort: true,
  },
  ssr: {
    noExternal: ["@react-keycloak/web", "keycloak-js"],
  },
  optimizeDeps: {
    include: ["@react-keycloak/web", "keycloak-js"],
  },
  envPrefix: ['VITE_', 'AUTH_SYSTEM', 'KeycloakRealm', 'KeycloakClientId', 'HostUrl', "ApiUrl", "LOGO_URL"],
  plugins: [tailwindcss(), reactRouter(), tsconfigPaths()],
});
