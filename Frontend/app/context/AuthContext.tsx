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

export const useTokenParsed = (): TokenParsed | null => {
  const authService = useAuth();
  const [tokenParsed, setTokenParsed] = useState<TokenParsed | null>(null);

  useEffect(() => {
    let cancelled = false;
    authService.getTokenParsed().then((token) => {
      if (!cancelled) {
        setTokenParsed(token);
      }
    });
    return () => {
      cancelled = true;
    };
  }, [authService]);

  return tokenParsed;
};

export default AuthContext;
