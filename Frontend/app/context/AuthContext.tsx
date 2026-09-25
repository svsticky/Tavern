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

/** Provided by `AuthenticatedLayout` so layouts can read the token synchronously; `null` outside it. */
export const TokenParsedContext = createContext<TokenParsed | null>(null);

/** Reads the token from context (there on first render), falling back to fetching it outside the app tree. */
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
