import { Monitor, Moon, Sun } from "lucide-react";
import { useTranslation } from "react-i18next";
import { type Theme, useTheme } from "~/context/ThemeContext";
import Button from "./Button";

interface ThemeToggleProps {
  variant?: "segmented" | "dropdown" | "icon";
  className?: string;
}

export default function ThemeToggle({
  variant = "segmented",
  className = "",
}: ThemeToggleProps) {
  const { t } = useTranslation();
  const { theme, resolvedTheme, setTheme, toggleTheme } = useTheme();

  if (variant === "icon") {
    const isDark = resolvedTheme === "dark";
    return (
      <button
        type="button"
        onClick={toggleTheme}
        className={`p-2 rounded-lg transition-colors hover:bg-gray-100 text-gray-700 cursor-pointer ${className}`}
        aria-label={t("toggle_theme")}
        title={t("toggle_theme")}
      >
        {isDark ? <Sun size={18} /> : <Moon size={18} />}
      </button>
    );
  }

  if (variant === "dropdown") {
    return (
      <div
        className={`flex items-center justify-between p-2 text-xs ${className}`}
      >
        <span className="text-gray-500 font-medium">{t("theme")}</span>
        <div className="flex bg-gray-100 rounded-lg p-0.5 border border-gray-200">
          <button
            type="button"
            onClick={() => setTheme("light")}
            title={t("theme_light")}
            className={`p-1.5 rounded-md transition-colors ${
              theme === "light"
                ? "bg-white text-(--board-primary) shadow-xs"
                : "text-gray-500 hover:text-gray-900"
            }`}
          >
            <Sun size={14} />
          </button>
          <button
            type="button"
            onClick={() => setTheme("dark")}
            title={t("theme_dark")}
            className={`p-1.5 rounded-md transition-colors ${
              theme === "dark"
                ? "bg-white text-(--board-primary) shadow-xs"
                : "text-gray-500 hover:text-gray-900"
            }`}
          >
            <Moon size={14} />
          </button>
          <button
            type="button"
            onClick={() => setTheme("system")}
            title={t("theme_system")}
            className={`p-1.5 rounded-md transition-colors ${
              theme === "system"
                ? "bg-white text-(--board-primary) shadow-xs"
                : "text-gray-500 hover:text-gray-900"
            }`}
          >
            <Monitor size={14} />
          </button>
        </div>
      </div>
    );
  }

  // Segmented control (default)
  const options: { value: Theme; label: string; icon: typeof Sun }[] = [
    { value: "light", label: t("theme_light"), icon: Sun },
    { value: "dark", label: t("theme_dark"), icon: Moon },
    { value: "system", label: t("theme_system"), icon: Monitor },
  ];

  return (
    <div className={`flex gap-2 ${className}`}>
      {options.map(({ value, label, icon: Icon }) => {
        const isSelected = theme === value;
        return (
          <Button
            key={value}
            type="button"
            variant={isSelected ? "primary" : "secondary"}
            className="flex-1 flex items-center justify-center gap-2"
            onClick={() => setTheme(value)}
          >
            <Icon size={16} />
            <span>{label}</span>
          </Button>
        );
      })}
    </div>
  );
}
