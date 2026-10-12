import React, { useEffect, useRef, useState } from "react";
import { NavLink } from "react-router";
import {
  handleClickOutside,
  handleOptionClick,
  toggleDropdown,
} from "./ProfileDropdown.handlers";

/**
 * Context values required to control the dropdown's layout behavior.
 */
export type ProfileDropdownContextValues = {
  compact: boolean;
  setCompact: React.Dispatch<React.SetStateAction<boolean>>;
};

/**
 * Represents a single clickable action within the dropdown menu.
 */
export type ProfileDropdownOption = {
  label: string;
  href: string;
};

/**
 * Props for the {@link ProfileDropdown} component.
 */
export type ProfileOptions = {
  username: string;
  context?: React.Context<ProfileDropdownContextValues>;
  avatarUrl?: string;
  options?: ProfileDropdownOption[];
  onOptionSelect?: (option: ProfileDropdownOption) => void;
  onClose?: () => void;
  isHonoraryOrMerit?: boolean;
};

/**
 * A profile menu component that adapts its layout based on a provided context.
 *
 * In standard mode, it behaves as a floating dropdown that closes on outside clicks.
 * In compact mode, it renders as an inline list suitable for sidebars.
 *
 * @param {ProfileOptions} props - The properties for the component.
 * @returns {JSX.Element} The rendered ProfileDropdown component.
 */
export default function ProfileDropdown({
  username,
  context = React.createContext<ProfileDropdownContextValues>({
    compact: false,
    setCompact: () => {},
  }),
  avatarUrl = "/default-avatar.png",
  options = [],
  onOptionSelect,
  onClose,
  isHonoraryOrMerit = false,
}: ProfileOptions) {
  const [isOpen, setIsOpen] = useState(false);
  const compact = React.useContext(context).compact;
  const dropdownRef = useRef<HTMLDivElement | null>(null);

  // Close on outside click (desktop only)
  useEffect(() => {
    if (compact || !isOpen) return;

    const onClickOutside = (event: MouseEvent) =>
      handleClickOutside(event, dropdownRef, setIsOpen);

    document.addEventListener("mousedown", onClickOutside);
    return () => document.removeEventListener("mousedown", onClickOutside);
  }, [compact, isOpen]);

  return (
    <div ref={dropdownRef} className={compact ? "w-full" : "relative ml-5"}>
      <button
        type="button"
        onClick={() => toggleDropdown(compact, setIsOpen)}
        className={`
          flex items-center px-2 gap-2 rounded-xl border-2
          ${
            compact
              ? "w-full py-2 justify-start border-transparent cursor-default"
              : "py-1 cursor-pointer border-transparent hover:bg-(--board-primary-light) hover:border-white/20 transition-colors"
          }
        `}
      >
        <img
          src={avatarUrl}
          alt={`${username} avatar`}
          className={`w-8 h-8 rounded-full object-cover ${
            isHonoraryOrMerit
              ? "ring-2 ring-amber-400 ring-offset-1 ring-offset-(--board-primary)"
              : ""
          }`}
        />
        <span className="text-white font-bold text-sm">{username}</span>
      </button>

      {(compact || isOpen) && (
        <div
          className={`
            flex flex-col mt-1 overflow-hidden
            ${
              compact
                ? "w-full"
                : "absolute right-0 mt-3 min-w-40 bg-white rounded-lg shadow-lg"
            }
          `}
        >
          {options.map((option) => (
            <NavLink
              to={option.href}
              key={option.label}
              onClick={(e) => {
                if (!e.metaKey && !e.ctrlKey && !e.shiftKey && e.button !== 1) {
                  handleOptionClick(
                    () => {
                      onOptionSelect?.(option);
                      onClose?.();
                    },
                    compact,
                    setIsOpen,
                  );
                } else {
                  onClose?.();
                }
              }}
              className={`
                block text-left px-2 py-2.5 text-sm cursor-pointer
                ${
                  compact
                    ? "text-white rounded-lg hover:bg-(--board-primary-light)"
                    : "text-gray-800 hover:bg-gray-100"
                }
              `}
            >
              {option.label}
            </NavLink>
          ))}
        </div>
      )}
    </div>
  );
}
