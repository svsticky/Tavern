import { act, renderHook } from "@testing-library/react";
import type React from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  resolveEffectiveTheme,
  THEME_STORAGE_KEY,
  ThemeProvider,
  useTheme,
} from "~/context/ThemeContext";

describe("ThemeContext", () => {
  beforeEach(() => {
    localStorage.clear();
    document.documentElement.classList.remove("dark");
    vi.restoreAllMocks();

    window.matchMedia = vi.fn().mockImplementation((query) => ({
      matches: false,
      media: query,
      onchange: null,
      addListener: vi.fn(),
      removeListener: vi.fn(),
      addEventListener: vi.fn(),
      removeEventListener: vi.fn(),
      dispatchEvent: vi.fn(),
    }));
  });

  describe("resolveEffectiveTheme", () => {
    it("resolves dark to dark and light to light", () => {
      expect(resolveEffectiveTheme("dark")).toBe("dark");
      expect(resolveEffectiveTheme("light")).toBe("light");
    });

    it("resolves system based on matchMedia", () => {
      (window.matchMedia as any).mockReturnValue({ matches: true });
      expect(resolveEffectiveTheme("system")).toBe("dark");

      (window.matchMedia as any).mockReturnValue({ matches: false });
      expect(resolveEffectiveTheme("system")).toBe("light");
    });
  });

  describe("ThemeProvider", () => {
    const wrapper = ({ children }: { children: React.ReactNode }) => (
      <ThemeProvider>{children}</ThemeProvider>
    );

    it("defaults to system and respects system preference", () => {
      const { result } = renderHook(() => useTheme(), { wrapper });
      expect(result.current.theme).toBe("system");
      expect(result.current.resolvedTheme).toBe("light");
      expect(document.documentElement.classList.contains("dark")).toBe(false);
    });

    it("applies dark class when dark theme is selected", () => {
      const { result } = renderHook(() => useTheme(), { wrapper });

      act(() => {
        result.current.setTheme("dark");
      });

      expect(result.current.theme).toBe("dark");
      expect(result.current.resolvedTheme).toBe("dark");
      expect(document.documentElement.classList.contains("dark")).toBe(true);
      expect(localStorage.getItem(THEME_STORAGE_KEY)).toBe("dark");
    });

    it("removes dark class when light theme is selected", () => {
      document.documentElement.classList.add("dark");
      const { result } = renderHook(() => useTheme(), { wrapper });

      act(() => {
        result.current.setTheme("light");
      });

      expect(result.current.theme).toBe("light");
      expect(result.current.resolvedTheme).toBe("light");
      expect(document.documentElement.classList.contains("dark")).toBe(false);
      expect(localStorage.getItem(THEME_STORAGE_KEY)).toBe("light");
    });

    it("toggles between dark and light using toggleTheme", () => {
      const { result } = renderHook(() => useTheme(), { wrapper });

      act(() => {
        result.current.toggleTheme();
      });
      expect(result.current.resolvedTheme).toBe("dark");
      expect(document.documentElement.classList.contains("dark")).toBe(true);

      act(() => {
        result.current.toggleTheme();
      });
      expect(result.current.resolvedTheme).toBe("light");
      expect(document.documentElement.classList.contains("dark")).toBe(false);
    });

    it("uses default functions when used outside ThemeProvider", () => {
      const { result } = renderHook(() => useTheme());
      expect(result.current.theme).toBe("system");
      expect(() => {
        result.current.setTheme("dark");
        result.current.toggleTheme();
      }).not.toThrow();
    });

    it("initializes from localStorage if valid theme saved", () => {
      localStorage.setItem(THEME_STORAGE_KEY, "dark");
      const { result } = renderHook(() => useTheme(), { wrapper });
      expect(result.current.theme).toBe("dark");
      expect(result.current.resolvedTheme).toBe("dark");
    });

    it("reacts to system media query change when theme is system", () => {
      let changeHandler: (() => void) | null = null;
      window.matchMedia = vi.fn().mockImplementation((query) => ({
        matches: false,
        media: query,
        addEventListener: vi.fn((event, handler) => {
          if (event === "change") changeHandler = handler;
        }),
        removeEventListener: vi.fn(),
      }));

      renderHook(() => useTheme(), { wrapper });
      expect(changeHandler).toBeTypeOf("function");

      act(() => {
        // Change system preference to dark
        (window.matchMedia as any).mockImplementation((query: string) => ({
          matches: true,
          media: query,
          addEventListener: vi.fn(),
          removeEventListener: vi.fn(),
        }));
        changeHandler!();
      });

      expect(document.documentElement.classList.contains("dark")).toBe(true);
    });
  });
});
