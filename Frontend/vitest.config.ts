import tailwindcss from "@tailwindcss/vite";
import tsconfigPaths from "vite-tsconfig-paths";
import { defineConfig } from "vitest/config";

export default defineConfig({
  plugins: [tailwindcss(), tsconfigPaths()],
  test: {
    environment: "jsdom",
    globals: true,
    setupFiles: ["./vitest.setup.ts"],
    css: true,
    coverage: {
      provider: "v8",
      // Note: the "text" console table silently drops rows once there are enough files in a
      // directory group (observed with vitest 4.1.10 / @vitest/coverage-v8 4.1.10) - it's a
      // display bug only. Use "json-summary" (coverage/coverage-summary.json) or the "html"
      // report for ground truth per-file numbers instead of trusting the console table.
      reporter: ["text", "html", "json-summary"],
      include: ["app/**/*.{ts,tsx}"],
      // Mirrors the backend's approach (Program.cs, DatabaseSeeder, etc. are excluded there too):
      // composition roots, generated code, and declarative config are excluded because they carry
      // no branching logic of their own, not to dodge testing real behavior.
      exclude: [
        "app/api/**", // generated OpenAPI client (openapi-ts)
        "app/root.tsx", // SSR/CSR app shell composition root
        "app/routes.ts", // declarative route table, no logic
        "app/i18n/index.ts", // i18next bootstrap/config side effect only
        "**/*.d.ts",
        "**/*.gen.ts",
        "**/*.types.ts", // type-only modules (interfaces/types), no runtime code to cover
        "**/*.types.tsx",
        "app/auth/IAuthService.ts", // type-only interface contract
        "app/types/TokenParsed.ts", // type-only
        "app/types/MembersFilterDto.ts", // type-only
      ],
      thresholds: {
        statements: 95,
        // Branches includes a long tail of defensive/dead-code fallbacks (e.g. `?? new Error(...)`
        // after a guard that already guarantees truthiness) that aren't reachable through real
        // user interaction - 95% would mean testing unreachable code paths for no benefit.
        branches: 80,
        functions: 95,
        lines: 95,
      },
    },
  },
});
