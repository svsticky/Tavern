import React, { useEffect, useRef, useState } from "react";
import { cn } from "~/util/tailwind.util";
import DropdownMenu from "../DropdownMenu/DropdownMenu";
import MenuBranding from "../MenuBranding";
import MenuContent from "../MenuContent";
import MenuItemComponent from "../MenuItem";
import type {
  ProfileDropdownContextValues,
  ProfileDropdownOption,
  ProfileOptions,
} from "./ProfileDropdown/ProfileDropdown";
import ProfileDropdown from "./ProfileDropdown/ProfileDropdown";

/**
 * Context to manage the visual state (compact vs. full) of the NavBar and its children.
 */
const ProfileDropdownContext: React.Context<ProfileDropdownContextValues> =
  React.createContext<ProfileDropdownContextValues>({
    compact: false,
    setCompact: () => {},
  });

/**
 * Props for the NavBar component.
 * @typedef {Object} NavBarProps
 * @property {number} [maxWidthBeforeCompact] - The pixel width threshold at which the navbar switches to a compact/mobile dropdown view.
 * @property {string} [color] - Optional background color override.
 * @property {string} [className] - Additional CSS classes for styling.
 * @property {React.ReactNode} [children] - Components to be rendered within the NavBar (e.g., NavBar.Branding, NavBar.Item).
 */
type NavBarProps = {
  maxWidthBeforeCompact?: number;
  color?: string | undefined;
  className?: string;
  children?: React.ReactNode;
};

/**
 * A responsive navigation bar that switches between a standard horizontal layout and a
 * compact dropdown menu based on container width.
 *
 * @param {NavBarProps} props - The properties for the NavBar.
 */
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
            {childrenArray
              .filter((c: any) => c.type === NavBar.ProfileDropdown)
              .map((child, index) => {
                if (React.isValidElement(child)) {
                  return React.cloneElement(child as React.ReactElement<any>, {
                    key: index,
                    onOptionSelect: (option: any) => {
                      const props = child.props as any;
                      if (props.onOptionSelect) props.onOptionSelect(option);

                      if (props.onClose) props.onClose();
                    },
                  });
                }
                return child;
              })}
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

/**
 * Subcomponent for rendering branding assets (e.g., Logos, Titles) within the NavBar.
 */
NavBar.Branding = MenuBranding;

/**
 * Subcomponent for individual navigation links or interactive items.
 */
NavBar.Item = MenuItemComponent;

/**
 * Subcomponent for the user profile section, integrating with the NavBar's compact state.
 *
 * @param {Object} props - Profile dropdown properties.
 * @param {string} props.username - The name of the current user.
 * @param {string} props.avatarUrl - The URL of the user's profile image.
 * @param {ProfileDropdownOption[]} props.options - List of menu options for the dropdown.
 * @param {(option: ProfileDropdownOption) => void} [props.onOptionSelect] - Optional callback triggered when a dropdown option is selected, receiving the selected option as an argument.
 */
NavBar.ProfileDropdown = function NavBarProfileDropdown(
  props: Omit<ProfileOptions, "context">,
) {
  return <ProfileDropdown context={ProfileDropdownContext} {...props} />;
};

/**
 * Subcomponent for general content sections within the navigation system.
 */
NavBar.Content = MenuContent;
