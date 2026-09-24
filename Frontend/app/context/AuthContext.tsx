import { createContext, useContext, useEffect, useState } from "react";
import type { IAuthService } from "~/auth/IAuthService";
import type { TokenParsed } from "~/types/TokenParsed";

const AuthContext = createContext<IAuthService | null>(null);

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error("useAuth must be used within an AuthProvider");
  }
  return context;
};

/**
 * The current user's parsed token, provided by `AuthenticatedLayout` (which
 * only renders its children once it has one). Lets layouts below it read the
 * user synchronously instead of re-fetching the token on every mount - a
 * layout that renders nothing while it does so leaves the page empty at the
 * moment React Router restores scroll on back-navigation, so restoration
 * clamps to the top.
 *
 * `null` outside `AuthenticatedLayout` (e.g. tests rendering a layout alone).
 */
export const TokenParsedContext = createContext<TokenParsed | null>(null);

/**
 * The current user's parsed token. Inside the app tree it comes straight from
 * `TokenParsedContext`, so it's there on the very first render - a component
 * that had to fetch it asynchronously would pop its token-dependent parts in
 * late. Rendered outside that context (e.g. on its own in a test) it falls
 * back to fetching the token, and is `null` until that resolves.
 */
export function useTokenParsed(): TokenParsed | null {
  const authService = useAuth();
  const inherited = useContext(TokenParsedContext);
  const [fetched, setFetched] = useState<TokenParsed | null>(null);

  useEffect(() => {
    if (inherited) return;
    let cancelled = false;
    authService.getTokenParsed().then((token) => {
      if (!cancelled) setFetched(token);
    });
    return () => {
      cancelled = true;
    };
  }, [authService, inherited]);

  return inherited ?? fetched;
}

export default AuthContext;
