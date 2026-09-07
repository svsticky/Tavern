import { fireEvent, render, screen } from "@testing-library/react";
import type React from "react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import ThemeToggle from "~/components/UI/ThemeToggle";
import { ThemeProvider } from "~/context/ThemeContext";

function renderWithTheme(ui: React.ReactElement) {
  return render(<ThemeProvider>{ui}</ThemeProvider>);
}

describe("ThemeToggle", () => {
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

  it("renders segmented control with Light, Dark, System options by default", () => {
    renderWithTheme(<ThemeToggle variant="segmented" />);

    expect(
      screen.getByRole("button", { name: /theme_light/i }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: /theme_dark/i }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole("button", { name: /theme_system/i }),
    ).toBeInTheDocument();
  });

  it("switches theme when segmented option is clicked", () => {
    renderWithTheme(<ThemeToggle variant="segmented" />);

    const darkBtn = screen.getByRole("button", { name: /theme_dark/i });
    fireEvent.click(darkBtn);

    expect(document.documentElement.classList.contains("dark")).toBe(true);
    expect(localStorage.getItem("tavern_theme")).toBe("dark");

    const lightBtn = screen.getByRole("button", { name: /theme_light/i });
    fireEvent.click(lightBtn);

    expect(document.documentElement.classList.contains("dark")).toBe(false);
    expect(localStorage.getItem("tavern_theme")).toBe("light");
  });

  it("renders icon toggle button and toggles on click", () => {
    renderWithTheme(<ThemeToggle variant="icon" />);

    const toggleBtn = screen.getByRole("button", { name: /toggle_theme/i });
    expect(toggleBtn).toBeInTheDocument();

    fireEvent.click(toggleBtn);
    expect(document.documentElement.classList.contains("dark")).toBe(true);

    fireEvent.click(toggleBtn);
    expect(document.documentElement.classList.contains("dark")).toBe(false);
  });

  it("renders dropdown variant with compact buttons", () => {
    renderWithTheme(<ThemeToggle variant="dropdown" />);

    expect(screen.getByText("theme")).toBeInTheDocument();
    const darkBtn = screen.getByTitle("theme_dark");
    expect(darkBtn).toBeInTheDocument();

    fireEvent.click(darkBtn);
    expect(document.documentElement.classList.contains("dark")).toBe(true);
  });
});
