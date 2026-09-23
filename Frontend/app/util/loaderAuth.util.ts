import { waitForAuthReady } from "~/layout/auth-service";
import type { TokenParsed } from "~/types/TokenParsed";

/**
 * Waits for the auth service to be ready and returns the current user's
 * parsed token, for use inside a `clientLoader`.
 *
 * `clientLoader`s for the whole matched route tree run before
 * `AuthenticatedLayout` mounts and redirects an unauthenticated visitor to
 * Keycloak, so a loader that needs the user's identity (e.g. to filter
 * "my enrollments") must do its own wait/redirect rather than assuming the
 * layout has already handled it.
 */
export async function requireTokenParsed(): Promise<TokenParsed> {
  const authService = await waitForAuthReady();

  if (!authService?.isAuthenticated()) {
    await authService?.login(window.location.href);
    // authService.login() navigates away; this promise intentionally never
    // resolves so the loader doesn't continue rendering the route with no user.
    return new Promise<never>(() => {});
  }

  const tokenParsed = await authService.getTokenParsed();
  if (!tokenParsed) {
    await authService.login(window.location.href);
    return new Promise<never>(() => {});
  }

  return tokenParsed;
}
