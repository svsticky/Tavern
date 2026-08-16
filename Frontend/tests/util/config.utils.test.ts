import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { getEnv } from "~/util/config.utils";

type WindowWithEnv = Window & { _env_?: Record<string, string> };

function setWindowEnv(value: Record<string, string> | undefined) {
  (window as WindowWithEnv)._env_ = value;
}

describe("getEnv", () => {
  beforeEach(() => {
    // getEnv() checks the exact, non-prefixed key first - some environments (e.g. this project's
    // devcontainer) define a real "ApiUrl" var matching the custom envPrefix in vite.config.ts,
    // which would otherwise short-circuit every test below before it reaches the window._env_/
    // VITE_-prefixed fallback logic these tests actually exercise.
    vi.stubEnv("ApiUrl", "");
  });

  afterEach(() => {
    vi.unstubAllEnvs();
    vi.unstubAllGlobals();
    setWindowEnv(undefined);
    delete process.env.VITE_ApiUrl;
  });

  it("returns the value directly from import.meta.env when the exact key is set", () => {
    vi.stubEnv("ApiUrl", "https://direct.example.com");
    expect(getEnv("ApiUrl")).toBe("https://direct.example.com");
  });

  it("falls back to window._env_ using the VITE_-prefixed key", () => {
    setWindowEnv({ VITE_ApiUrl: "https://runtime.example.com" });
    expect(getEnv("ApiUrl")).toBe("https://runtime.example.com");
  });

  it("falls back to the VITE_-prefixed import.meta.env key when window._env_ is unset", () => {
    vi.stubEnv("VITE_ApiUrl", "https://build-time.example.com");
    expect(getEnv("ApiUrl")).toBe("https://build-time.example.com");
  });

  it("returns undefined when nothing matches", () => {
    expect(getEnv("SomethingThatIsNeverSet")).toBeUndefined();
  });

  it("prefers window._env_ over the VITE_-prefixed import.meta.env fallback", () => {
    vi.stubEnv("VITE_ApiUrl", "https://build-time.example.com");
    setWindowEnv({ VITE_ApiUrl: "https://runtime.example.com" });
    expect(getEnv("ApiUrl")).toBe("https://runtime.example.com");
  });

  it("falls back to process.env when window is undefined", () => {
    vi.stubGlobal("window", undefined);
    process.env.VITE_ApiUrl = "https://process-env.example.com";

    expect(getEnv("ApiUrl")).toBe("https://process-env.example.com");
  });
});
