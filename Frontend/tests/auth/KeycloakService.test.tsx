import { render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";

const { keycloakInstances, MockKeycloak } = vi.hoisted(() => {
  const keycloakInstances: any[] = [];
  class MockKeycloak {
    authenticated = false;
    token: string | undefined;
    tokenParsed: any;
    init = vi.fn();
    login = vi.fn(async () => {});
    logout = vi.fn(async () => {});
    updateToken = vi.fn(async () => true);
    createLoginUrl = vi.fn(
      (opts: { action: string; redirectUri: string }) =>
        `https://kc.example.com/login?action=${opts.action}`,
    );
    constructor(public options: unknown) {
      keycloakInstances.push(this);
    }
  }
  return { keycloakInstances, MockKeycloak };
});

vi.mock("keycloak-js", () => ({ default: MockKeycloak }));

const { KeycloakAuthService } = await import("~/auth/KeycloakService");

describe("KeycloakAuthService", () => {
  beforeEach(() => {
    keycloakInstances.length = 0;
  });

  function latestKeycloak() {
    return keycloakInstances[keycloakInstances.length - 1];
  }

  it("constructs the underlying Keycloak client with env-configured (or default) options", () => {
    new KeycloakAuthService();
    expect(latestKeycloak().options).toMatchObject({
      url: expect.any(String),
      realm: expect.any(String),
      clientId: expect.any(String),
    });
  });

  describe("init", () => {
    it("marks the service ready after a successful init", async () => {
      const service = new KeycloakAuthService();
      latestKeycloak().init.mockResolvedValue(true);

      await service.init();

      expect(service.isReady()).toBe(true);
    });

    it("leaves the service not-ready and rethrows when init fails", async () => {
      const service = new KeycloakAuthService();
      latestKeycloak().init.mockRejectedValue(new Error("init failed"));

      await expect(service.init()).rejects.toThrow("init failed");
      expect(service.isReady()).toBe(false);
    });

    it("does not call the underlying init again once already ready", async () => {
      const service = new KeycloakAuthService();
      latestKeycloak().init.mockResolvedValue(true);

      await service.init();
      await service.init();

      expect(latestKeycloak().init).toHaveBeenCalledTimes(1);
    });

    it("shares the in-flight init promise for concurrent callers", async () => {
      const service = new KeycloakAuthService();
      let resolveInit: () => void = () => {};
      latestKeycloak().init.mockReturnValue(
        new Promise<boolean>((resolve) => {
          resolveInit = () => resolve(true);
        }),
      );

      const first = service.init();
      const second = service.init();
      resolveInit();
      await Promise.all([first, second]);

      expect(latestKeycloak().init).toHaveBeenCalledTimes(1);
    });
  });

  it("login() delegates to keycloak.login with the redirect URI", async () => {
    const service = new KeycloakAuthService();
    await service.login("https://app.example.com/after-login");
    expect(latestKeycloak().login).toHaveBeenCalledWith({
      redirectUri: "https://app.example.com/after-login",
    });
  });

  it("logout() delegates to keycloak.logout with the redirect URI", async () => {
    const service = new KeycloakAuthService();
    await service.logout("https://app.example.com/login");
    expect(latestKeycloak().logout).toHaveBeenCalledWith({
      redirectUri: "https://app.example.com/login",
    });
  });

  describe("isAuthenticated", () => {
    it("reflects keycloak.authenticated when set", () => {
      const service = new KeycloakAuthService();
      latestKeycloak().authenticated = true;
      expect(service.isAuthenticated()).toBe(true);
    });

    it("falls back to whether a token is present when authenticated is undefined", () => {
      const service = new KeycloakAuthService();
      latestKeycloak().authenticated = undefined;
      latestKeycloak().token = "abc";
      expect(service.isAuthenticated()).toBe(true);
    });
  });

  describe("getToken", () => {
    it("returns null when there is no token", async () => {
      const service = new KeycloakAuthService();
      latestKeycloak().token = undefined;
      expect(await service.getToken()).toBeNull();
    });

    it("refreshes and returns the token when present", async () => {
      const service = new KeycloakAuthService();
      latestKeycloak().token = "the-token";
      expect(await service.getToken()).toBe("the-token");
      expect(latestKeycloak().updateToken).toHaveBeenCalledWith(30);
    });

    it("still returns the (stale) token if the refresh call throws", async () => {
      const service = new KeycloakAuthService();
      latestKeycloak().token = "the-token";
      latestKeycloak().updateToken.mockRejectedValue(
        new Error("refresh failed"),
      );

      await expect(service.getToken()).resolves.toBe("the-token");
    });
  });

  describe("getTokenParsed", () => {
    it("returns null when there is no parsed token", async () => {
      const service = new KeycloakAuthService();
      latestKeycloak().tokenParsed = undefined;
      expect(await service.getTokenParsed()).toBeNull();
    });

    it("refreshes and returns the parsed token when present", async () => {
      const service = new KeycloakAuthService();
      latestKeycloak().tokenParsed = { name: "Test User" };
      expect(await service.getTokenParsed()).toEqual({ name: "Test User" });
    });
  });

  describe("AuthProvider", () => {
    it("shows a loading state until keycloak init resolves, then renders children", async () => {
      const service = new KeycloakAuthService();
      latestKeycloak().init.mockResolvedValue(true);
      const { AuthProvider } = service;

      render(
        <AuthProvider>
          <div>Protected content</div>
        </AuthProvider>,
      );

      expect(screen.getByText("loading")).toBeInTheDocument();
      await waitFor(() =>
        expect(screen.getByText("Protected content")).toBeInTheDocument(),
      );
    });

    it("keeps showing the loading state if init fails", async () => {
      const service = new KeycloakAuthService();
      latestKeycloak().init.mockRejectedValue(new Error("nope"));
      const { AuthProvider } = service;
      const consoleError = vi
        .spyOn(console, "error")
        .mockImplementation(() => {});

      render(
        <AuthProvider>
          <div>Protected content</div>
        </AuthProvider>,
      );

      await waitFor(() => expect(latestKeycloak().init).toHaveBeenCalled());
      expect(screen.getByText("loading")).toBeInTheDocument();
      expect(screen.queryByText("Protected content")).not.toBeInTheDocument();
      consoleError.mockRestore();
    });
  });

  describe("action URL helpers", () => {
    it("throw when there is no authenticated user", async () => {
      const service = new KeycloakAuthService();
      latestKeycloak().tokenParsed = undefined;

      await expect(service.getUpdateEmailUrl()).rejects.toThrow(
        "User not authenticated",
      );
      await expect(service.getUpdatePasswordUrl()).rejects.toThrow(
        "User not authenticated",
      );
      await expect(service.configureMFA()).rejects.toThrow(
        "User not authenticated",
      );
    });

    it("build the expected createLoginUrl action for each helper", async () => {
      const service = new KeycloakAuthService();
      latestKeycloak().tokenParsed = { name: "Test User" };

      expect(await service.getUpdateEmailUrl()).toContain("UPDATE_EMAIL");
      expect(await service.getUpdatePasswordUrl()).toContain("UPDATE_PASSWORD");
      expect(await service.configureMFA()).toContain("CONFIGURE_TOTP");
    });
  });

  describe("resetCredentials", () => {
    it("builds a reset-credentials URL containing the client id and redirect uri", async () => {
      const service = new KeycloakAuthService();
      const url = await service.resetCredentials();
      expect(url).toContain("reset-credentials");
      expect(url).toContain("client_id=");
      expect(url).toContain("redirect_uri=");
    });
  });
});
