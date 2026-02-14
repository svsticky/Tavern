import type { ComponentType } from "react";
import { NavLink, useLocation } from "react-router";
import { cn } from "~/util/tailwind.util";

export type MenuItem = {
  id: string;
  label: string;
  href: string;
  icon?: ComponentType<any>;
};

type MenuItemProps = {
  item: MenuItem;
  className?: string;
  onClick?: () => void;
};

export default function MenuItem({ item, onClick, className }: MenuItemProps) {
  const IconComponent = item.icon;
  const location = useLocation();
  const isActive = location.pathname === item.href;

  return (
    <NavLink
      to={item.href}
      className={cn(
        "flex items-center gap-2 font-bold no-underline transition-colors duration-200 ease-in-out border-2 border-transparent rounded-lg py-2 px-2",
        "text-sm text-white justify-start whitespace-nowrap",
        isActive
          ? "bg-[var(--board-primary-light)] border-white/20"
          : "hover:bg-[var(--board-primary-light)] hover:border-white/20",
        className,
      )}
      onClick={() => onClick?.()}
    >
      {IconComponent && <IconComponent />}
      <span>{item.label}</span>
    </NavLink>
  );
}
