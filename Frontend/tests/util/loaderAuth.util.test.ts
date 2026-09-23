import { describe, expect, it, vi } from "vitest";
import { requireTokenParsed } from "~/util/loaderAuth.util";

const { waitForAuthReady } = vi.hoisted(() => ({
  waitForAuthReady: vi.fn(),
}));
vi.mock("~/layout/auth-service", () => ({ waitForAuthReady }));

describe("requireTokenParsed", () => {
  it("returns the parsed token once the auth service is ready and authenticated", async () => {
    const tokenParsed = { UserId: "user-1" };
    waitForAuthReady.mockResolvedValue({
      isAuthenticated: () => true,
      getTokenParsed: vi.fn(async () => tokenParsed),
      login: vi.fn(),
    });

    await expect(requireTokenParsed()).resolves.toEqual(tokenParsed);
  });

  it("redirects to login and never resolves when there is no auth service", async () => {
    waitForAuthReady.mockResolvedValue(null);

    let settled = false;
    requireTokenParsed().then(() => {
      settled = true;
    });

    await new Promise((r) => setTimeout(r, 0));
    expect(settled).toBe(false);
  });

  it("redirects to login and never resolves when not authenticated", async () => {
    const login = vi.fn();
    waitForAuthReady.mockResolvedValue({
      isAuthenticated: () => false,
      getTokenParsed: vi.fn(),
      login,
    });

    let settled = false;
    requireTokenParsed().then(() => {
      settled = true;
    });

    await new Promise((r) => setTimeout(r, 0));
    expect(login).toHaveBeenCalled();
    expect(settled).toBe(false);
  });

  it("redirects to login and never resolves when authenticated but the token hasn't parsed yet", async () => {
    const login = vi.fn();
    waitForAuthReady.mockResolvedValue({
      isAuthenticated: () => true,
      getTokenParsed: vi.fn(async () => null),
      login,
    });

    let settled = false;
    requireTokenParsed().then(() => {
      settled = true;
    });

    await new Promise((r) => setTimeout(r, 0));
    expect(login).toHaveBeenCalled();
    expect(settled).toBe(false);
  });
});
