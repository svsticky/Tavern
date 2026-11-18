import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  DEFAULT_DASHBOARD_WIDGETS,
  getDashboardStorageKey,
  loadDashboardWidgetConfig,
  resetDashboardWidgetConfig,
  saveDashboardWidgetConfig,
} from "~/util/dashboardWidgets";

describe("dashboardWidgets util", () => {
  beforeEach(() => {
    localStorage.clear();
    vi.restoreAllMocks();
  });

  describe("getDashboardStorageKey", () => {
    it("returns base key when no userId provided", () => {
      expect(getDashboardStorageKey()).toBe("tavern_dashboard_widgets");
    });

    it("returns user-specific key when userId provided", () => {
      expect(getDashboardStorageKey("user-123")).toBe(
        "tavern_dashboard_widgets_user-123",
      );
    });
  });

  describe("loadDashboardWidgetConfig", () => {
    it("returns default widgets when nothing is saved in localStorage", () => {
      const config = loadDashboardWidgetConfig("user-1");
      expect(config).toHaveLength(DEFAULT_DASHBOARD_WIDGETS.length);
      expect(config[0].id).toBe("announcements");
      expect(config[0].visible).toBe(true);
    });

    it("returns stored widgets when valid JSON exists in localStorage", () => {
      const customConfig = [
        { id: "announcements", visible: false, column: "main", order: 1 },
        { id: "upcoming_activities", visible: true, column: "main", order: 0 },
        { id: "my_enrollments", visible: true, column: "sidebar", order: 0 },
        { id: "my_groups", visible: false, column: "sidebar", order: 1 },
      ];
      localStorage.setItem(
        getDashboardStorageKey("user-1"),
        JSON.stringify(customConfig),
      );

      const config = loadDashboardWidgetConfig("user-1");
      expect(config.find((w) => w.id === "announcements")?.visible).toBe(false);
      expect(config.find((w) => w.id === "my_groups")?.visible).toBe(false);
      expect(config.find((w) => w.id === "upcoming_activities")?.visible).toBe(
        true,
      );
    });

    it("falls back to default widgets if JSON is corrupted", () => {
      localStorage.setItem(getDashboardStorageKey("user-1"), "invalid-json{");
      const config = loadDashboardWidgetConfig("user-1");
      expect(config).toEqual(DEFAULT_DASHBOARD_WIDGETS);
    });

    it("falls back to default widgets if stored data is not an array", () => {
      localStorage.setItem(getDashboardStorageKey("user-1"), '{"not":"array"}');
      const config = loadDashboardWidgetConfig("user-1");
      expect(config).toEqual(DEFAULT_DASHBOARD_WIDGETS);
    });

    it("returns default widgets when window.localStorage is undefined", () => {
      const originalStorage = window.localStorage;
      Object.defineProperty(window, "localStorage", {
        value: undefined,
        configurable: true,
        writable: true,
      });

      const config = loadDashboardWidgetConfig("user-none");
      expect(config).toEqual(DEFAULT_DASHBOARD_WIDGETS);

      saveDashboardWidgetConfig([], "user-none");

      Object.defineProperty(window, "localStorage", {
        value: originalStorage,
        configurable: true,
        writable: true,
      });
    });
  });

  describe("saveDashboardWidgetConfig", () => {
    it("saves widget configuration to localStorage", () => {
      const customConfig = [
        {
          id: "announcements" as const,
          visible: false,
          column: "sidebar" as const,
          order: 0,
        },
      ];
      saveDashboardWidgetConfig(customConfig, "user-2");

      const raw = localStorage.getItem(getDashboardStorageKey("user-2"));
      expect(raw).toBeTruthy();
      expect(JSON.parse(raw!)).toEqual(customConfig);
    });

    it("catches and logs error if setItem throws", () => {
      const consoleSpy = vi
        .spyOn(console, "error")
        .mockImplementation(() => {});
      vi.spyOn(Storage.prototype, "setItem").mockImplementation(() => {
        throw new Error("QuotaExceeded");
      });

      expect(() => saveDashboardWidgetConfig([], "user-err")).not.toThrow();
      expect(consoleSpy).toHaveBeenCalled();
      consoleSpy.mockRestore();
    });
  });

  describe("resetDashboardWidgetConfig", () => {
    it("removes the key from localStorage and returns default widgets", () => {
      localStorage.setItem(
        getDashboardStorageKey("user-3"),
        JSON.stringify([{ id: "announcements", visible: false }]),
      );

      const reset = resetDashboardWidgetConfig("user-3");
      expect(localStorage.getItem(getDashboardStorageKey("user-3"))).toBeNull();
      expect(reset).toEqual(DEFAULT_DASHBOARD_WIDGETS);
    });

    it("catches and logs error if removeItem throws", () => {
      const consoleSpy = vi
        .spyOn(console, "error")
        .mockImplementation(() => {});
      vi.spyOn(Storage.prototype, "removeItem").mockImplementation(() => {
        throw new Error("StorageError");
      });

      const reset = resetDashboardWidgetConfig("user-err");
      expect(reset).toEqual(DEFAULT_DASHBOARD_WIDGETS);
      expect(consoleSpy).toHaveBeenCalled();
      consoleSpy.mockRestore();
    });
  });

  describe("loadDashboardWidgetConfig item parsing fallbacks", () => {
    it("handles partial items with invalid visible or order", () => {
      const partialData = [
        { id: "announcements", visible: "not-a-bool" }, // invalid visible -> fallback
        {
          id: "upcoming_activities",
          visible: false,
          column: "sidebar",
          order: "not-a-number",
        }, // valid visible, invalid order -> fallback default order
        { id: "unknown_widget", visible: true },
      ];
      localStorage.setItem(
        getDashboardStorageKey("user-p"),
        JSON.stringify(partialData),
      );

      const config = loadDashboardWidgetConfig("user-p");
      const announcements = config.find((w) => w.id === "announcements");
      expect(announcements?.visible).toBe(true); // default fallback

      const upcoming = config.find((w) => w.id === "upcoming_activities");
      expect(upcoming?.visible).toBe(false);
      expect(upcoming?.column).toBe("sidebar");
      expect(upcoming?.order).toBe(1); // default order
    });
  });
});
