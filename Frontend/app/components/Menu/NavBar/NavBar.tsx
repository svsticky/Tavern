import React, { useEffect, useRef, useState } from "react";
import { cn } from "~/util/tailwind.util";
import DropdownMenu from "../DropdownMenu/DropdownMenu";
import MenuBranding from "../MenuBranding";
import MenuContent from "../MenuContent";
import MenuItemComponent from "../MenuItem";
import type {
  ProfileDropdownContextValues,
  ProfileDropdownOption,
} from "./ProfileDropdown";
import ProfileDropdown from "./ProfileDropdown";

const ProfileDropdownContext: React.Context<ProfileDropdownContextValues> =
  React.createContext<ProfileDropdownContextValues>({
    compact: false,
    setCompact: () => {},
  });

type NavBarProps = {
  maxWidthBeforeCompact?: number;
  color?: string | undefined;
  className?: string;
  children?: React.ReactNode;
};

export default function NavBar({
  maxWidthBeforeCompact,
  color,
  className,
  children,
}: NavBarProps) {
  const [compact, setCompact] = useState(false);
  const containerRef = useRef<HTMLDivElement>(null);

  const childrenArray = React.Children.toArray(children);

  useEffect(() => {
    if (!containerRef.current) return;

    const observer = new ResizeObserver(() => {
      const width = containerRef.current?.offsetWidth ?? 0;
      setCompact(
        maxWidthBeforeCompact !== undefined && width < maxWidthBeforeCompact,
      );
    });
    observer.observe(containerRef.current);

    return () => observer.disconnect();
  }, [maxWidthBeforeCompact]);

  return (
    <ProfileDropdownContext.Provider value={{ compact, setCompact }}>
      <div ref={containerRef} className="w-full">
        <DropdownMenu
          color={color}
          className={cn(className, compact ? "" : "hidden")}
        >
          {childrenArray.filter((c: any) => c.type === NavBar.Branding)}
          {childrenArray.filter((c: any) => c.type === NavBar.Item)}
          <DropdownMenu.Footer>
            {childrenArray.filter(
              (c: any) => c.type === NavBar.ProfileDropdown,
            )}
          </DropdownMenu.Footer>
        </DropdownMenu>
        <header
          className={cn(
            "bg-(--board-primary) w-full sticky top-0 z-50 shadow-lg px-5 py-2",
            className,
            compact ? "hidden" : "",
            color ? `bg-[${color}]` : "",
          )}
        >
          <div className="relative flex items-center w-full">
            <div className="flex-shrink-0">
              {childrenArray.filter((c: any) => c.type === NavBar.Branding)}
            </div>

            <div className="absolute left-1/2 transform -translate-x-1/2 flex gap-1 text-white text-2xl items-center min-w-0">
              {childrenArray.filter((c: any) => c.type === NavBar.Item)}
            </div>

            <div className="flex-shrink-0 ml-auto">
              {childrenArray.filter(
                (c: any) => c.type === NavBar.ProfileDropdown,
              )}
            </div>
          </div>
        </header>
      </div>
    </ProfileDropdownContext.Provider>
  );
}

//  subcomponent
NavBar.Branding = MenuBranding;

// Navigation subcomponent
NavBar.Item = MenuItemComponent;

// Footer subcomponent
NavBar.ProfileDropdown = function NavBarProfileDropdown(props: {
  username: string;
  avatarUrl: string;
  options: ProfileDropdownOption[];
}) {
  return <ProfileDropdown context={ProfileDropdownContext} {...props} />;
};

// Content subcomponent
NavBar.Content = MenuContent;
