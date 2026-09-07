import { reactRouter } from "@react-router/dev/vite";
import tailwindcss from "@tailwindcss/vite";
import { defineConfig } from "vite";
import tsconfigPaths from "vite-tsconfig-paths";

export default defineConfig({
  server: {
    host: true,
    port: 5173,
    strictPort: true,
    watch: {
      usePolling: true,
      interval: 1000,
      ignored: [
        "**/node_modules/**",
        "**/.git/**",
        "**/build/**",
        "**/.react-router/**",
      ],
    },
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
