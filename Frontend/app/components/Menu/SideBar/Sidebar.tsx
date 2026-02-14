import React from "react";
import { cn } from "~/util/tailwind.util";
import DropdownMenu from "../DropdownMenu/DropdownMenu";
import MenuBranding from "../MenuBranding";
import MenuContent from "../MenuContent";
import MenuFooter from "../MenuFooter";
import MenuItem from "../MenuItem";

type SideBarProps = {
  color?: string | undefined;
  children?: React.ReactNode;
  className?: string;
};

export default function SideBar({ color = undefined, children, className }: SideBarProps) {
  const childrenArray = React.Children.toArray(children);
  return (
    <div className={cn("flex flex-col sm:flex-row", className)}>
      <div className="hidden sm:flex">
        <aside
          className={cn(
            `flex h-screen w-64 flex-col bg-(--board-primary) text-white`,
            color ? `bg-[${color}]` : "",
          )}
        >
          <div className="px-6 py-5">
            {childrenArray.filter(
              (child: any) => child.type === SideBar.Branding,
            )}
          </div>
          <nav className="flex-1 space-y-1 px-3">
            {childrenArray.filter((child: any) => child.type === SideBar.Item)}
          </nav>
          {childrenArray.filter((child: any) => child.type === SideBar.Footer)}
        </aside>
      </div>

      <div className="flex sm:hidden w-full">
        <DropdownMenu color={color}>
          {childrenArray.filter(
            (child: any) => child.type === SideBar.Branding,
          )}
          {childrenArray.filter((child: any) => child.type === SideBar.Item)}
          {childrenArray.filter((child: any) => child.type === SideBar.Footer)}
        </DropdownMenu>
      </div>

      <div className="flex-1 overflow-y-auto p-5 h-screen">
        {React.Children.toArray(children).find(
          (child: any) => child.type === SideBar.Content,
        )}
      </div>
    </div>
  );
}

// Header subcomponent
SideBar.Branding = MenuBranding;

// Navigation subcomponent
SideBar.Item = MenuItem;

// Footer subcomponent
SideBar.Footer = MenuFooter;

SideBar.Content = MenuContent;
