import type { JSX } from "react";
import type { TokenParsed } from "~/types/TokenParsed";

/**
 * Defines the frontend auth service contract used by auth flows and route guards.
 */
export interface IAuthService {
  login: (redirectUri?: string) => Promise<void>;
  logout: (redirectUri: string) => Promise<void>;
  isAuthenticated: () => boolean;
  isReady: () => boolean;
  getToken: () => Promise<string | null>;
  getTokenParsed: () => Promise<TokenParsed | null>;
  AuthProvider: ({ children }: { children: React.ReactNode }) => JSX.Element;
  getUpdateEmailUrl: () => Promise<string>;
  getUpdatePasswordUrl: () => Promise<string>;
  resetCredentials: () => Promise<string>;
  configure2FA: () => Promise<string>;
}
