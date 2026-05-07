import React, { useEffect, useState } from "react";
import { useLocation } from "react-router";
import { cn } from "~/util/tailwind.util";
import MenuBranding from "../MenuBranding";
import MenuContent from "../MenuContent";
import MenuFooter from "../MenuFooter";
import MenuItem from "../MenuItem";

/**
 * A responsive, sticky dropdown navigation header.
 *
 * This component uses the **Compound Component pattern**. It filters its children
 * based on their sub-component type (Branding, Item, or Footer) and injects them
 * into specific layout slots. It also handles mobile toggle state and
 * automatically closes the menu on route changes.
 *
 * @component
 * @example
 * ```tsx
 * <DropdownMenu>
 *   <DropdownMenu.Branding>My App</DropdownMenu.Branding>
 *   <DropdownMenu.Item href="/home">Home</DropdownMenu.Item>
 *   <DropdownMenu.Footer>v1.0.0</DropdownMenu.Footer>
 * </DropdownMenu>
 * ```
 */
export default function DropdownMenu({
  color = undefined,
  className,
  children,
}: {
  color?: string | undefined;
  className?: string;
  children?: React.ReactNode;
}) {
  const childrenArray = React.Children.toArray(children);
  const _location = useLocation();
  const [isNavBarOpen, setIsNavBarOpen] = useState(false);

  const toggleNavBar = () => setIsNavBarOpen((prev) => !prev);

  useEffect(() => {
    setIsNavBarOpen(false);
  }, []);

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
            {childrenArray
            .filter((child: any) => child.type === DropdownMenu.Item)
            .map((child, index) => {
              if (React.isValidElement(child)) {
                return React.cloneElement(child as React.ReactElement<any>, {
                  key: index,
                  onClick: () => setIsNavBarOpen(false),
                });
              }
              return child;
            })}
          </nav>

          {childrenArray
            .filter((child: any) => child.type === DropdownMenu.Footer)
            .map((child, index) => {
              if (React.isValidElement(child)) {
                return React.cloneElement(child as React.ReactElement<any>, {
                  key: index,
                  onClose: () => setIsNavBarOpen(false),
                });
              }
              return child;
            })
          }
        </div>
      )}
    </header>
  );
}

/**
 * Sub-component for branding/logos. Placed in the top-left of the header.
 * @static
 */
DropdownMenu.Branding = MenuBranding;

/**
 * Sub-component for navigation links. Rendered inside the toggleable list.
 * @static
 */
DropdownMenu.Item = MenuItem;

/**
 * Sub-component for additional content or versioning at the bottom of the menu.
 * @static
 */
DropdownMenu.Footer = MenuFooter;

/**
 * Sub-component for general menu content.
 * @static
 */
DropdownMenu.Content = MenuContent;
