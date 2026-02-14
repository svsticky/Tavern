import {
  Bell,
  CalendarDays,
  Euro,
  LayoutDashboard,
  LogOut,
  Settings,
  Users,
  UsersRound,
} from "lucide-react";
import { useTranslation } from "react-i18next";
import { Outlet } from "react-router";
import type { MenuItem } from "~/components/Menu/MenuItem";
import SideBar from "~/components/Menu/SideBar/Sidebar";

export default function AdminDashboardLayout() {
  const { t } = useTranslation();

  const sideBarItems: MenuItem[] = [
    {
      id: "dashboard",
      label: t("dashboard"),
      href: "/admin",
      icon: LayoutDashboard,
    },
    {
      id: "activities",
      label: t("activities"),
      href: "/admin/activities",
      icon: CalendarDays,
    },
    {
      id: "members",
      label: t("members"),
      href: "/admin/members",
      icon: Users,
    },
    {
      id: "groups",
      label: t("groups"),
      href: "/admin/groups",
      icon: UsersRound,
    },
    {
      id: "payments",
      label: t("payments"),
      href: "/admin/payments",
      icon: Euro,
    },
    {
      id: "announcements",
      label: t("announcements"),
      href: "/admin/announcements",
      icon: Bell,
    },
    {
      id: "settings",
      label: t("settings"),
      href: "/admin/settings",
      icon: Settings,
    },
  ];

  return (
    <div className="min-h-screen min-w-[415px] flex flex-col lg:flex-row">
      <SideBar>
        <SideBar.Branding title="Admin" />
        {sideBarItems.map((item) => (
          <SideBar.Item key={item.id} item={item} />
        ))}
        <SideBar.Footer>
          <button
            type="button"
            className="flex w-full items-center gap-3 rounded-xl px-4 py-2 text-sm text-white/90 hover:bg-white/10 transition-colors duration-200 ease-in-out cursor-pointer"
          >
            <LogOut size={16} />
            <span>{t("to_member_portal")}</span>
          </button>
        </SideBar.Footer>
        <SideBar.Content>
          <Outlet />
        </SideBar.Content>
      </SideBar>
    </div>
  );
}
