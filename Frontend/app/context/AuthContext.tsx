import { createContext, useContext, useEffect, useState } from "react";
import type { IAuthService } from "~/auth/IAuthService";
import type { TokenParsed } from "~/types/TokenParsed";

const AuthContext = createContext<IAuthService | null>(null);

/**
 * Returns the active auth service.
 *
 * @throws {Error} When used outside an `AuthContext` provider.
 */
export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return context;
};

export default AuthContext;

/**
 * Holds the parsed token once the authenticated layout has loaded it, so pages
 * below it don't have to fetch it again.
 */
export const TokenParsedContext = createContext<TokenParsed | null>(null);

/**
 * Returns the parsed token of the logged-in user. Uses the token from
 * `TokenParsedContext` when available, and otherwise fetches it from the auth service.
 *
 * @returns {TokenParsed | null} The parsed token, or `null` while it's loading.
 */
export function useTokenParsed(): TokenParsed | null {
  const authService = useAuth();
  const inherited = useContext(TokenParsedContext);
  const [fetched, setFetched] = useState<TokenParsed | null>(null);

  useEffect(() => {
    if (inherited) return;

    let cancelled = false;
    authService.getTokenParsed().then((tokenParsed) => {
      if (!cancelled) setFetched(tokenParsed);
    });

    return () => {
      cancelled = true;
    };
  }, [authService, inherited]);

  return fetched;
}
