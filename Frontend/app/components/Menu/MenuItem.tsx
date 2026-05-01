import type { ComponentType } from "react";
import { NavLink, useLocation } from "react-router";
import { cn } from "~/util/tailwind.util";

/**
 * Definition for a navigation item.
 * @typedef {Object} MenuItem
 * @property {string} id - Unique identifier for the item.
 * @property {string} label - The display text for the navigation link.
 * @property {string} href - The destination URL or route path.
 * @property {ComponentType<any>} [icon] - An optional React component to render as an icon.
 */
export type MenuItem = {
  id: string;
  label: string;
  href: string;
  icon?: ComponentType<any>;
};

/**
 * Props for the MenuItem component.
 * @typedef {Object} MenuItemProps
 * @property {MenuItem} item - The menu item configuration object.
 * @property {string} [className] - Additional CSS classes to apply to the link.
 * @property {() => void} [onClick] - Optional click handler, often used to close mobile menus.
 */
type MenuItemProps = {
  item: MenuItem;
  className?: string;
  onClick?: () => void;
};

/**
 * A navigation link component used within sidebars and navbars.
 * Automatically handles active state styling based on the current URL path.
 * 
 * @component
 * @param {MenuItemProps} props - The component properties.
 */
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
