import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";

const { getSettingsById } = vi.hoisted(() => ({
  getSettingsById: vi.fn(),
}));

vi.mock("~/api", () => ({ getSettingsById }));

describe("loadBoardThemeSettings", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    document.documentElement.removeAttribute("style");
  });

  afterEach(() => {
    document.documentElement.removeAttribute("style");
  });

  it("applies the fetched value and counts a successfully loaded setting", async () => {
    getSettingsById.mockImplementation(({ path }: { path: { id: string } }) =>
      Promise.resolve(
        path.id === "BoardPrimary"
          ? { data: { value: "#123456" } }
          : { error: { title: "not found" } },
      ),
    );

    const { loadBoardThemeSettings } = await import("~/util/theme-settings");
    const count = await loadBoardThemeSettings();

    expect(count).toBe(1);
    expect(
      document.documentElement.style.getPropertyValue("--board-primary"),
    ).toBe("#123456");
  });

  it("falls back to the default color when a setting errors", async () => {
    getSettingsById.mockResolvedValue({ error: { title: "not found" } });

    const { loadBoardThemeSettings } = await import("~/util/theme-settings");
    const count = await loadBoardThemeSettings();

    expect(count).toBe(0);
    expect(
      document.documentElement.style.getPropertyValue("--board-primary"),
    ).toBe("#fa6b20");
  });

  it("falls back to the default color when the request is rejected", async () => {
    getSettingsById.mockRejectedValue(new Error("network error"));

    const { loadBoardThemeSettings } = await import("~/util/theme-settings");
    const count = await loadBoardThemeSettings();

    expect(count).toBe(0);
    expect(
      document.documentElement.style.getPropertyValue("--board-primary-dark"),
    ).toBe("#ca5617");
  });
});
