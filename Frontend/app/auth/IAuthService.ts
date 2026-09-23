import type { JSX } from "react";
import type { TokenParsed } from "~/types/TokenParsed";

/**
 * Defines the frontend auth service contract used by auth flows and route guards.
 */
export interface IAuthService {
  /**
   * Idempotent - safe to call from multiple places (a `clientLoader` and the
   * `AuthProvider` mount effect) without triggering duplicate init flows.
   */
  init: () => Promise<void>;
  login: (redirectUri?: string) => Promise<void>;
  logout: (redirectUri: string) => Promise<void>;
  isAuthenticated: () => boolean;
  isReady: () => boolean;
  getToken: () => Promise<string | null>;
  getTokenParsed: () => Promise<TokenParsed | null>;
  AuthProvider: ({ children }: { children: React.ReactNode }) => JSX.Element;
  getUpdateEmailUrl: () => Promise<string>;
  getUpdatePasswordUrl: () => Promise<string>;
  configure2FA: () => Promise<string>;
}
