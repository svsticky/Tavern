import { renderHook, waitFor } from "@testing-library/react";
import type { ReactNode } from "react";
import { describe, expect, it, vi } from "vitest";
import type { IAuthService } from "~/auth/IAuthService";
import AuthContext, { useAuth, useTokenParsed } from "~/context/AuthContext";
import type { TokenParsed } from "~/types/TokenParsed";

const mockService = {} as IAuthService;

describe("useAuth", () => {
  it("returns the auth service provided by the nearest AuthContext.Provider", () => {
    const wrapper = ({ children }: { children: ReactNode }) => (
      <AuthContext.Provider value={mockService}>
        {children}
      </AuthContext.Provider>
    );

    const { result } = renderHook(() => useAuth(), { wrapper });

    expect(result.current).toBe(mockService);
  });

  it("throws when used outside of an AuthContext.Provider", () => {
    expect(() => renderHook(() => useAuth())).toThrow(
      "useAuth must be used within an AuthProvider",
    );
  });
});

describe("useTokenParsed", () => {
  const sampleToken = {
    locale: "en",
    UserId: "00000000-0000-0000-0000-000000000000" as TokenParsed["UserId"],
    access_level: "member",
    given_name: "Test",
    family_name: "User",
    name: "Test User",
  };

  it("loads and returns the parsed token from the auth service", async () => {
    const service = {
      getTokenParsed: vi.fn(async () => sampleToken),
    } as unknown as IAuthService;
    const wrapper = ({ children }: { children: ReactNode }) => (
      <AuthContext.Provider value={service}>{children}</AuthContext.Provider>
    );

    const { result } = renderHook(() => useTokenParsed(), { wrapper });

    await waitFor(() => expect(result.current).toEqual(sampleToken));
  });

  it("ignores the resolved token when unmounted before resolution", async () => {
    let resolveToken: (token: TokenParsed) => void = () => {};
    const service = {
      getTokenParsed: vi.fn(
        () =>
          new Promise<TokenParsed>((resolve) => {
            resolveToken = resolve;
          }),
      ),
    } as unknown as IAuthService;
    const wrapper = ({ children }: { children: ReactNode }) => (
      <AuthContext.Provider value={service}>{children}</AuthContext.Provider>
    );

    const { unmount } = renderHook(() => useTokenParsed(), { wrapper });

    unmount();
    resolveToken(sampleToken);

    await waitFor(() => expect(service.getTokenParsed).toHaveBeenCalled());
  });
});
