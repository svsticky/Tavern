import {
  Bell,
  CalendarDays,
  LayoutDashboard,
  SquareArrowOutUpRight,
} from "lucide-react";
import { useTranslation } from "react-i18next";
import { Outlet } from "react-router";
import NavBar from "~/components/Menu/NavBar/NavBar";

const profileOptions = {
  username: "JohnDoe",
  avatarUrl: "https://cdn.nos.nl/image/2017/07/16/403534/xxl.jpg",
  options: [
    { label: "Settings", action: () => alert("Navigating to Settings") },
    { label: "Logout", action: () => alert("Logging out") },
  ],
};

export default function NavBarLayout() {
  const { t } = useTranslation();

  const navBarItems = [
    {
      id: "dashboard",
      label: t("dashboard"),
      href: "/",
      icon: LayoutDashboard,
    },
    {
      id: "activities",
      label: t("activities"),
      href: "/activities",
      icon: CalendarDays,
    },
    {
      id: "announcements",
      label: t("announcements"),
      href: "/announcements",
      icon: Bell,
    },
    {
      id: "external-links",
      label: t("external_links"),
      href: "/external-links",
      icon: SquareArrowOutUpRight,
    },
  ];

  return (
    <div className="min-w-[415px]">
      <NavBar className="px-[10%]" maxWidthBeforeCompact={1046}>
        <NavBar.Branding title="Sticky" />
        {navBarItems.map((item) => (
          <NavBar.Item key={item.id} item={item} />
        ))}
        <NavBar.ProfileDropdown
          username={profileOptions.username}
          avatarUrl={profileOptions.avatarUrl}
          options={profileOptions.options}
        ></NavBar.ProfileDropdown>
      </NavBar>
      <main className="px-[10%] py-5">
        <Outlet />
      </main>
    </div>
  );
}
