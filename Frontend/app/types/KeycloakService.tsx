import Keycloak from 'keycloak-js';
import type { IAuthService } from './IAuthService';
import { getEnv } from '~/util/config.utils';
import React from 'react';
import { t } from 'i18next';
import AuthContext from '~/context/AuthContext';
import type { TokenParsed } from './TokenParsed';

export class KeycloakAuthService implements IAuthService {
    private keycloak: Keycloak;
    private isInitialized = false;

    constructor() {
        this.keycloak = new Keycloak({
            url: getEnv("KeycloakUrl") ?? "https://localhost:8085/",
            realm: getEnv("KeycloakRealm") ?? "master",
            clientId: getEnv("KeycloakClientId") ?? "react",
        });
    }

    public async init(): Promise<void> {
        await this.keycloak.init({
            onLoad: 'check-sso',
            silentCheckSsoRedirectUri: window.location.origin + '/silent-check-sso.html',
            pkceMethod: 'S256',
        });
    }

    public async login(): Promise<void> {
        await this.keycloak.login();
    }

    public async logout(redirectUri: string): Promise<void> {
        await this.keycloak.logout({
            redirectUri: redirectUri,
        });
    }

    public isAuthenticated(): boolean {
        return !!this.keycloak.token;
    }

    public async getToken(): Promise<string | null> {
        if (!this.keycloak.token) return null;

        try {
            await this.keycloak.updateToken(30);
            return this.keycloak.token;
        } catch (error) {
            console.error("Failed to refresh token", error);
            return null;
        }
    }

    public async getTokenParsed(): Promise<TokenParsed | null> {
        if (!this.keycloak.tokenParsed) return null;

        try {
            await this.keycloak.updateToken(30);
            return this.keycloak.tokenParsed as TokenParsed;
        } catch (error) {
            console.error("Failed to refresh token", error);
            return null;
        }
    }

    public AuthProvider = ({ children }: { children: React.ReactNode }): React.JSX.Element => {
        const [initialized, setInitialized] = React.useState(false);

        React.useEffect(() => {
            if (this.isInitialized) {
                setInitialized(true);
                return;
            }

            this.isInitialized = true;

            this.keycloak.init({
                onLoad: 'check-sso',
                silentCheckSsoRedirectUri: window.location.origin + '/silent-check-sso.html',
                pkceMethod: 'S256',
            })
            .then(() => setInitialized(true))
            .catch(err => {
                console.error("Keycloak init fail", err);
                this.isInitialized = false; 
            });
        }, []);

        if (!initialized) return <>{t("loading")}</>;

        return (
            <AuthContext.Provider value={this}>
                {children}
            </AuthContext.Provider>
        );
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