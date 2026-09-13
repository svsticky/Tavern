import { ShieldCheck, User } from "lucide-react";
import { useTranslation } from "react-i18next";
import { useAdminMode } from "~/context/AdminModeContext";
import Button from "./Button";

interface AdminModeToggleProps {
  variant?: "segmented" | "switch" | "dropdown";
  className?: string;
}

export default function AdminModeToggle({
  variant = "segmented",
  className = "",
}: AdminModeToggleProps) {
  const { t } = useTranslation();
  const { isAdminUser, adminMode, setAdminMode, toggleAdminMode } =
    useAdminMode();

  if (!isAdminUser) {
    return null;
  }

  if (variant === "dropdown") {
    return (
      <div
        className={`flex items-center justify-between p-2 text-xs ${className}`}
      >
        <span className="text-gray-500 font-medium">{t("view_mode")}</span>
        <div className="flex bg-gray-100 rounded-lg p-0.5 border border-gray-200">
          <button
            type="button"
            onClick={() => setAdminMode(true)}
            title={t("admin_mode")}
            aria-label={t("admin_mode")}
            className={`p-1.5 rounded-md transition-colors cursor-pointer ${
              adminMode
                ? "bg-white text-(--board-primary) shadow-xs"
                : "text-gray-500 hover:text-gray-900"
            }`}
          >
            <ShieldCheck size={14} />
          </button>
          <button
            type="button"
            onClick={() => setAdminMode(false)}
            title={t("member_mode")}
            aria-label={t("member_mode")}
            className={`p-1.5 rounded-md transition-colors cursor-pointer ${
              !adminMode
                ? "bg-white text-(--board-primary) shadow-xs"
                : "text-gray-500 hover:text-gray-900"
            }`}
          >
            <User size={14} />
          </button>
        </div>
      </div>
    );
  }

  if (variant === "switch") {
    return (
      <div className={`flex items-center justify-between gap-4 ${className}`}>
        <div className="flex flex-col">
          <span className="text-sm font-medium text-gray-700">
            {!adminMode ? t("member_mode_active") : t("admin_mode_active")}
          </span>
          <span className="text-xs text-gray-500">
            {!adminMode
              ? t("member_mode_description")
              : t("admin_mode_description")}
          </span>
        </div>
        <button
          type="button"
          role="switch"
          aria-checked={!adminMode}
          onClick={toggleAdminMode}
          aria-label={t("member_mode")}
          title={
            !adminMode ? t("switch_to_admin_mode") : t("switch_to_member_mode")
          }
          className={`relative inline-flex h-6 w-11 shrink-0 cursor-pointer rounded-full border-2 border-transparent transition-colors duration-200 ease-in-out focus:outline-none focus:ring-2 focus:ring-(--board-primary) ${
            !adminMode ? "bg-(--board-primary)" : "bg-gray-300"
          }`}
        >
          <span
            className={`pointer-events-none inline-block h-5 w-5 transform rounded-full bg-white shadow-md ring-0 transition duration-200 ease-in-out ${
              !adminMode ? "translate-x-5" : "translate-x-0"
            }`}
          />
        </button>
      </div>
    );
  }

  // Segmented variant (default)
  return (
    <div className={`flex flex-col gap-2 ${className}`}>
      <div className="flex gap-2">
        <Button
          type="button"
          variant={adminMode ? "primary" : "secondary"}
          className="flex-1 flex items-center justify-center gap-2"
          onClick={() => setAdminMode(true)}
        >
          <ShieldCheck size={16} />
          <span>{t("admin_mode")}</span>
        </Button>
        <Button
          type="button"
          variant={!adminMode ? "primary" : "secondary"}
          className="flex-1 flex items-center justify-center gap-2"
          onClick={() => setAdminMode(false)}
        >
          <User size={16} />
          <span>{t("member_mode")}</span>
        </Button>
      </div>
      <p className="text-xs text-gray-500">
        {adminMode ? t("admin_mode_description") : t("member_mode_description")}
      </p>
    </div>
  );
}
