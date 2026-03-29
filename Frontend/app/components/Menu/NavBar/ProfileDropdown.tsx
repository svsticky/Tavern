import React, { useEffect, useRef, useState } from "react";

export type ProfileDropdownContextValues = {
  compact: boolean;
  setCompact: React.Dispatch<React.SetStateAction<boolean>>;
};

export type ProfileDropdownOption = {
  label: string;
  action: () => void;
};

export type ProfileOptions = {
  username: string;
  context?: React.Context<ProfileDropdownContextValues>;
  avatarUrl?: string;
  options?: ProfileDropdownOption[];
};

export default function ProfileDropdown({
  username,
  context = React.createContext<ProfileDropdownContextValues>({
    compact: false,
    setCompact: () => {},
  }),
  avatarUrl = "/default-avatar.png",
  options = [],
}: ProfileOptions) {
  const [isOpen, setIsOpen] = useState(false);
  const compact = React.useContext(context).compact;
  const dropdownRef = useRef<HTMLDivElement | null>(null);

  const toggleDropdown = () => {
    if (!compact) setIsOpen((prev) => !prev);
  };

  const handleOptionClick = (action: () => void) => {
    action();
    if (!compact) setIsOpen(false);
  };

  // Close on outside click (desktop only)
  useEffect(() => {
    if (compact || !isOpen) return;

    const handleClickOutside = (event: MouseEvent) => {
      if (
        dropdownRef.current &&
        !dropdownRef.current.contains(event.target as Node)
      ) {
        setIsOpen(false);
      }
    };

    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, [compact, isOpen]);

  return (
    <div ref={dropdownRef} className={compact ? "w-full" : "relative ml-5"}>
      <button
        type="button"
        onClick={toggleDropdown}
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
          className="w-8 h-8 rounded-full object-cover"
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
            <button
              key={option.label}
              type="button"
              onClick={() => handleOptionClick(option.action)}
              className={`
                text-left px-2 py-2.5 text-sm cursor-pointer
                ${
                  compact
                    ? "text-white rounded-lg hover:bg-(--board-primary-light)"
                    : "text-gray-800 hover:bg-gray-100"
                }
              `}
            >
              {option.label}
            </button>
          ))}
        </div>
      )}
    </div>
  );
}
