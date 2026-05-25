import { t } from "i18next";
import Keycloak from "keycloak-js";
import React from "react";
import AuthContext from "~/context/AuthContext";
import type { TokenParsed } from "~/types/TokenParsed";
import { getEnv } from "~/util/config.utils";
import type { IAuthService } from "./IAuthService";

/**
 * Keycloak-backed implementation of the frontend auth service contract.
 */
export class KeycloakAuthService implements IAuthService {
  private keycloak: Keycloak;
  private ready = false;
  private initPromise: Promise<void> | null = null;

  constructor() {
    this.keycloak = new Keycloak({
      url: getEnv("KeycloakUrl") ?? "https://localhost:8085/",
      realm: getEnv("KeycloakRealm") ?? "master",
      clientId: getEnv("KeycloakClientId") ?? "react",
    });
  }

  public async init(): Promise<void> {
    if (this.ready) return;
    if (this.initPromise) return this.initPromise;

    this.initPromise = this.keycloak
      .init({
        onLoad: "check-sso",
        silentCheckSsoRedirectUri: `${window.location.origin}/silent-check-sso.html`,
        pkceMethod: "S256",
      })
      .then(() => {
        this.ready = true;
      })
      .catch((err) => {
        this.ready = false;
        throw err;
      })
      .finally(() => {
        this.initPromise = null;
      });

    return this.initPromise;
  }

  public async login(redirectUri?: string): Promise<void> {
    await this.keycloak.login({
      redirectUri: redirectUri
    });
  }

  public async logout(redirectUri: string): Promise<void> {
    await this.keycloak.logout({
      redirectUri: redirectUri,
    });
  }

  public isAuthenticated(): boolean {
    return this.keycloak.authenticated ?? !!this.keycloak.token;
  }

  public isReady(): boolean {
    return this.ready;
  }

  public async getToken(): Promise<string | null> {
    if (!this.keycloak.token) return null;

    try {
      await this.keycloak.updateToken(30);
    } catch (error) {
      console.error("Failed to refresh token", error);
    }

    return this.keycloak.token;
  }

  public async getTokenParsed(): Promise<TokenParsed | null> {
    if (!this.keycloak.tokenParsed) return null;

    try {
      await this.keycloak.updateToken(30);
    } catch (error) {
      console.error("Failed to refresh token", error);
    }

    return this.keycloak.tokenParsed as TokenParsed;
  }

  public AuthProvider = ({
    children,
  }: {
    children: React.ReactNode;
  }): React.JSX.Element => {
    const [initialized, setInitialized] = React.useState(false);

    React.useEffect(() => {
      let cancelled = false;

      this.init()
        .then(() => {
          if (!cancelled) setInitialized(true);
        })
        .catch((err) => {
          console.error("Keycloak init fail", err);
          if (!cancelled) setInitialized(false);
        });

      return () => {
        cancelled = true;
      };
    }, []);

    if (!initialized) return <>{t("loading")}</>;

    return <AuthContext.Provider value={this}>{children}</AuthContext.Provider>;
  };

  public async getUpdateEmailUrl(): Promise<string> {
    if (!this.keycloak.tokenParsed) throw new Error("User not authenticated");

    return this.keycloak.createLoginUrl({
      action: "UPDATE_EMAIL",
      redirectUri: window.location.href,
    });
  }

  public async getUpdatePasswordUrl(): Promise<string> {
    if (!this.keycloak.tokenParsed) throw new Error("User not authenticated");

    return this.keycloak.createLoginUrl({
      action: "UPDATE_PASSWORD",
      redirectUri: window.location.href,
    });
  }

  public async resetCredentials(): Promise<string> {
    const baseUrl = `${getEnv("KeycloakUrl")}/realms/${getEnv("KeycloakRealm")}/login-actions/reset-credentials`;

    const clientId = `${getEnv("KeycloakClientId")}`;
    const redirectUri = encodeURIComponent(`${window.location.origin}/`);

    return `${baseUrl}?client_id=${clientId}&tab_id=...&redirect_uri=${redirectUri}`;
  }
}
