import { renderHook } from "@testing-library/react";
import type { ReactNode } from "react";
import { describe, expect, it } from "vitest";
import type { IAuthService } from "~/auth/IAuthService";
import AuthContext, { useAuth } from "~/context/AuthContext";

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
