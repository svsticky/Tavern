import { act, renderHook } from "@testing-library/react";
import type React from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  ADMIN_MODE_CHANGED_EVENT,
  ADMIN_MODE_STORAGE_KEY,
  AdminModeProvider,
  isAdminModeActive,
  useAdminMode,
} from "~/context/AdminModeContext";

describe("AdminModeContext", () => {
  beforeEach(() => {
    localStorage.clear();
    vi.restoreAllMocks();
  });

  describe("isAdminModeActive", () => {
    it("returns true when localStorage has no entry", () => {
      expect(isAdminModeActive()).toBe(true);
    });

    it("returns false when localStorage has false", () => {
      localStorage.setItem(ADMIN_MODE_STORAGE_KEY, "false");
      expect(isAdminModeActive()).toBe(false);
    });

    it("returns true when localStorage has true", () => {
      localStorage.setItem(ADMIN_MODE_STORAGE_KEY, "true");
      expect(isAdminModeActive()).toBe(true);
    });
  });

  describe("AdminModeProvider and useAdminMode", () => {
    it("provides default values when used outside provider", () => {
      const { result } = renderHook(() => useAdminMode());
      expect(result.current.isAdminUser).toBe(false);
      expect(result.current.adminMode).toBe(true);
      // Calling defaults should not crash
      expect(() => {
        result.current.setIsAdminUser(true);
        result.current.setAdminMode(true);
        result.current.toggleAdminMode();
      }).not.toThrow();
    });

    it("returns adminMode false when user is not an admin, even if adminMode state is true", () => {
      const wrapper = ({ children }: { children: React.ReactNode }) => (
        <AdminModeProvider initialIsAdmin={false}>{children}</AdminModeProvider>
      );
      const { result } = renderHook(() => useAdminMode(), { wrapper });

      expect(result.current.isAdminUser).toBe(false);
      expect(result.current.adminMode).toBe(false);
    });

    it("allows toggling adminMode when user is an admin", () => {
      const wrapper = ({ children }: { children: React.ReactNode }) => (
        <AdminModeProvider initialIsAdmin={true}>{children}</AdminModeProvider>
      );
      const { result } = renderHook(() => useAdminMode(), { wrapper });

      expect(result.current.isAdminUser).toBe(true);
      expect(result.current.adminMode).toBe(true);

      act(() => {
        result.current.toggleAdminMode();
      });

      expect(result.current.adminMode).toBe(false);
      expect(localStorage.getItem(ADMIN_MODE_STORAGE_KEY)).toBe("false");

      act(() => {
        result.current.toggleAdminMode();
      });

      expect(result.current.adminMode).toBe(true);
      expect(localStorage.getItem(ADMIN_MODE_STORAGE_KEY)).toBe("true");
    });

    it("allows setting adminMode explicitly", () => {
      const wrapper = ({ children }: { children: React.ReactNode }) => (
        <AdminModeProvider initialIsAdmin={true}>{children}</AdminModeProvider>
      );
      const { result } = renderHook(() => useAdminMode(), { wrapper });

      act(() => {
        result.current.setAdminMode(false);
      });
      expect(result.current.adminMode).toBe(false);

      act(() => {
        result.current.setAdminMode(true);
      });
      expect(result.current.adminMode).toBe(true);
    });

    it("updates when setIsAdminUser is called", () => {
      const wrapper = ({ children }: { children: React.ReactNode }) => (
        <AdminModeProvider initialIsAdmin={false}>{children}</AdminModeProvider>
      );
      const { result } = renderHook(() => useAdminMode(), { wrapper });

      expect(result.current.adminMode).toBe(false);

      act(() => {
        result.current.setIsAdminUser(true);
      });

      expect(result.current.isAdminUser).toBe(true);
      expect(result.current.adminMode).toBe(true);
    });

    it("listens to ADMIN_MODE_CHANGED_EVENT with custom detail", () => {
      const wrapper = ({ children }: { children: React.ReactNode }) => (
        <AdminModeProvider initialIsAdmin={true}>{children}</AdminModeProvider>
      );
      const { result } = renderHook(() => useAdminMode(), { wrapper });

      act(() => {
        window.dispatchEvent(
          new CustomEvent(ADMIN_MODE_CHANGED_EVENT, {
            detail: { adminMode: false },
          }),
        );
      });

      expect(result.current.adminMode).toBe(false);

      act(() => {
        localStorage.setItem(ADMIN_MODE_STORAGE_KEY, "true");
        window.dispatchEvent(new CustomEvent(ADMIN_MODE_CHANGED_EVENT, {}));
      });

      expect(result.current.adminMode).toBe(true);
    });
  });
});
