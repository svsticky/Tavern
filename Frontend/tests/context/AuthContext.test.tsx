import { renderHook, waitFor } from "@testing-library/react";
import type { ReactNode } from "react";
import { describe, expect, it, vi } from "vitest";
import type { IAuthService } from "~/auth/IAuthService";
import AuthContext, {
  TokenParsedContext,
  useAuth,
  useTokenParsed,
} from "~/context/AuthContext";
import { createMockAuthService } from "~/testUtils";
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
  const token = {
    UserId: "user-1",
    locale: "en",
  } as unknown as TokenParsed;

  const wrapperFor =
    (service: IAuthService, inherited: TokenParsed | null) =>
    ({ children }: { children: ReactNode }) => (
      <AuthContext.Provider value={service}>
        <TokenParsedContext.Provider value={inherited}>
          {children}
        </TokenParsedContext.Provider>
      </AuthContext.Provider>
    );

  it("returns the token from context on the very first render, without fetching it", () => {
    const service = createMockAuthService();

    const { result } = renderHook(() => useTokenParsed(), {
      wrapper: wrapperFor(service, token),
    });

    expect(result.current).toBe(token);
    expect(service.getTokenParsed).not.toHaveBeenCalled();
  });

  it("falls back to fetching the token outside the app tree, and is null until it resolves", async () => {
    const service = createMockAuthService({
      getTokenParsed: vi.fn(async () => token),
    });

    const { result } = renderHook(() => useTokenParsed(), {
      wrapper: wrapperFor(service, null),
    });

    expect(result.current).toBeNull();
    await waitFor(() => expect(result.current).toBe(token));
    expect(service.getTokenParsed).toHaveBeenCalledTimes(1);
  });

  it("stays null when there is no token to fetch", async () => {
    const service = createMockAuthService({
      getTokenParsed: vi.fn(async () => null),
    });

    const { result } = renderHook(() => useTokenParsed(), {
      wrapper: wrapperFor(service, null),
    });

    await waitFor(() => expect(service.getTokenParsed).toHaveBeenCalled());
    expect(result.current).toBeNull();
  });

  it("ignores a token that resolves after the component unmounted", async () => {
    let resolveToken: (value: TokenParsed) => void = () => {};
    const service = createMockAuthService({
      getTokenParsed: vi.fn(
        () =>
          new Promise<TokenParsed | null>((resolve) => {
            resolveToken = resolve;
          }),
      ),
    });

    const { result, unmount } = renderHook(() => useTokenParsed(), {
      wrapper: wrapperFor(service, null),
    });
    unmount();
    resolveToken(token);
    await Promise.resolve();

    expect(result.current).toBeNull();
  });
});
