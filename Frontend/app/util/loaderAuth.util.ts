import { waitForAuthReady } from "~/layout/auth-service";
import type { TokenParsed } from "~/types/TokenParsed";

/** Loaders run before `AuthenticatedLayout` redirects unauthenticated visitors, so this waits for auth itself. */
export async function requireTokenParsed(): Promise<TokenParsed> {
  const authService = await waitForAuthReady();

  if (!authService?.isAuthenticated()) {
    await authService?.login(window.location.href);
    // login() navigates away; never resolving stops the loader from rendering the route without a user.
    return new Promise<never>(() => {});
  }

  const tokenParsed = await authService.getTokenParsed();
  if (!tokenParsed) {
    await authService.login(window.location.href);
    return new Promise<never>(() => {});
  }

  return tokenParsed;
}
