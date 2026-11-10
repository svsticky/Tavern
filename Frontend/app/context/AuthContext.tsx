import { createContext, useContext } from "react";
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

export default AuthContext;
