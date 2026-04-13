import React, { use, useEffect, useState } from "react";
import { cn } from "~/util/tailwind.util";
import MenuBranding from "../MenuBranding";
import MenuContent from "../MenuContent";
import MenuFooter from "../MenuFooter";
import MenuItem from "../MenuItem";
import { useLocation } from "react-router";

type DropdownMenuProps = {
  color?: string | undefined;
  className?: string;
  children?: React.ReactNode;
};

export default function DropdownMenu({
  color = undefined,
  className,
  children,
}: DropdownMenuProps) {
  const childrenArray = React.Children.toArray(children);
  const location = useLocation();
  const [isNavBarOpen, setIsNavBarOpen] = useState(false);

  const toggleNavBar = () => setIsNavBarOpen((prev) => !prev);

  useEffect(() => {
    setIsNavBarOpen(false);
  }, [location.pathname]);

  return (
    <header
      className={cn(
        "flex flex-col gap-1 bg-(--board-primary) w-full sticky top-0 z-50 shadow-lg py-2 px-5",
        className,
        color ? `bg-[${color}]` : "",
      )}
    >
      <div className="flex items-center justify-between w-full">
        {childrenArray.filter(
          (child: any) => child.type === DropdownMenu.Branding,
        )}

        <div className="flex items-center">
          <button
            type="button"
            className="text-white text-3xl cursor-pointer"
            onClick={toggleNavBar}
            aria-label="Open navigation menu"
          >
            {isNavBarOpen ? "✕" : "☰"}
          </button>
        </div>
      </div>

      {isNavBarOpen && (
        <div className="bg-(--board-primary) border-t border-opacity-20 border-white">
          <nav className="flex flex-col text-white text-xl gap-1 py-1">
            {childrenArray.filter(
              (child: any) => child.type === DropdownMenu.Item,
            )}
          </nav>

          {childrenArray.filter(
            (child: any) => child.type === DropdownMenu.Footer,
          )}
        </div>
      )}
    </header>
  );
}

// Branding subcomponent
DropdownMenu.Branding = MenuBranding;

// Navigation subcomponent
DropdownMenu.Item = MenuItem;

// Footer subcomponent
DropdownMenu.Footer = MenuFooter;

DropdownMenu.Content = MenuContent;
